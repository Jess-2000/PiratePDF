namespace BlackFlag.Engine.Services.Text;

/// <summary>
/// Provides the shared PDF string scanning, decoding, and encoding rules used
/// by direct Tj and positioned TJ existing-text mutation.
/// </summary>
internal static class PdfTextStringTokenCodec
{
    public static int SkipComment(ReadOnlySpan<byte> bytes, int start) =>
        SkipComment(bytes, start, bytes.Length);

    public static int SkipComment(ReadOnlySpan<byte> bytes, int start, int end)
    {
        int index = start + 1;
        while (index < end && bytes[index] is not (byte)'\r' and not (byte)'\n')
        {
            index++;
        }

        return index;
    }

    public static int FindLiteralStringEnd(ReadOnlySpan<byte> bytes, int start) =>
        FindLiteralStringEndCore(
            bytes,
            start,
            bytes.Length,
            "The existing-text literal string is unterminated.");

    public static int FindLiteralStringEnd(ReadOnlySpan<byte> bytes, int start, int end) =>
        FindLiteralStringEndCore(
            bytes,
            start,
            end,
            "A positioned-text literal string is unterminated.");

    public static int FindHexadecimalStringEnd(ReadOnlySpan<byte> bytes, int start) =>
        FindHexadecimalStringEndCore(
            bytes,
            start,
            bytes.Length,
            "The existing-text hexadecimal string is unterminated.");

    public static int FindHexadecimalStringEnd(ReadOnlySpan<byte> bytes, int start, int end) =>
        FindHexadecimalStringEndCore(
            bytes,
            start,
            end,
            "A positioned-text hexadecimal string is unterminated.");

    private static int FindLiteralStringEndCore(
        ReadOnlySpan<byte> bytes,
        int start,
        int end,
        string errorMessage)
    {
        int depth = 1;
        int index = start + 1;
        while (index < end)
        {
            byte value = bytes[index++];
            if (value == (byte)'\\')
            {
                if (index >= end)
                {
                    break;
                }

                if (bytes[index] == (byte)'\r')
                {
                    index++;
                    if (index < end && bytes[index] == (byte)'\n')
                    {
                        index++;
                    }
                    continue;
                }

                index++;
                continue;
            }

            if (value == (byte)'(')
            {
                depth++;
                continue;
            }

            if (value != (byte)')')
            {
                continue;
            }

            if (--depth == 0)
            {
                return index;
            }
        }

        throw new InvalidOperationException(errorMessage);
    }

    private static int FindHexadecimalStringEndCore(
        ReadOnlySpan<byte> bytes,
        int start,
        int end,
        string errorMessage)
    {
        for (int index = start + 1; index < end; index++)
        {
            if (bytes[index] == (byte)'>')
            {
                return index + 1;
            }
        }

        throw new InvalidOperationException(errorMessage);
    }

    public static byte[] DecodeStringToken(ReadOnlySpan<byte> token)
    {
        if (token.Length < 2)
        {
            throw new InvalidOperationException("The existing-text source string token is malformed.");
        }

        return token[0] switch
        {
            (byte)'(' => DecodeLiteralString(token),
            (byte)'<' => DecodeHexadecimalString(token),
            _ => throw new InvalidOperationException(
                "The existing-text source token is not a supported PDF string.")
        };
    }

    public static byte[] EncodeLiteralString(ReadOnlySpan<byte> bytes)
    {
        using var destination = new MemoryStream();
        destination.WriteByte((byte)'(');
        foreach (byte item in bytes)
        {
            switch (item)
            {
                case (byte)'\n':
                    WriteEscape(destination, (byte)'n');
                    break;
                case (byte)'\r':
                    WriteEscape(destination, (byte)'r');
                    break;
                case (byte)'\t':
                    WriteEscape(destination, (byte)'t');
                    break;
                case 8:
                    WriteEscape(destination, (byte)'b');
                    break;
                case 12:
                    WriteEscape(destination, (byte)'f');
                    break;
                case (byte)'(':
                case (byte)')':
                case (byte)'\\':
                    destination.WriteByte((byte)'\\');
                    destination.WriteByte(item);
                    break;
                default:
                    if (item is >= 32 and <= 126)
                    {
                        destination.WriteByte(item);
                    }
                    else
                    {
                        WriteOctalByte(destination, item);
                    }
                    break;
            }
        }

        destination.WriteByte((byte)')');
        return destination.ToArray();
    }

    public static byte[] EncodeHexadecimalString(ReadOnlySpan<byte> bytes)
    {
        const string Hex = "0123456789ABCDEF";
        byte[] result = new byte[checked(2 + (bytes.Length * 2))];
        result[0] = (byte)'<';
        int index = 1;
        foreach (byte item in bytes)
        {
            result[index++] = (byte)Hex[item >> 4];
            result[index++] = (byte)Hex[item & 0x0F];
        }

        result[index] = (byte)'>';
        return result;
    }

    private static byte[] DecodeLiteralString(ReadOnlySpan<byte> token)
    {
        if (token[^1] != (byte)')')
        {
            throw new InvalidOperationException("The existing-text literal string is malformed.");
        }

        var decoded = new List<byte>(token.Length - 2);
        int index = 1;
        int contentEnd = token.Length - 1;
        while (index < contentEnd)
        {
            byte value = token[index++];
            if (value == (byte)'\\')
            {
                DecodeEscape(token, contentEnd, ref index, decoded);
                continue;
            }

            if (value == (byte)'\r')
            {
                if (index < contentEnd && token[index] == (byte)'\n')
                {
                    index++;
                }
                decoded.Add((byte)'\n');
                continue;
            }

            decoded.Add(value);
        }

        return [.. decoded];
    }

    private static void DecodeEscape(
        ReadOnlySpan<byte> token,
        int contentEnd,
        ref int index,
        List<byte> decoded)
    {
        if (index >= contentEnd)
        {
            throw new InvalidOperationException("The existing-text literal-string escape is incomplete.");
        }

        byte escaped = token[index++];
        switch (escaped)
        {
            case (byte)'n': decoded.Add((byte)'\n'); return;
            case (byte)'r': decoded.Add((byte)'\r'); return;
            case (byte)'t': decoded.Add((byte)'\t'); return;
            case (byte)'b': decoded.Add((byte)'\b'); return;
            case (byte)'f': decoded.Add((byte)'\f'); return;
            case (byte)'(':
            case (byte)')':
            case (byte)'\\':
                decoded.Add(escaped);
                return;
            case (byte)'\r':
                if (index < contentEnd && token[index] == (byte)'\n') index++;
                return;
            case (byte)'\n':
                return;
        }

        if (escaped is < (byte)'0' or > (byte)'7')
        {
            decoded.Add(escaped);
            return;
        }

        int octal = escaped - (byte)'0';
        int additional = 0;
        while (additional < 2 && index < contentEnd && token[index] is >= (byte)'0' and <= (byte)'7')
        {
            octal = (octal * 8) + (token[index] - (byte)'0');
            index++;
            additional++;
        }

        decoded.Add(unchecked((byte)octal));
    }

    private static byte[] DecodeHexadecimalString(ReadOnlySpan<byte> token)
    {
        if (token[^1] != (byte)'>')
        {
            throw new InvalidOperationException("The existing-text hexadecimal string is malformed.");
        }

        var result = new List<byte>(Math.Max((token.Length - 2) / 2, 0));
        int? highNibble = null;
        for (int index = 1; index < token.Length - 1; index++)
        {
            byte value = token[index];
            if (value is 0 or 9 or 10 or 12 or 13 or 32)
            {
                continue;
            }

            int nibble = DecodeHexNibble(value);
            if (!highNibble.HasValue)
            {
                highNibble = nibble;
                continue;
            }

            result.Add(checked((byte)((highNibble.Value << 4) | nibble)));
            highNibble = null;
        }

        if (highNibble.HasValue)
        {
            result.Add(checked((byte)(highNibble.Value << 4)));
        }

        return [.. result];
    }

    private static int DecodeHexNibble(byte value)
    {
        if (value is >= (byte)'0' and <= (byte)'9') return value - (byte)'0';
        if (value is >= (byte)'A' and <= (byte)'F') return value - (byte)'A' + 10;
        if (value is >= (byte)'a' and <= (byte)'f') return value - (byte)'a' + 10;
        throw new InvalidOperationException("The existing-text hexadecimal string contains an invalid digit.");
    }

    private static void WriteOctalByte(Stream destination, byte value)
    {
        destination.WriteByte((byte)'\\');
        destination.WriteByte((byte)('0' + ((value >> 6) & 0x07)));
        destination.WriteByte((byte)('0' + ((value >> 3) & 0x07)));
        destination.WriteByte((byte)('0' + (value & 0x07)));
    }

    private static void WriteEscape(Stream destination, byte escaped)
    {
        destination.WriteByte((byte)'\\');
        destination.WriteByte(escaped);
    }
}
