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
                PdfTextStringTokenCodec.DecodeStringToken(
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
                    ? PdfTextStringTokenCodec.EncodeHexadecimalString(
                        replacementEncodedSegments[index].Span)
                    : PdfTextStringTokenCodec.EncodeLiteralString(
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
                    PdfTextStringTokenCodec.SkipComment(
                        operation,
                        index,
                        arrayEnd);
                continue;
            }

            if (value == (byte)'(')
            {
                int end =
                    PdfTextStringTokenCodec.FindLiteralStringEnd(
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
                    PdfTextStringTokenCodec.FindHexadecimalStringEnd(
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
                    PdfTextStringTokenCodec.SkipComment(
                        bytes,
                        index,
                        bytes.Length);
                continue;
            }

            if (value == (byte)'(')
            {
                index =
                    PdfTextStringTokenCodec.FindLiteralStringEnd(
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
                    PdfTextStringTokenCodec.FindHexadecimalStringEnd(
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

    private readonly record struct TokenSpan(
        int Start,
        int Length,
        bool IsHexadecimal);
}
