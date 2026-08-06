namespace BlackFlag.Engine.Syntax.Lexing;

public sealed partial class PdfLexer
{
    private PdfToken ReadName()
    {
        PdfSourcePosition start =
            Position;

        ConsumeExpectedByte(
            (byte)'/');

        EnsureTokenLength(
            start);

        while (
            cursor.TryPeekByte(out byte value) &&
            PdfCharacterClassifier.IsRegular(value))
        {
            if (value == (byte)'#')
            {
                ReadNameEscape(
                    start);

                continue;
            }

            ConsumeByte();

            EnsureTokenLength(
                start);
        }

        return CreateToken(
            PdfTokenKind.Name,
            start);
    }

    private void ReadNameEscape(
        PdfSourcePosition tokenStart)
    {
        PdfSourcePosition escapePosition =
            Position;

        ConsumeExpectedByte(
            (byte)'#');

        EnsureTokenLength(
            tokenStart);

        ReadNameEscapeDigit(
            tokenStart,
            escapePosition,
            digitNumber: 1);

        ReadNameEscapeDigit(
            tokenStart,
            escapePosition,
            digitNumber: 2);
    }

    private void ReadNameEscapeDigit(
        PdfSourcePosition tokenStart,
        PdfSourcePosition escapePosition,
        int digitNumber)
    {
        if (!cursor.TryPeekByte(out byte value))
        {
            throw new PdfLexerException(
                string.Concat(
                    "Name escape beginning at offset ",
                    escapePosition.Offset,
                    " is incomplete"),
                escapePosition);
        }

        if (!PdfCharacterClassifier.IsHexadecimalDigit(value))
        {
            throw new PdfLexerException(
                string.Concat(
                    "Name escape contains an invalid hexadecimal digit at digit ",
                    digitNumber),
                Position);
        }

        ConsumeByte();

        EnsureTokenLength(
            tokenStart);
    }
}