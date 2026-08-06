using System.Globalization;

namespace BlackFlag.Engine.Syntax.Lexing;

public sealed partial class PdfLexer
{
    private PdfToken ReadDictionaryOrHexadecimalString()
    {
        PdfSourcePosition start =
            Position;

        ConsumeExpectedByte(
            (byte)'<');

        EnsureTokenLength(
            start);

        if (
            TryPeek(out byte value) &&
            value == (byte)'<')
        {
            ConsumeByte();

            EnsureTokenLength(
                start);

            return CreateToken(
                PdfTokenKind.DictionaryStart,
                start);
        }

        return ReadHexadecimalStringContent(
            start);
    }

    private PdfToken ReadHexadecimalStringContent(
        PdfSourcePosition start)
    {
        while (TryPeek(out byte value))
        {
            if (value == (byte)'>')
            {
                ConsumeByte();

                EnsureTokenLength(
                    start);

                return CreateToken(
                    PdfTokenKind.HexadecimalString,
                    start);
            }

            if (
                IsHexadecimalDigit(value) ||
                IsHexadecimalStringWhiteSpace(value))
            {
                ConsumeByte();

                EnsureTokenLength(
                    start);

                continue;
            }

            throw CreateInvalidHexadecimalStringByteException(
                value,
                Position);
        }

        throw new PdfLexerException(
            "Unterminated PDF hexadecimal string",
            start);
    }

    private static bool IsHexadecimalStringWhiteSpace(
        byte value)
    {
        return value is
            0x00 or
            0x09 or
            0x0A or
            0x0C or
            0x0D or
            0x20;
    }

    private static PdfLexerException
        CreateInvalidHexadecimalStringByteException(
            byte value,
            PdfSourcePosition position)
    {
        return new PdfLexerException(
            string.Concat(
                "Invalid byte 0x",
                value.ToString(
                    "X2",
                    CultureInfo.InvariantCulture),
                " in PDF hexadecimal string"),
            position);
    }
}
