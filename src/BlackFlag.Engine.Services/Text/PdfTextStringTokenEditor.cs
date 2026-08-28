using BlackFlag.Engine.Editing.Text;

namespace BlackFlag.Engine.Services.Text;

/// <summary>
/// Rewrites the single PDF string token inside one provenance-identified text
/// operation while preserving all bytes outside that token.
/// </summary>
internal static class PdfTextStringTokenEditor
{
    public static byte[] Replace(
        ReadOnlySpan<byte> streamData,
        PdfExistingTextIdentity identity,
        ReadOnlySpan<byte> expectedEncodedBytes,
        ReadOnlySpan<byte> replacementEncodedBytes)
    {
        if (identity.EndOffset >
            streamData.Length)
        {
            throw new InvalidOperationException(
                "The existing-text source span lies outside the current content-stream data.");
        }

        int operationStart =
            checked(
                (int)identity.StartOffset);
        int operationEnd =
            checked(
                (int)identity.EndOffset);

        ReadOnlySpan<byte> operation =
            streamData.Slice(
                operationStart,
                operationEnd - operationStart);

        TokenSpan textToken =
            FindSingleStringToken(
                operation);

        ReadOnlySpan<byte> originalToken =
            operation.Slice(
                textToken.Start,
                textToken.Length);

        byte[] originalEncoded =
            DecodeStringToken(
                originalToken);

        if (!originalEncoded.AsSpan().SequenceEqual(
                expectedEncodedBytes))
        {
            throw new InvalidOperationException(
                "The existing-text identity is stale because the source string bytes no longer match the discovered text.");
        }

        byte[] replacementToken =
            textToken.IsHexadecimal
                ? EncodeHexadecimalString(
                    replacementEncodedBytes)
                : EncodeLiteralString(
                    replacementEncodedBytes);

        int tokenStart =
            checked(
                operationStart +
                textToken.Start);
        int tokenEnd =
            checked(
                tokenStart +
                textToken.Length);

        byte[] result =
            new byte[
                checked(
                    streamData.Length -
                    textToken.Length +
                    replacementToken.Length)];

        streamData[..tokenStart]
            .CopyTo(
                result);

        replacementToken.CopyTo(
            result.AsSpan(
                tokenStart));

        streamData[tokenEnd..]
            .CopyTo(
                result.AsSpan(
                    tokenStart +
                    replacementToken.Length));

        return result;
    }

    private static TokenSpan FindSingleStringToken(
        ReadOnlySpan<byte> operation)
    {
        TokenSpan? found = null;
        int index = 0;

        while (index < operation.Length)
        {
            byte value =
                operation[index];

            if (value == (byte)'%')
            {
                index =
                    SkipComment(
                        operation,
                        index);
                continue;
            }

            if (value == (byte)'(')
            {
                int end =
                    FindLiteralStringEnd(
                        operation,
                        index);

                AddFound(
                    ref found,
                    new TokenSpan(
                        index,
                        end - index,
                        IsHexadecimal: false));

                index = end;
                continue;
            }

            if (value == (byte)'<' &&
                (index + 1 >= operation.Length ||
                operation[index + 1] != (byte)'<'))
            {
                int end =
                    FindHexadecimalStringEnd(
                        operation,
                        index);

                AddFound(
                    ref found,
                    new TokenSpan(
                        index,
                        end - index,
                        IsHexadecimal: true));

                index = end;
                continue;
            }

            index++;
        }

        return found ??
            throw new InvalidOperationException(
                "The existing-text source operation does not contain a PDF string token.");
    }

    private static void AddFound(
        ref TokenSpan? found,
        TokenSpan candidate)
    {
        if (found.HasValue)
        {
            throw new NotSupportedException(
                "BF-1054C requires a text-showing operation with exactly one string token.");
        }

        found = candidate;
    }

    private static int SkipComment(
        ReadOnlySpan<byte> bytes,
        int start)
    {
        int index =
            start + 1;

        while (index < bytes.Length &&
            bytes[index] is not (byte)'\r' and not (byte)'\n')
        {
            index++;
        }

        return index;
    }

    private static int FindLiteralStringEnd(
        ReadOnlySpan<byte> bytes,
        int start)
    {
        int depth = 1;
        int index =
            start + 1;

        while (index < bytes.Length)
        {
            byte value =
                bytes[index++];

            if (value == (byte)'\\')
            {
                if (index >= bytes.Length)
                {
                    break;
                }

                if (bytes[index] == (byte)'\r')
                {
                    index++;

                    if (index < bytes.Length &&
                        bytes[index] == (byte)'\n')
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

            depth--;

            if (depth == 0)
            {
                return index;
            }
        }

        throw new InvalidOperationException(
            "The existing-text literal string is unterminated.");
    }

    private static int FindHexadecimalStringEnd(
        ReadOnlySpan<byte> bytes,
        int start)
    {
        for (int index = start + 1;
            index < bytes.Length;
            index++)
        {
            if (bytes[index] == (byte)'>')
            {
                return index + 1;
            }
        }

        throw new InvalidOperationException(
            "The existing-text hexadecimal string is unterminated.");
    }

    private static byte[] DecodeStringToken(
        ReadOnlySpan<byte> token)
    {
        if (token.Length < 2)
        {
            throw new InvalidOperationException(
                "The existing-text source string token is malformed.");
        }

        return token[0] switch
        {
            (byte)'(' =>
                DecodeLiteralString(
                    token),

            (byte)'<' =>
                DecodeHexadecimalString(
                    token),

            _ =>
                throw new InvalidOperationException(
                    "The existing-text source token is not a supported PDF string.")
        };
    }

    private static byte[] DecodeLiteralString(
        ReadOnlySpan<byte> token)
    {
        if (token[^1] != (byte)')')
        {
            throw new InvalidOperationException(
                "The existing-text literal string is malformed.");
        }

        var decoded =
            new List<byte>(
                token.Length - 2);

        int index = 1;
        int contentEnd =
            token.Length - 1;

        while (index < contentEnd)
        {
            byte value =
                token[index++];

            if (value == (byte)'\\')
            {
                DecodeEscape(
                    token,
                    contentEnd,
                    ref index,
                    decoded);
                continue;
            }

            if (value == (byte)'\r')
            {
                if (index < contentEnd &&
                    token[index] == (byte)'\n')
                {
                    index++;
                }

                decoded.Add(
                    (byte)'\n');
                continue;
            }

            decoded.Add(
                value);
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
            throw new InvalidOperationException(
                "The existing-text literal-string escape is incomplete.");
        }

        byte escaped =
            token[index++];

        switch (escaped)
        {
            case (byte)'n':
                decoded.Add((byte)'\n');
                return;
            case (byte)'r':
                decoded.Add((byte)'\r');
                return;
            case (byte)'t':
                decoded.Add((byte)'\t');
                return;
            case (byte)'b':
                decoded.Add((byte)'\b');
                return;
            case (byte)'f':
                decoded.Add((byte)'\f');
                return;
            case (byte)'(':
            case (byte)')':
            case (byte)'\\':
                decoded.Add(escaped);
                return;
            case (byte)'\r':
                if (index < contentEnd &&
                    token[index] == (byte)'\n')
                {
                    index++;
                }
                return;
            case (byte)'\n':
                return;
        }

        if (escaped is < (byte)'0' or > (byte)'7')
        {
            decoded.Add(
                escaped);
            return;
        }

        int octal =
            escaped - (byte)'0';
        int additional = 0;

        while (additional < 2 &&
            index < contentEnd &&
            token[index] is >= (byte)'0' and <= (byte)'7')
        {
            octal =
                (octal * 8) +
                (token[index] - (byte)'0');
            index++;
            additional++;
        }

        decoded.Add(
            unchecked(
                (byte)octal));
    }

    private static byte[] DecodeHexadecimalString(
        ReadOnlySpan<byte> token)
    {
        if (token[^1] != (byte)'>')
        {
            throw new InvalidOperationException(
                "The existing-text hexadecimal string is malformed.");
        }

        var result =
            new List<byte>(
                Math.Max(
                    (token.Length - 2) / 2,
                    0));

        int? highNibble = null;

        for (int index = 1;
            index < token.Length - 1;
            index++)
        {
            byte value =
                token[index];

            if (value is 0 or 9 or 10 or 12 or 13 or 32)
            {
                continue;
            }

            int nibble =
                DecodeHexNibble(
                    value);

            if (!highNibble.HasValue)
            {
                highNibble = nibble;
                continue;
            }

            result.Add(
                checked(
                    (byte)(
                        (highNibble.Value << 4) |
                        nibble)));

            highNibble = null;
        }

        if (highNibble.HasValue)
        {
            result.Add(
                checked(
                    (byte)(
                        highNibble.Value << 4)));
        }

        return [.. result];
    }

    private static int DecodeHexNibble(
        byte value)
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

        throw new InvalidOperationException(
            "The existing-text hexadecimal string contains an invalid digit.");
    }

    private static byte[] EncodeLiteralString(
        ReadOnlySpan<byte> bytes)
    {
        using var destination =
            new MemoryStream();

        destination.WriteByte(
            (byte)'(');

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

        destination.WriteByte(
            (byte)')');

        return destination.ToArray();
    }

    private static byte[] EncodeHexadecimalString(
        ReadOnlySpan<byte> bytes)
    {
        const string Hex = "0123456789ABCDEF";

        byte[] result =
            new byte[
                checked(
                    2 +
                    (bytes.Length * 2))];

        result[0] = (byte)'<';
        int index = 1;

        foreach (byte item in bytes)
        {
            result[index++] =
                (byte)Hex[item >> 4];
            result[index++] =
                (byte)Hex[item & 0x0F];
        }

        result[index] = (byte)'>';
        return result;
    }

    private static void WriteOctalByte(
        Stream destination,
        byte value)
    {
        destination.WriteByte((byte)'\\');
        destination.WriteByte(
            (byte)('0' + ((value >> 6) & 0x07)));
        destination.WriteByte(
            (byte)('0' + ((value >> 3) & 0x07)));
        destination.WriteByte(
            (byte)('0' + (value & 0x07)));
    }

    private static void WriteEscape(
        Stream destination,
        byte escaped)
    {
        destination.WriteByte((byte)'\\');
        destination.WriteByte(escaped);
    }

    private readonly record struct TokenSpan(
        int Start,
        int Length,
        bool IsHexadecimal);
}
