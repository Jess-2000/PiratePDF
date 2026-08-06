using BlackFlag.Engine.Objects;
using BlackFlag.Engine.Syntax.Lexing;

namespace BlackFlag.Engine.Parsing;

public sealed partial class PdfObjectParser
{
    private byte[] ReadTokenBytes(
        PdfToken token)
    {
        if (token.Span.Length > int.MaxValue)
        {
            throw new NotSupportedException(
                "A single parser token cannot exceed Int32.MaxValue bytes.");
        }

        int byteCount =
            checked(
                (int)token.Span.Length);

        byte[] bytes =
            GC.AllocateUninitializedArray<byte>(
                byteCount);

        int totalBytesRead = 0;

        while (totalBytesRead < bytes.Length)
        {
            int bytesRead =
                source.Read(
                    checked(
                        token.Span.Start.Offset +
                        totalBytesRead),
                    bytes.AsSpan(totalBytesRead));

            if (
                bytesRead < 0 ||
                bytesRead > bytes.Length - totalBytesRead)
            {
                throw new InvalidDataException(
                    "The random-access source returned an invalid byte count.");
            }

            if (bytesRead == 0)
            {
                throw new EndOfStreamException(
                    string.Concat(
                        "The source ended before the token at offset ",
                        token.Span.Start.Offset,
                        " could be read completely."));
            }

            totalBytesRead += bytesRead;
        }

        return bytes;
    }

    private static byte DecodeHexadecimalByte(
        byte highDigit,
        byte lowDigit,
        PdfToken token)
    {
        return checked(
            (byte)(
                (DecodeHexadecimalNibble(
                    highDigit,
                    token) << 4) |
                DecodeHexadecimalNibble(
                    lowDigit,
                    token)));
    }

    private static int DecodeHexadecimalNibble(
        byte value,
        PdfToken token)
    {
        if (value is >= (byte)'0' and <= (byte)'9')
        {
            return value - (byte)'0';
        }

        if (value is >= (byte)'A' and <= (byte)'F')
        {
            return value - (byte)'A' + 10;
        }

        if (value is >= (byte)'a' and <= (byte)'f')
        {
            return value - (byte)'a' + 10;
        }

        throw CreateTokenFormatException(
            token,
            "The token contains an invalid hexadecimal digit.");
    }

    private static bool IsPdfWhiteSpace(
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

    private static FormatException CreateTokenFormatException(
        PdfToken token,
        string message)
    {
        return new FormatException(
            string.Concat(
                message,
                " Offset: ",
                token.Span.Start.Offset,
                "."));
    }
}

