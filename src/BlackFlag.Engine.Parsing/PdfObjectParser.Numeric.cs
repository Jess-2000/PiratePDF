using System.Globalization;
using System.Text;
using BlackFlag.Engine.Objects;
using BlackFlag.Engine.Syntax.Lexing;

namespace BlackFlag.Engine.Parsing;

public sealed partial class PdfObjectParser
{
    private PdfInteger ParseInteger(
        PdfToken token)
    {
        string text =
            Encoding.ASCII.GetString(
                ReadTokenBytes(token));

        if (
            !long.TryParse(
                text,
                NumberStyles.AllowLeadingSign,
                CultureInfo.InvariantCulture,
                out long value))
        {
            throw new FormatException(
                string.Concat(
                    "The integer token at offset ",
                    token.Span.Start.Offset,
                    " is outside the supported Int64 range."));
        }

        return new PdfInteger(value);
    }

    private PdfReal ParseReal(
        PdfToken token)
    {
        string text =
            Encoding.ASCII.GetString(
                ReadTokenBytes(token));

        const NumberStyles styles =
            NumberStyles.AllowLeadingSign |
            NumberStyles.AllowDecimalPoint;

        if (
            !double.TryParse(
                text,
                styles,
                CultureInfo.InvariantCulture,
                out double value) ||
            !double.IsFinite(value))
        {
            throw new FormatException(
                string.Concat(
                    "The real-number token at offset ",
                    token.Span.Start.Offset,
                    " cannot be represented as a finite Double value."));
        }

        return new PdfReal(value);
    }
}

