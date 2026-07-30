using System.Globalization;

namespace BlackFlag.Engine.Syntax.Lexing;

public sealed partial class PdfLexer
{
    private PdfToken CreateToken(
        PdfTokenKind kind,
        PdfSourcePosition start)
    {
        return new PdfToken(
            kind,
            new PdfSourceSpan(
                start,
                Position));
    }

    private void EnsureTokenLength(
        PdfSourcePosition start)
    {
        long tokenLength =
            Position.Offset - start.Offset;

        if (tokenLength <= Options.MaximumTokenLength)
        {
            return;
        }

        throw new PdfLexerException(
            string.Concat(
                "Token length exceeds the configured maximum of ",
                Options.MaximumTokenLength,
                " bytes"),
            start);
    }

    private static PdfLexerException CreateUnsupportedTokenException(
        byte value,
        PdfSourcePosition position)
    {
        return new PdfLexerException(
            string.Concat(
                "Token recognition has not yet been implemented for byte 0x",
                value.ToString(
                    "X2",
                    CultureInfo.InvariantCulture)),
            position);
    }
}