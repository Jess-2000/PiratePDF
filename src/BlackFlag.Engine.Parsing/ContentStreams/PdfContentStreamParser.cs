using System.Text;
using BlackFlag.Engine.IO.RandomAccess;
using BlackFlag.Engine.Objects;
using BlackFlag.Engine.Rendering.Graphics;
using BlackFlag.Engine.Syntax.Lexing;
using BlackFlag.Engine.Syntax.Parsing;

namespace BlackFlag.Engine.Parsing.ContentStreams;

/// <summary>Parses decoded PDF content streams into canonical graphics operations.</summary>
public sealed class PdfContentStreamParser
{
    private readonly PdfContentParseOptions options;

    /// <summary>Initializes a content-stream parser.</summary>
    public PdfContentStreamParser(PdfContentParseOptions? options = null) =>
        this.options = options ?? new PdfContentParseOptions();

    /// <summary>Materializes all operations from one decoded content stream.</summary>
    public IReadOnlyList<PdfGraphicsOperation> Parse(ReadOnlyMemory<byte> decodedBytes, int streamIndex = 0) =>
        Enumerate(decodedBytes, streamIndex).ToArray();

    /// <summary>Lazily enumerates operations from one decoded content stream.</summary>
    public IEnumerable<PdfGraphicsOperation> Enumerate(ReadOnlyMemory<byte> decodedBytes, int streamIndex = 0)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(streamIndex);
        return EnumerateCore(decodedBytes.ToArray(), streamIndex);
    }

    /// <summary>Parses multiple decoded streams in sequence.</summary>
    public IEnumerable<PdfGraphicsOperation> Enumerate(IEnumerable<ReadOnlyMemory<byte>> decodedStreams)
    {
        ArgumentNullException.ThrowIfNull(decodedStreams);
        return EnumerateStreamsCore(decodedStreams);
    }

    private IEnumerable<PdfGraphicsOperation> EnumerateStreamsCore(IEnumerable<ReadOnlyMemory<byte>> streams)
    {
        int streamIndex = 0;
        foreach (ReadOnlyMemory<byte> stream in streams)
        {
            foreach (PdfGraphicsOperation operation in Enumerate(stream, streamIndex))
            {
                yield return operation;
            }
            streamIndex++;
        }
    }

    private IEnumerable<PdfGraphicsOperation> EnumerateCore(byte[] bytes, int streamIndex)
    {
        using var source = new MemoryRandomAccessSource(bytes);
        PdfTokenReader reader = CreateReader(source, offset: 0);
        var objectParser = new PdfObjectParser(reader, source);
        var operands = new List<PdfObject>();
        long operationStart = -1;
        int operationCount = 0;
        int graphicsStateDepth = 0;

        while (!reader.EndOfFile)
        {
            PdfToken token = reader.Peek();
            operationStart = operationStart < 0 ? token.Span.Start.Offset : operationStart;

            if (token.Kind != PdfTokenKind.Keyword)
            {
                try
                {
                    operands.Add(objectParser.Parse());
                }
                catch (Exception exception) when (exception is FormatException or EndOfStreamException)
                {
                    throw CreateException(PdfContentParseFailure.InvalidOperand, streamIndex, token.Span.Start.Offset,
                        "The content stream contains an invalid operand.", exception);
                }

                if (operands.Count > options.MaximumOperandCount)
                {
                    throw CreateException(PdfContentParseFailure.LimitExceeded, streamIndex, token.Span.Start.Offset,
                        "The content stream exceeded the configured operand limit.");
                }
                continue;
            }

            reader.Read();
            string code = ReadAscii(source, token.Span);

            if (string.Equals(code, "BI", StringComparison.Ordinal))
            {
                if (operands.Count != 0)
                {
                    throw CreateException(PdfContentParseFailure.OperandCountMismatch, streamIndex,
                        token.Span.Start.Offset, "The BI operator cannot have preceding operands.");
                }

                InlineImageParseResult inlineResult = ParseInlineImage(
                    bytes, source, reader, objectParser, streamIndex, token.Span.Start.Offset);
                reader = CreateReader(source, inlineResult.EndOffset);
                objectParser = new PdfObjectParser(reader, source);
                operationCount = IncrementOperationCount(operationCount, streamIndex, token.Span.Start.Offset);
                operationStart = -1;
                yield return inlineResult.Operation;
                continue;
            }

            PdfGraphicsOperator graphicsOperator = ResolveOperator(code, streamIndex, token.Span.Start.Offset);
            ValidateOperandCount(graphicsOperator, operands.Count, streamIndex, token.Span.Start.Offset);
            ValidateOperandTypes(graphicsOperator, operands, streamIndex, token.Span.Start.Offset);
            UpdateGraphicsStateDepth(code, ref graphicsStateDepth, streamIndex, token.Span.Start.Offset);

            operationCount = IncrementOperationCount(operationCount, streamIndex, token.Span.Start.Offset);
            long start = operationStart < 0 ? token.Span.Start.Offset : operationStart;
            yield return new PdfGraphicsOperation(
                graphicsOperator,
                operands,
                new PdfContentSourceSpan(streamIndex, start, token.Span.End.Offset));

            operands = [];
            operationStart = -1;
        }

        if (operands.Count != 0)
        {
            throw CreateException(PdfContentParseFailure.MalformedContent, streamIndex,
                operationStart < 0 ? bytes.LongLength : operationStart,
                "The content stream ended with operands that were not followed by an operator.");
        }

        if (options.ValidateGraphicsStateBalance && graphicsStateDepth != 0)
        {
            throw CreateException(PdfContentParseFailure.UnbalancedGraphicsState, streamIndex, bytes.LongLength,
                "The content stream ended with an unbalanced graphics-state stack.");
        }
    }

    private InlineImageParseResult ParseInlineImage(
        byte[] bytes,
        MemoryRandomAccessSource source,
        PdfTokenReader reader,
        PdfObjectParser objectParser,
        int streamIndex,
        long startOffset)
    {
        var entries = new List<KeyValuePair<PdfName, PdfObject>>();
        long dataStart;

        while (true)
        {
            PdfToken token = reader.Peek();
            if (token.Kind == PdfTokenKind.EndOfFile)
            {
                throw CreateException(PdfContentParseFailure.UnterminatedInlineImage, streamIndex,
                    token.Span.Start.Offset, "The inline image ended before its ID marker.");
            }

            if (token.Kind == PdfTokenKind.Keyword &&
                string.Equals(ReadAscii(source, token.Span), "ID", StringComparison.Ordinal))
            {
                reader.Read();
                dataStart = SkipInlineImageDataSeparator(bytes, token.Span.End.Offset);
                break;
            }

            if (token.Kind != PdfTokenKind.Name)
            {
                throw CreateException(PdfContentParseFailure.InvalidOperand, streamIndex,
                    token.Span.Start.Offset, "An inline-image dictionary key must be a PDF name.");
            }

            PdfName key = (PdfName)objectParser.Parse();
            PdfToken valueToken = reader.Peek();
            if (valueToken.Kind is PdfTokenKind.EndOfFile or PdfTokenKind.Keyword)
            {
                throw CreateException(PdfContentParseFailure.InvalidOperand, streamIndex,
                    valueToken.Span.Start.Offset, "An inline-image dictionary key is missing its value.");
            }

            PdfObject value = objectParser.Parse();
            entries.Add(new KeyValuePair<PdfName, PdfObject>(key, value));
            if (entries.Count > options.MaximumInlineImageDictionaryEntries)
            {
                throw CreateException(PdfContentParseFailure.LimitExceeded, streamIndex,
                    valueToken.Span.Start.Offset, "The inline-image dictionary exceeded its configured limit.");
            }
        }

        long terminatorOffset = FindInlineImageTerminator(bytes, dataStart, streamIndex);
        long encodedEnd = terminatorOffset - 1;
        long encodedLength = Math.Max(0, encodedEnd - dataStart);
        if (encodedLength > options.MaximumInlineImageBytes)
        {
            throw CreateException(PdfContentParseFailure.LimitExceeded, streamIndex, dataStart,
                "The inline image exceeded the configured encoded-byte limit.");
        }

        int dataStartIndex = checked((int)dataStart);
        int dataLength = checked((int)encodedLength);
        var inlineImage = new PdfInlineImage(new PdfDictionary(entries), bytes.AsSpan(dataStartIndex, dataLength));
        long endOffset = terminatorOffset + 2;

        PdfGraphicsOperator.TryGet("BI", out PdfGraphicsOperator? inlineOperator);
        var operation = new PdfGraphicsOperation(
            inlineOperator!,
            Array.Empty<PdfObject>(),
            new PdfContentSourceSpan(streamIndex, startOffset, endOffset),
            inlineImage);

        return new InlineImageParseResult(operation, endOffset);
    }

    private static long FindInlineImageTerminator(byte[] bytes, long dataStart, int streamIndex)
    {
        for (long index = dataStart + 1; index + 2 < bytes.LongLength; index++)
        {
            int current = checked((int)index);
            if (bytes[current] != (byte)'E' || bytes[current + 1] != (byte)'I')
            {
                continue;
            }

            byte before = bytes[current - 1];
            byte after = bytes[current + 2];
            if (IsWhiteSpace(before) && (IsWhiteSpace(after) || IsDelimiter(after)))
            {
                return index;
            }
        }

        throw new PdfContentParseException(
            PdfContentParseFailure.UnterminatedInlineImage,
            streamIndex,
            dataStart,
            "The inline image does not contain a valid EI terminator.");
    }

    private void UpdateGraphicsStateDepth(string code, ref int depth, int streamIndex, long offset)
    {
        if (!options.ValidateGraphicsStateBalance)
        {
            return;
        }

        if (string.Equals(code, "q", StringComparison.Ordinal))
        {
            depth++;
            if (depth > options.MaximumGraphicsStateDepth)
            {
                throw CreateException(PdfContentParseFailure.LimitExceeded, streamIndex, offset,
                    "The graphics-state stack exceeded its configured depth.");
            }
        }
        else if (string.Equals(code, "Q", StringComparison.Ordinal))
        {
            if (depth == 0)
            {
                throw CreateException(PdfContentParseFailure.UnbalancedGraphicsState, streamIndex, offset,
                    "The content stream restores graphics state without a matching save.");
            }
            depth--;
        }
    }

    private PdfGraphicsOperator ResolveOperator(string code, int streamIndex, long offset)
    {
        if (PdfGraphicsOperator.TryGet(code, out PdfGraphicsOperator? value) && value is not null)
        {
            return value;
        }

        if (options.AllowUnknownOperators)
        {
            return PdfGraphicsOperator.CreateUnknown(code);
        }

        throw CreateException(PdfContentParseFailure.UnsupportedOperator, streamIndex, offset,
            $"The content-stream operator '{code}' is not supported.");
    }

    private static void ValidateOperandCount(PdfGraphicsOperator value, int count, int streamIndex, long offset)
    {
        if (count < value.MinimumOperandCount || count > value.MaximumOperandCount)
        {
            throw CreateException(PdfContentParseFailure.OperandCountMismatch, streamIndex, offset,
                $"Operator '{value.Code}' received {count} operands; expected "
                + (value.MinimumOperandCount == value.MaximumOperandCount
                    ? value.MinimumOperandCount.ToString(System.Globalization.CultureInfo.InvariantCulture)
                    : $"{value.MinimumOperandCount} through {value.MaximumOperandCount}"));
        }
    }

    private static void ValidateOperandTypes(
        PdfGraphicsOperator value,
        List<PdfObject> operands,
        int streamIndex,
        long offset)
    {
        bool valid = value.Code switch
        {
            "cm" or "c" or "d1" or "Tm" => operands.All(IsNumber),
            "m" or "l" or "v" or "y" or "re" or "d0" or "Td" or "TD" or
            "RG" or "rg" or "K" or "k" => operands.All(IsNumber),
            "w" or "J" or "j" or "M" or "i" or "Tc" or "Tw" or "Tz" or
            "TL" or "Tr" or "Ts" or "G" or "g" => IsNumber(operands[0]),
            "Tf" => operands[0] is PdfName && IsNumber(operands[1]),
            "Do" or "gs" or "CS" or "cs" or "sh" or "MP" or "BMC" => operands[0] is PdfName,
            "TJ" => operands[0] is PdfArray,
            "d" => operands[0] is PdfArray && IsNumber(operands[1]),
            _ => true
        };

        if (!valid)
        {
            throw CreateException(PdfContentParseFailure.InvalidOperand, streamIndex, offset,
                $"Operator '{value.Code}' received an operand of an invalid type.");
        }
    }

    private int IncrementOperationCount(int count, int streamIndex, long offset)
    {
        int next = checked(count + 1);
        if (next > options.MaximumOperationCount)
        {
            throw CreateException(PdfContentParseFailure.LimitExceeded, streamIndex, offset,
                "The content stream exceeded the configured operation limit.");
        }
        return next;
    }

    private static PdfTokenReader CreateReader(MemoryRandomAccessSource source, long offset)
    {
        var cursor = new RandomAccessCursor(source, offset);
        return new PdfTokenReader(
            new PdfLexer(
                cursor,
                new PdfLexerOptions
                {
                    AllowUnknownKeywords = true
                }));
    }

    private static string ReadAscii(MemoryRandomAccessSource source, PdfSourceSpan span)
    {
        byte[] bytes = new byte[checked((int)span.Length)];
        int count = source.Read(span.Start.Offset, bytes);
        if (count != bytes.Length)
        {
            throw new EndOfStreamException("The source ended while reading content syntax.");
        }
        return Encoding.ASCII.GetString(bytes);
    }

    private static long SkipInlineImageDataSeparator(byte[] bytes, long offset)
    {
        if (offset >= bytes.LongLength || !IsWhiteSpace(bytes[checked((int)offset)]))
        {
            throw new FormatException("The inline-image ID marker must be followed by whitespace.");
        }

        int index = checked((int)offset);
        if (bytes[index] == 13 && offset + 1 < bytes.LongLength && bytes[index + 1] == 10)
        {
            return offset + 2;
        }
        return offset + 1;
    }

    private static bool IsNumber(PdfObject value) => value is PdfInteger or PdfReal;

    private static bool IsWhiteSpace(byte value) => value is 0 or 9 or 10 or 12 or 13 or 32;

    private static bool IsDelimiter(byte value) => value is (byte)'(' or (byte)')' or (byte)'<' or
        (byte)'>' or (byte)'[' or (byte)']' or (byte)'{' or (byte)'}' or (byte)'/' or (byte)'%';

    private sealed record InlineImageParseResult(
        PdfGraphicsOperation Operation,
        long EndOffset);

    private static PdfContentParseException CreateException(
        PdfContentParseFailure failure,
        int streamIndex,
        long offset,
        string message,
        Exception? innerException = null) =>
        new(failure, streamIndex, offset, message, innerException);
}
