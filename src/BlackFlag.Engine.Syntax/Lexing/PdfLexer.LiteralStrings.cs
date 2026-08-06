namespace BlackFlag.Engine.Syntax.Lexing;

public sealed partial class PdfLexer
{
    private PdfToken ReadLiteralString()
    {
        PdfSourcePosition start =
            Position;

        ConsumeExpectedByte(
            (byte)'(');

        EnsureTokenLength(
            start);

        int nestingDepth =
            1;

        while (TryPeek(out byte value))
        {
            if (value == (byte)'\\')
            {
                ConsumeLiteralStringEscape(
                    start);

                continue;
            }

            ConsumeByte();

            EnsureTokenLength(
                start);

            if (value == (byte)'(')
            {
                nestingDepth++;

                continue;
            }

            if (value != (byte)')')
            {
                continue;
            }

            nestingDepth--;

            if (nestingDepth == 0)
            {
                return CreateToken(
                    PdfTokenKind.LiteralString,
                    start);
            }
        }

        throw new PdfLexerException(
            "Unterminated PDF literal string",
            start);
    }

    private void ConsumeLiteralStringEscape(
        PdfSourcePosition tokenStart)
    {
        ConsumeExpectedByte(
            (byte)'\\');

        EnsureTokenLength(
            tokenStart);

        if (!TryPeek(out byte escapedByte))
        {
            throw new PdfLexerException(
                "Unterminated escape sequence in PDF literal string",
                Position);
        }

        if (escapedByte == (byte)'\r')
        {
            ConsumeByte();

            EnsureTokenLength(
                tokenStart);

            if (
                TryPeek(out byte followingByte) &&
                followingByte == (byte)'\n')
            {
                ConsumeByte();

                EnsureTokenLength(
                    tokenStart);
            }

            return;
        }

        if (escapedByte == (byte)'\n')
        {
            ConsumeByte();

            EnsureTokenLength(
                tokenStart);

            return;
        }

        if (IsOctalDigit(escapedByte))
        {
            ConsumeLiteralStringOctalEscape(
                tokenStart);

            return;
        }

        ConsumeByte();

        EnsureTokenLength(
            tokenStart);
    }

    private void ConsumeLiteralStringOctalEscape(
        PdfSourcePosition tokenStart)
    {
        const int maximumOctalDigits = 3;

        int consumedDigits =
            0;

        while (
            consumedDigits < maximumOctalDigits &&
            TryPeek(out byte value) &&
            IsOctalDigit(value))
        {
            ConsumeByte();

            EnsureTokenLength(
                tokenStart);

            consumedDigits++;
        }
    }

    private static bool IsOctalDigit(
        byte value)
    {
        return value is >= (byte)'0' and <= (byte)'7';
    }
}
