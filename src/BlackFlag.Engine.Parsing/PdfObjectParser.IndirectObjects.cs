using BlackFlag.Engine.Objects;
using BlackFlag.Engine.Parsing.Streams;
using BlackFlag.Engine.Syntax.Lexing;
using BlackFlag.Engine.Syntax.Parsing;

namespace BlackFlag.Engine.Parsing;

public sealed partial class PdfObjectParser
{
    internal PdfIndirectObject ParseIndirectObject()
    {
        PdfToken objectNumberToken =
            reader.Peek();

        if (objectNumberToken.Kind != PdfTokenKind.IntegerNumber)
        {
            throw new PdfUnexpectedTokenException(
                PdfTokenKind.IntegerNumber,
                objectNumberToken);
        }

        PdfToken generationNumberToken =
            reader.Peek(1);

        if (generationNumberToken.Kind != PdfTokenKind.IntegerNumber)
        {
            throw new PdfUnexpectedTokenException(
                PdfTokenKind.IntegerNumber,
                generationNumberToken);
        }

        PdfToken objectStartToken =
            reader.Peek(2);

        if (objectStartToken.Kind != PdfTokenKind.ObjectStart)
        {
            throw new PdfUnexpectedTokenException(
                PdfTokenKind.ObjectStart,
                objectStartToken);
        }

        int objectNumber =
            ParseObjectNumber(
                objectNumberToken);

        int generationNumber =
            ParseGenerationNumber(
                generationNumberToken);

        reader.Read();
        reader.Read();
        reader.Read();

        PdfObject value =
            Parse();

        if (
            value is PdfDictionary dictionary &&
            reader.Peek().Kind == PdfTokenKind.StreamStart)
        {
            var streamParser =
                new PdfStreamParser(
                    reader,
                    source);

            value =
                streamParser.Parse(
                    dictionary);
        }

        reader.Expect(
            PdfTokenKind.ObjectEnd);

        return new PdfIndirectObject(
            objectNumber,
            generationNumber,
            value);
    }
}

