using BlackFlag.Engine.Objects;
using BlackFlag.Engine.Syntax.Lexing;
using BlackFlag.Engine.Syntax.Parsing;

namespace BlackFlag.Engine.Parsing;

public sealed partial class PdfObjectParser
{
    private PdfDictionary ParseDictionary()
    {
        reader.Expect(PdfTokenKind.DictionaryStart);

        var entries =
            new Dictionary<PdfName, PdfObject>();

        while (true)
        {
            PdfToken token =
                reader.Peek();

            if (token.Kind == PdfTokenKind.DictionaryEnd)
            {
                return entries.Count == 0
                    ? PdfDictionary.Empty
                    : new PdfDictionary(entries);
            }

            if (token.Kind == PdfTokenKind.EndOfFile)
            {
                throw new PdfUnexpectedTokenException(
                    PdfTokenKind.DictionaryEnd,
                    token);
            }

            if (token.Kind != PdfTokenKind.Name)
            {
                throw new PdfUnexpectedTokenException(
                    PdfTokenKind.Name,
                    token);
            }

            PdfName key =
                (PdfName)Parse();

            PdfObject value =
                Parse();

            entries[key] = value;
        }
    }
}

