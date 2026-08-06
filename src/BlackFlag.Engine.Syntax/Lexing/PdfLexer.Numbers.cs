using System.Globalization;

namespace BlackFlag.Engine.Syntax.Lexing;

public sealed partial class PdfLexer
{
    private PdfToken ReadNumber()
    {
        PdfSourcePosition start =
            Position;

        ConsumeOptionalNumberSign(
            start);

        bool hasDigits =
            ConsumeDecimalDigits(
                start);

        bool hasDecimalPoint =
            ConsumeOptionalDecimalPoint(
                start);

        if (hasDecimalPoint)
        {
            hasDigits =
                ConsumeDecimalDigits(
                    start) ||
                hasDigits;
        }

        if (!hasDigits)
        {
            if (
                TryPeek(out byte invalidByte) &&
                !PdfCharacterClassifier.IsTokenBoundary(
                    invalidByte))
            {
                throw CreateInvalidNumberByteException(
                    invalidByte);
            }

            throw new PdfLexerException(
                "A PDF number must contain at least one decimal digit",
                start);
        }

        ValidateNumberBoundary();

        PdfTokenKind kind =
            hasDecimalPoint
                ? PdfTokenKind.RealNumber
                : PdfTokenKind.IntegerNumber;

        return CreateToken(
            kind,
            start);
    }

    private void ConsumeOptionalNumberSign(
        PdfSourcePosition tokenStart)
    {
        if (
            !TryPeek(out byte value) ||
            value is not ((byte)'+' or (byte)'-'))
        {
            return;
        }

        ConsumeByte();

        EnsureTokenLength(
            tokenStart);
    }

    private bool ConsumeDecimalDigits(
        PdfSourcePosition tokenStart)
    {
        bool consumedDigit =
            false;

        while (
            TryPeek(out byte value) &&
            PdfCharacterClassifier.IsDecimalDigit(
                value))
        {
            ConsumeByte();

            EnsureTokenLength(
                tokenStart);

            consumedDigit =
                true;
        }

        return consumedDigit;
    }

    private bool ConsumeOptionalDecimalPoint(
        PdfSourcePosition tokenStart)
    {
        if (
            !TryPeek(out byte value) ||
            value != (byte)'.')
        {
            return false;
        }

        ConsumeByte();

        EnsureTokenLength(
            tokenStart);

        return true;
    }

    private void ValidateNumberBoundary()
    {
        if (
            !TryPeek(out byte value) ||
            PdfCharacterClassifier.IsTokenBoundary(
                value))
        {
            return;
        }

        throw CreateInvalidNumberByteException(
            value);
    }

    private PdfLexerException CreateInvalidNumberByteException(
        byte value)
    {
        return new PdfLexerException(
            string.Concat(
                "Invalid byte 0x",
                value.ToString(
                    "X2",
                    CultureInfo.InvariantCulture),
                " in PDF number"),
            Position);
    }
}