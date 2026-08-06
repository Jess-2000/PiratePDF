using BlackFlag.Engine.Abstractions.IO;
using BlackFlag.Engine.Objects;
using BlackFlag.Engine.Syntax.Lexing;
using BlackFlag.Engine.Syntax.Parsing;

namespace BlackFlag.Engine.Parsing.Streams;

internal sealed partial class PdfStreamParser
{
    private static readonly PdfName LengthKey =
        new("Length");

    private readonly PdfTokenReader reader;
    private readonly IRandomAccessSource source;

    public PdfStreamParser(
        PdfTokenReader reader,
        IRandomAccessSource source)
    {
        ArgumentNullException.ThrowIfNull(reader);
        ArgumentNullException.ThrowIfNull(source);

        this.reader = reader;
        this.source = source;
    }

    public PdfStream Parse(
        PdfDictionary dictionary)
    {
        ArgumentNullException.ThrowIfNull(dictionary);

        int length =
            GetLength(dictionary);

        PdfToken streamStartToken =
            reader.Expect(
                PdfTokenKind.StreamStart);

        long dataOffset =
            ReadDataOffset(
                streamStartToken);

        byte[] data =
            ReadData(
                dataOffset,
                length);

        long streamEndOffset =
            ReadStreamEndOffset(
                checked(dataOffset + length));

        ResetReader(
            streamEndOffset);

        return new PdfStream(
            dictionary,
            data);
    }

    private static int GetLength(
        PdfDictionary dictionary)
    {
        if (!dictionary.TryGetValue(
                LengthKey,
                out PdfObject lengthValue))
        {
            throw new FormatException(
                "A PDF stream dictionary must contain a /Length entry.");
        }

        if (lengthValue is not PdfInteger lengthInteger)
        {
            throw new FormatException(
                "A PDF stream /Length entry must be a direct integer.");
        }

        if (lengthInteger.Value < 0)
        {
            throw new FormatException(
                "A PDF stream /Length value cannot be negative.");
        }

        if (lengthInteger.Value > int.MaxValue)
        {
            throw new NotSupportedException(
                "A PDF stream larger than Int32.MaxValue bytes is not supported.");
        }

        return checked(
            (int)lengthInteger.Value);
    }
}
