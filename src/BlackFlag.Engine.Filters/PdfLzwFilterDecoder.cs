using System.Buffers;
using BlackFlag.Engine.Objects;

namespace BlackFlag.Engine.Filters;

/// <summary>
/// Decodes PDF <c>/LZWDecode</c> streams.
/// </summary>
public sealed class PdfLzwFilterDecoder : IPdfFilterDecoder
{
    private const int ClearCode = 256;
    private const int EndOfDataCode = 257;
    private const int FirstDynamicCode = 258;
    private const int MaximumCode = 4095;

    private static readonly PdfName FilterName =
        new("LZWDecode");

    /// <inheritdoc />
    public PdfName Name => FilterName;

    /// <inheritdoc />
    public void Decode(
        ReadOnlySpan<byte> encodedData,
        IBufferWriter<byte> destination,
        PdfFilterDecodeContext context)
    {
        ArgumentNullException.ThrowIfNull(destination);
        ArgumentNullException.ThrowIfNull(context);

        PdfLzwDecodeParameters parameters =
            PdfLzwDecodeParameters.FromDictionary(
                context.Parameters);

        using var decompressed =
            new PooledByteAccumulator(
                context.Budget.Limits.BufferSize,
                context.Budget.Limits.MaximumDecodedBytes,
                encodedData.Length,
                context.Budget.Limits.MaximumExpansionRatio);

        DecodeCodes(
            encodedData,
            decompressed,
            parameters.EarlyChange,
            context.Budget);

        PdfPredictorDecoder.Decode(
            decompressed.AsSpan(),
            destination,
            parameters.PredictorParameters,
            context.Budget);
    }

    private static void DecodeCodes(
        ReadOnlySpan<byte> encodedData,
        PooledByteAccumulator destination,
        int earlyChange,
        IPdfFilterDecodeBudget budget)
    {
        if (encodedData.IsEmpty)
        {
            throw new PdfFilterDecodeException(
                PdfFilterDecodeFailure.InvalidData,
                "An LZWDecode stream cannot be empty.");
        }

        var prefixes =
            new short[MaximumCode + 1];

        var suffixes =
            new byte[MaximumCode + 1];

        Span<byte> stack =
            stackalloc byte[MaximumCode + 1];

        InitializeDictionary(
            prefixes,
            suffixes);

        var reader =
            new PdfLzwBitReader(
                encodedData);

        int codeWidth = 9;
        int nextCode = FirstDynamicCode;
        int previousCode = -1;
        bool terminated = false;

        while (
            reader.TryRead(
                codeWidth,
                out int code))
        {
            budget.ThrowIfCancellationRequested();

            if (code == ClearCode)
            {
                InitializeDictionary(
                    prefixes,
                    suffixes);

                codeWidth = 9;
                nextCode = FirstDynamicCode;
                previousCode = -1;
                continue;
            }

            if (code == EndOfDataCode)
            {
                terminated = true;
                break;
            }

            byte firstByte;
            int currentCode = code;

            if (code < nextCode)
            {
                int count =
                    ExpandCode(
                        code,
                        prefixes,
                        suffixes,
                        stack,
                        out firstByte);

                destination.Append(
                    stack[..count]);
            }
            else if (
                code == nextCode &&
                previousCode >= 0)
            {
                int count =
                    ExpandCode(
                        previousCode,
                        prefixes,
                        suffixes,
                        stack,
                        out firstByte);

                destination.Append(
                    stack[..count]);

                destination.Append(
                    [firstByte]);
            }
            else
            {
                throw new PdfFilterDecodeException(
                    PdfFilterDecodeFailure.InvalidData,
                    $"LZWDecode encountered invalid code {code}.");
            }

            if (
                previousCode >= 0 &&
                nextCode <= MaximumCode)
            {
                prefixes[nextCode] =
                    checked(
                        (short)previousCode);

                suffixes[nextCode] =
                    firstByte;

                nextCode++;

                if (
                    codeWidth < 12 &&
                    nextCode ==
                    (1 << codeWidth) -
                    earlyChange)
                {
                    codeWidth++;
                }
            }

            previousCode =
                currentCode;
        }

        if (!terminated)
        {
            throw new PdfFilterDecodeException(
                PdfFilterDecodeFailure.InvalidData,
                "LZWDecode data is missing the end-of-data code.");
        }
    }

    private static int ExpandCode(
        int code,
        ReadOnlySpan<short> prefixes,
        ReadOnlySpan<byte> suffixes,
        Span<byte> output,
        out byte firstByte)
    {
        int count = 0;
        int current = code;

        while (current >= 256)
        {
            if (
                current < FirstDynamicCode ||
                current > MaximumCode ||
                count >= output.Length)
            {
                throw new PdfFilterDecodeException(
                    PdfFilterDecodeFailure.InvalidData,
                    "LZWDecode contains an invalid dictionary chain.");
            }

            output[count++] =
                suffixes[current];

            current =
                prefixes[current];
        }

        if (
            current < 0 ||
            current > 255 ||
            count >= output.Length)
        {
            throw new PdfFilterDecodeException(
                PdfFilterDecodeFailure.InvalidData,
                "LZWDecode contains an invalid literal code.");
        }

        firstByte =
            (byte)current;

        output[count++] =
            firstByte;

        output[..count].Reverse();

        return count;
    }

    private static void InitializeDictionary(
        Span<short> prefixes,
        Span<byte> suffixes)
    {
        prefixes.Fill(-1);
        suffixes.Clear();

        for (int code = 0; code < 256; code++)
        {
            suffixes[code] =
                (byte)code;
        }
    }
}

