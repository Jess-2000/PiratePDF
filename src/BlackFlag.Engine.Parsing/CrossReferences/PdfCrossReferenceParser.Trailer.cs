using BlackFlag.Engine.IO.RandomAccess;
using BlackFlag.Engine.Objects;
using BlackFlag.Engine.Syntax.Lexing;
using BlackFlag.Engine.Syntax.Parsing;

namespace BlackFlag.Engine.Parsing.CrossReferences;

public sealed partial class PdfCrossReferenceParser
{
    private PdfDictionary ParseTrailerDictionary(
        long offset)
    {
        using var cursor =
            new BufferedRandomAccessCursor(
                source,
                initialPosition: offset);

        var lexer =
            new PdfLexer(cursor);

        var reader =
            new PdfTokenReader(lexer);

        var objectParser =
            new PdfObjectParser(
                reader,
                source);

        PdfObject value =
            objectParser.Parse();

        if (value is not PdfDictionary trailer)
        {
            throw new FormatException(
                $"The trailer at offset {offset} must be followed by a PDF "
                + "dictionary.");
        }

        return trailer;
    }
}
