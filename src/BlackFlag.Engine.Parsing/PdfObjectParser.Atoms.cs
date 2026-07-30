using System.Text;
using BlackFlag.Engine.Objects;
using BlackFlag.Engine.Syntax.Lexing;

namespace BlackFlag.Engine.Parsing;

public sealed partial class PdfObjectParser
{
    private PdfName ParseName(
        PdfToken token)
    {
        byte[] tokenBytes =
            ReadTokenBytes(token);

        if (
            tokenBytes.Length == 0 ||
            tokenBytes[0] != (byte)'/')
        {
            throw CreateTokenFormatException(
                token,
                "A PDF name token must begin with a solidus.");
        }

        var decodedBytes =
            new List<byte>(
                Math.Max(
                    tokenBytes.Length - 1,
                    0));

        int index = 1;

        while (index < tokenBytes.Length)
        {
            byte value =
                tokenBytes[index];

            if (value != (byte)'#')
            {
                decodedBytes.Add(value);
                index++;
                continue;
            }

            if (index + 2 >= tokenBytes.Length)
            {
                throw CreateTokenFormatException(
                    token,
                    "A PDF name escape must contain two hexadecimal digits.");
            }

            decodedBytes.Add(
                DecodeHexadecimalByte(
                    tokenBytes[index + 1],
                    tokenBytes[index + 2],
                    token));

            index += 3;
        }

        return new PdfName(
            Encoding.Latin1.GetString(
                decodedBytes.ToArray()));
    }
}
