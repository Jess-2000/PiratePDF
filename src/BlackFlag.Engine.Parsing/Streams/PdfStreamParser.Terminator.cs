using BlackFlag.Engine.IO.RandomAccess;
using BlackFlag.Engine.Syntax.Lexing;

namespace BlackFlag.Engine.Parsing.Streams;

internal sealed partial class PdfStreamParser
{
    private static ReadOnlySpan<byte> StreamEndKeyword =>
        "endstream"u8;

    private long ReadStreamEndOffset(
        long dataEndOffset)
    {
        long keywordOffset =
            ReadRequiredLineEnding(
                dataEndOffset);

        for (int index = 0; index < StreamEndKeyword.Length; index++)
        {
            long sourceOffset =
                checked(keywordOffset + index);

            byte actual =
                ReadByte(
                    sourceOffset,
                    "The source ended before the endstream keyword was complete.");

            if (actual != StreamEndKeyword[index])
            {
                throw new FormatException(
                    $"Expected the endstream keyword at offset {keywordOffset}.");
            }
        }

        long endOffset =
            checked(
                keywordOffset +
                StreamEndKeyword.Length);

        if (
            TryReadByte(
                endOffset,
                out byte following) &&
            !IsDelimiterOrWhiteSpace(following))
        {
            throw new FormatException(
                "The endstream keyword is not followed by a valid PDF delimiter.");
        }

        return endOffset;
    }

    private long ReadRequiredLineEnding(
        long offset)
    {
        byte first =
            ReadByte(
                offset,
                "The source ended before the endstream line ending.");

        if (first == 0x0A)
        {
            return checked(offset + 1);
        }

        if (first != 0x0D)
        {
            throw new FormatException(
                "PDF stream data must be followed by an end-of-line marker before endstream.");
        }

        long nextOffset =
            checked(offset + 1);

        if (
            TryReadByte(
                nextOffset,
                out byte second) &&
            second == 0x0A)
        {
            return checked(nextOffset + 1);
        }

        return nextOffset;
    }

    private void ResetReader(
        long offset)
    {
        var cursor =
            new RandomAccessCursor(
                source,
                initialPosition: offset);

        var lexer =
            new PdfLexer(cursor);

        reader.Reset(lexer);
    }

    private byte ReadByte(
        long offset,
        string endOfStreamMessage)
    {
        if (!TryReadByte(offset, out byte value))
        {
            throw new EndOfStreamException(
                endOfStreamMessage);
        }

        return value;
    }

    private bool TryReadByte(
        long offset,
        out byte value)
    {
        if (
            offset < 0 ||
            offset >= source.Length)
        {
            value = default;
            return false;
        }

        Span<byte> destination =
            stackalloc byte[1];

        int bytesRead =
            source.Read(
                offset,
                destination);

        if (bytesRead is < 0 or > 1)
        {
            throw new InvalidDataException(
                "The random-access source returned an invalid byte count.");
        }

        if (bytesRead == 0)
        {
            value = default;
            return false;
        }

        value = destination[0];
        return true;
    }

    private static bool IsDelimiterOrWhiteSpace(
        byte value)
    {
        return value is
            0x00 or
            0x09 or
            0x0A or
            0x0C or
            0x0D or
            0x20 or
            (byte)'(' or
            (byte)')' or
            (byte)'<' or
            (byte)'>' or
            (byte)'[' or
            (byte)']' or
            (byte)'{' or
            (byte)'}' or
            (byte)'/' or
            (byte)'%';
    }
}
