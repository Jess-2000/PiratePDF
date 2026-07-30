using System.Text;
using BlackFlag.Engine.Objects;
using BlackFlag.Engine.Syntax.Lexing;

namespace BlackFlag.Engine.Parsing;

public sealed partial class PdfObjectParser
{
    private PdfObject ParseLiteralString(
        PdfToken token)
    {
        byte[] tokenBytes =
            ReadTokenBytes(token);

        if (
            tokenBytes.Length < 2 ||
            tokenBytes[0] != (byte)'(' ||
            tokenBytes[^1] != (byte)')')
        {
            throw CreateTokenFormatException(
                token,
                "A PDF literal-string token must be parenthesis-delimited.");
        }

        var decodedBytes =
            new List<byte>(
                tokenBytes.Length - 2);

        int index = 1;
        int contentEnd = tokenBytes.Length - 1;

        while (index < contentEnd)
        {
            byte value =
                tokenBytes[index++];

            if (value == (byte)'\\')
            {
                DecodeLiteralStringEscape(
                    tokenBytes,
                    contentEnd,
                    ref index,
                    decodedBytes,
                    token);

                continue;
            }

            if (value == (byte)'\r')
            {
                if (
                    index < contentEnd &&
                    tokenBytes[index] == (byte)'\n')
                {
                    index++;
                }

                decodedBytes.Add((byte)'\n');
                continue;
            }

            decodedBytes.Add(value);
        }

        return new PdfLiteralString(
            Encoding.Latin1.GetString(
                decodedBytes.ToArray()));
    }

    private PdfObject ParseHexadecimalString(
        PdfToken token)
    {
        byte[] tokenBytes =
            ReadTokenBytes(token);

        if (
            tokenBytes.Length < 2 ||
            tokenBytes[0] != (byte)'<' ||
            tokenBytes[^1] != (byte)'>')
        {
            throw CreateTokenFormatException(
                token,
                "A PDF hexadecimal-string token must be angle-bracket-delimited.");
        }

        var decodedBytes =
            new List<byte>(
                Math.Max(
                    (tokenBytes.Length - 2) / 2,
                    0));

        int? highNibble = null;

        for (
            int index = 1;
            index < tokenBytes.Length - 1;
            index++)
        {
            byte value =
                tokenBytes[index];

            if (IsPdfWhiteSpace(value))
            {
                continue;
            }

            int nibble =
                DecodeHexadecimalNibble(
                    value,
                    token);

            if (highNibble is null)
            {
                highNibble = nibble;
                continue;
            }

            decodedBytes.Add(
                checked(
                    (byte)(
                        (highNibble.Value << 4) |
                        nibble)));

            highNibble = null;
        }

        if (highNibble is not null)
        {
            decodedBytes.Add(
                checked(
                    (byte)(
                        highNibble.Value << 4)));
        }

        return new PdfHexString(
            decodedBytes.ToArray());
    }

    private static void DecodeLiteralStringEscape(
        byte[] tokenBytes,
        int contentEnd,
        ref int index,
        List<byte> decodedBytes,
        PdfToken token)
    {
        if (index >= contentEnd)
        {
            throw CreateTokenFormatException(
                token,
                "A PDF literal-string escape is incomplete.");
        }

        byte escapedValue =
            tokenBytes[index++];

        switch (escapedValue)
        {
            case (byte)'n':
                decodedBytes.Add((byte)'\n');
                return;

            case (byte)'r':
                decodedBytes.Add((byte)'\r');
                return;

            case (byte)'t':
                decodedBytes.Add((byte)'\t');
                return;

            case (byte)'b':
                decodedBytes.Add((byte)'\b');
                return;

            case (byte)'f':
                decodedBytes.Add((byte)'\f');
                return;

            case (byte)'(':
            case (byte)')':
            case (byte)'\\':
                decodedBytes.Add(escapedValue);
                return;

            case (byte)'\r':
                if (
                    index < contentEnd &&
                    tokenBytes[index] == (byte)'\n')
                {
                    index++;
                }

                return;

            case (byte)'\n':
                return;
        }

        if (!IsOctalDigit(escapedValue))
        {
            decodedBytes.Add(escapedValue);
            return;
        }

        int octalValue =
            escapedValue - (byte)'0';

        int additionalDigits = 0;

        while (
            additionalDigits < 2 &&
            index < contentEnd &&
            IsOctalDigit(tokenBytes[index]))
        {
            octalValue =
                (octalValue * 8) +
                (tokenBytes[index] - (byte)'0');

            index++;
            additionalDigits++;
        }

        decodedBytes.Add(
            unchecked(
                (byte)octalValue));
    }

    private static bool IsOctalDigit(
        byte value)
    {
        return value is >= (byte)'0' and <= (byte)'7';
    }
}
