using BlackFlag.Engine.Objects;
using BlackFlag.Engine.Syntax.Lexing;
using BlackFlag.Engine.Syntax.Parsing;

namespace BlackFlag.Engine.Parsing;

public sealed partial class PdfObjectParser
{
    private PdfArray ParseArray()
    {
        reader.Expect(PdfTokenKind.ArrayStart);

        var items =
            new List<PdfObject>();

        while (true)
        {
            PdfToken token =
                reader.Peek();

            if (token.Kind == PdfTokenKind.ArrayEnd)
            {
                return items.Count == 0
                    ? PdfArray.Empty
                    : new PdfArray(items);
            }

            if (token.Kind == PdfTokenKind.EndOfFile)
            {
                throw new PdfUnexpectedTokenException(
                    PdfTokenKind.ArrayEnd,
                    token);
            }

            items.Add(Parse());
        }
    }
}
