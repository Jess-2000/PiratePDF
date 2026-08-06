using BlackFlag.Engine.Syntax.Lexing;

namespace BlackFlag.Engine.Parsing.Streams;

internal sealed partial class PdfStreamParser
{
    private long ReadDataOffset(
        PdfToken streamStartToken)
    {
        long offset =
            streamStartToken.Span.End.Offset;

        byte first =
            ReadByte(
                offset,
                "The source ended after the stream keyword.");

        if (first == 0x0A)
        {
            return checked(offset + 1);
        }

        if (first != 0x0D)
        {
            throw new FormatException(
                "The PDF stream keyword must be followed by an end-of-line marker.");
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
}

