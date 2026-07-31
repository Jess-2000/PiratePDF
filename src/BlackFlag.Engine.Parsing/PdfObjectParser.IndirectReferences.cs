using BlackFlag.Engine.Objects;
using BlackFlag.Engine.Syntax.Lexing;

namespace BlackFlag.Engine.Parsing;

public sealed partial class PdfObjectParser
{
    private bool IsIndirectReference()
    {
        return
            reader.Peek(1).Kind == PdfTokenKind.IntegerNumber &&
            reader.Peek(2).Kind == PdfTokenKind.IndirectReferenceMarker;
    }

    private PdfIndirectReference ParseIndirectReference()
    {
        PdfToken objectNumberToken =
            reader.Peek();

        PdfToken generationNumberToken =
            reader.Peek(1);

        int objectNumber =
            ParseObjectNumber(
                objectNumberToken);

        int generationNumber =
            ParseGenerationNumber(
                generationNumberToken);

        reader.Read();
        reader.Read();

        return new PdfIndirectReference(
            objectNumber,
            generationNumber);
    }

    private int ParseObjectNumber(
        PdfToken token)
    {
        long value =
            ParseInteger(token).Value;

        if (value is < 1 or > int.MaxValue)
        {
            throw CreateTokenFormatException(
                token,
                "A PDF object number must be between 1 and Int32.MaxValue.");
        }

        return checked((int)value);
    }

    private int ParseGenerationNumber(
        PdfToken token)
    {
        long value =
            ParseInteger(token).Value;

        if (value is < 0 or > 65535)
        {
            throw CreateTokenFormatException(
                token,
                "A PDF generation number must be between 0 and 65535.");
        }

        return checked((int)value);
    }
}
