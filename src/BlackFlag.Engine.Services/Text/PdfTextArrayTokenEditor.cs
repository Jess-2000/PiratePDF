namespace BlackFlag.Engine.Services.Text;

/// <summary>
/// Rewrites only text-string tokens in one provenance-identified TJ operation,
/// preserving all numeric adjustments and surrounding syntax byte-for-byte.
/// </summary>
internal static class PdfTextArrayTokenEditor
{
    public static byte[] Replace(
        ReadOnlySpan<byte> streamData,
        long operationStartOffset,
        long operationEndOffset,
        IReadOnlyList<ReadOnlyMemory<byte>> expectedEncodedSegments,
        IReadOnlyList<ReadOnlyMemory<byte>> replacementEncodedSegments)
    {
        ArgumentNullException.ThrowIfNull(expectedEncodedSegments);
        ArgumentNullException.ThrowIfNull(replacementEncodedSegments);

        if (expectedEncodedSegments.Count != replacementEncodedSegments.Count)
        {
            throw new ArgumentException(
                "Positioned text replacement requires one replacement segment per source text segment.",
                nameof(replacementEncodedSegments));
        }

        if (operationStartOffset < 0 ||
            operationEndOffset < operationStartOffset ||
            operationEndOffset > streamData.Length)
        {
            throw new InvalidOperationException(
                "The positioned-text source span lies outside the current content-stream data.");
        }

        int operationStart = checked((int)operationStartOffset);
        int operationEnd = checked((int)operationEndOffset);
        ReadOnlySpan<byte> operation =
            streamData[
                operationStart..operationEnd];

        TokenSpan[] tokens =
            FindStringTokens(operation);

        if (tokens.Length != expectedEncodedSegments.Count)
        {
            throw new InvalidOperationException(
                "The positioned-text source operation no longer has the expected number of text-string elements.");
        }

        var replacements =
            new byte[tokens.Length][];

        for (int index = 0; index < tokens.Length; index++)
        {
            TokenSpan token = tokens[index];
            byte[] current =
                DecodeStringToken(
                    operation.Slice(
                        token.Start,
                        token.Length));

            if (!current.AsSpan().SequenceEqual(
                    expectedEncodedSegments[index].Span))
            {
                throw new InvalidOperationException(
                    $"The positioned-text identity is stale because source text segment {index} no longer matches the discovered bytes.");
            }

            replacements[index] =
                token.IsHexadecimal
                    ? EncodeHexadecimalString(
                        replacementEncodedSegments[index].Span)
                    : EncodeLiteralString(
                        replacementEncodedSegments[index].Span);
        }

        int replacementLength =
            streamData.Length;

        for (int index = 0; index < tokens.Length; index++)
        {
            replacementLength =
                checked(
                    replacementLength -
                    tokens[index].Length +
                    replacements[index].Length);
        }

        byte[] result =
            new byte[replacementLength];

        int sourceCursor = 0;
        int destinationCursor = 0;

        for (int index = 0; index < tokens.Length; index++)
        {
            TokenSpan token = tokens[index];
            int absoluteTokenStart =
                checked(
                    operationStart +
                    token.Start);
            int absoluteTokenEnd =
                checked(
                    absoluteTokenStart +
                    token.Length);

            int prefixLength =
                absoluteTokenStart -
                sourceCursor;

            streamData.Slice(
                    sourceCursor,
                    prefixLength)
                .CopyTo(
                    result.AsSpan(
                        destinationCursor));

            destinationCursor +=
                prefixLength;

            replacements[index].CopyTo(
                result.AsSpan(
                    destinationCursor));

            destinationCursor +=
                replacements[index].Length;

            sourceCursor =
                absoluteTokenEnd;
        }

        streamData[sourceCursor..]
            .CopyTo(
                result.AsSpan(
                    destinationCursor));

        return result;
    }

    private static TokenSpan[] FindStringTokens(
        ReadOnlySpan<byte> operation)
    {
        var result =
            new List<TokenSpan>();

        int arrayStart =
            operation.IndexOf(
                (byte)'[');

        if (arrayStart < 0)
        {
            throw new InvalidOperationException(
                "The positioned-text TJ operation does not contain an array.");
        }

        int arrayEnd =
            FindArrayEnd(
                operation,
                arrayStart);

        int index =
            arrayStart + 1;

        while (index < arrayEnd)
        {
            byte value =
                operation[index];

            if (value == (byte)'%')
            {
                index =
                    SkipComment(
                        operation,
                        index,
                        arrayEnd);
                continue;
            }

            if (value == (byte)'(')
            {
                int end =
                    FindLiteralStringEnd(
                        operation,
                        index,
                        arrayEnd);

                result.Add(
                    new TokenSpan(
                        index,
                        end - index,
                        IsHexadecimal: false));

                index = end;
                continue;
            }

            if (value == (byte)'<' &&
                (index + 1 >= arrayEnd ||
                operation[index + 1] != (byte)'<'))
            {
                int end =
                    FindHexadecimalStringEnd(
                        operation,
                        index,
                        arrayEnd);

                result.Add(
                    new TokenSpan(
                        index,
                        end - index,
                        IsHexadecimal: true));

                index = end;
                continue;
            }

            index++;
        }

        if (result.Count == 0)
        {
            throw new InvalidOperationException(
                "The positioned-text TJ array contains no text-string elements.");
        }

        return [.. result];
    }

    private static int FindArrayEnd(
        ReadOnlySpan<byte> bytes,
        int start)
    {
        int depth = 1;
        int index = start + 1;

        while (index < bytes.Length)
        {
            byte value =
                bytes[index];

            if (value == (byte)'%')
            {
                index =
                    SkipComment(
                        bytes,
                        index,
                        bytes.Length);
                continue;
            }

            if (value == (byte)'(')
            {
                index =
                    FindLiteralStringEnd(
                        bytes,
                        index,
                        bytes.Length);
                continue;
            }

            if (value == (byte)'<' &&
                (index + 1 >= bytes.Length ||
                bytes[index + 1] != (byte)'<'))
            {
                index =
                    FindHexadecimalStringEnd(
                        bytes,
                        index,
                        bytes.Length);
                continue;
            }

            if (value == (byte)'[')
            {
                depth++;
            }
            else if (value == (byte)']')
            {
                depth--;

                if (depth == 0)
                {
                    return index;
                }
            }

            index++;
        }

        throw new InvalidOperationException(
            "The positioned-text TJ array is unterminated.");
    }

    private static int SkipComment(
        ReadOnlySpan<byte> bytes,
        int start,
        int end)
    {
        int index = start + 1;

        while (index < end &&
            bytes[index] is not (byte)'\r' and not (byte)'\n')
        {
            index++;
        }

        return index;
    }

    private static int FindLiteralStringEnd(
        ReadOnlySpan<byte> bytes,
        int start,
        int end)
    {
        int depth = 1;
        int index = start + 1;

        while (index < end)
        {
            byte value =
                bytes[index++];

            if (value == (byte)'\\')
            {
                if (index >= end)
                {
                    break;
                }

                if (bytes[index] == (byte)'\r')
                {
                    index++;

                    if (index < end &&
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
            "A positioned-text literal string is unterminated.");
    }

    private static int FindHexadecimalStringEnd(
        ReadOnlySpan<byte> bytes,
        int start,
        int end)
    {
        for (int index = start + 1;
            index < end;
            index++)
        {
            if (bytes[index] == (byte)'>')
            {
                return index + 1;
            }
        }

        throw new InvalidOperationException(
            "A positioned-text hexadecimal string is unterminated.");
    }

    private static byte[] DecodeStringToken(
        ReadOnlySpan<byte> token)
    {
        if (token.Length < 2)
        {
            throw new InvalidOperationException(
                "A positioned-text source string token is malformed.");
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
                    "A positioned-text source token is not a supported PDF string.")
        };
    }

    private static byte[] DecodeLiteralString(
        ReadOnlySpan<byte> token)
    {
        if (token[^1] != (byte)')')
        {
            throw new InvalidOperationException(
                "A positioned-text literal string is malformed.");
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
                "A positioned-text literal-string escape is incomplete.");
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
                "A positioned-text hexadecimal string is malformed.");
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
            "A positioned-text hexadecimal string contains an invalid digit.");
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
                        WriteOctalByte(
                            destination,
                            item);
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
