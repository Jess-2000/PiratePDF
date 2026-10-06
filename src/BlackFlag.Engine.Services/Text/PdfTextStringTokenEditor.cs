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
            PdfTextStringTokenCodec.DecodeStringToken(
                originalToken);

        if (!originalEncoded.AsSpan().SequenceEqual(
                expectedEncodedBytes))
        {
            throw new InvalidOperationException(
                "The existing-text identity is stale because the source string bytes no longer match the discovered text.");
        }

        byte[] replacementToken =
            textToken.IsHexadecimal
                ? PdfTextStringTokenCodec.EncodeHexadecimalString(
                    replacementEncodedBytes)
                : PdfTextStringTokenCodec.EncodeLiteralString(
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
                    PdfTextStringTokenCodec.SkipComment(
                        operation,
                        index);
                continue;
            }

            if (value == (byte)'(')
            {
                int end =
                    PdfTextStringTokenCodec.FindLiteralStringEnd(
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
                    PdfTextStringTokenCodec.FindHexadecimalStringEnd(
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

    private readonly record struct TokenSpan(
        int Start,
        int Length,
        bool IsHexadecimal);
}
