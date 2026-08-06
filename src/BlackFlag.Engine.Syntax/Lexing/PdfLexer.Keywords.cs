namespace BlackFlag.Engine.Syntax.Lexing;

public sealed partial class PdfLexer
{
    private const int MaximumDisplayedUnknownKeywordLength = 16;

    private PdfToken ReadKeyword()
    {
        PdfSourcePosition start =
            Position;

        Span<byte> keywordBytes =
            stackalloc byte[MaximumDisplayedUnknownKeywordLength];

        int keywordLength =
            0;

        while (
            TryPeek(out byte value) &&
            IsRegularPdfByte(value))
        {
            ConsumeByte();

            EnsureTokenLength(
                start);

            if (keywordLength < keywordBytes.Length)
            {
                keywordBytes[keywordLength] =
                    value;
            }

            keywordLength++;
        }

        if (
            keywordLength <= keywordBytes.Length &&
            TryGetKeywordKind(
                keywordBytes[..keywordLength],
                out PdfTokenKind kind))
        {
            return CreateToken(
                kind,
                start);
        }

        if (Options.AllowUnknownKeywords)
        {
            return CreateToken(
                PdfTokenKind.Keyword,
                start);
        }

        throw CreateUnknownKeywordException(
            keywordBytes,
            keywordLength,
            start);
    }

    private static bool TryGetKeywordKind(
        ReadOnlySpan<byte> keyword,
        out PdfTokenKind kind)
    {
        switch (keyword.Length)
        {
            case 1:
                if (keyword[0] == (byte)'R')
                {
                    kind =
                        PdfTokenKind.IndirectReferenceMarker;

                    return true;
                }

                break;

            case 3:
                if (keyword.SequenceEqual("obj"u8))
                {
                    kind =
                        PdfTokenKind.ObjectStart;

                    return true;
                }

                break;

            case 4:
                if (keyword.SequenceEqual("true"u8))
                {
                    kind =
                        PdfTokenKind.BooleanTrue;

                    return true;
                }

                if (keyword.SequenceEqual("null"u8))
                {
                    kind =
                        PdfTokenKind.Null;

                    return true;
                }

                if (keyword.SequenceEqual("xref"u8))
                {
                    kind =
                        PdfTokenKind.CrossReference;

                    return true;
                }

                break;

            case 5:
                if (keyword.SequenceEqual("false"u8))
                {
                    kind =
                        PdfTokenKind.BooleanFalse;

                    return true;
                }

                break;

            case 6:
                if (keyword.SequenceEqual("endobj"u8))
                {
                    kind =
                        PdfTokenKind.ObjectEnd;

                    return true;
                }

                if (keyword.SequenceEqual("stream"u8))
                {
                    kind =
                        PdfTokenKind.StreamStart;

                    return true;
                }

                break;

            case 7:
                if (keyword.SequenceEqual("trailer"u8))
                {
                    kind =
                        PdfTokenKind.Trailer;

                    return true;
                }

                break;

            case 9:
                if (keyword.SequenceEqual("endstream"u8))
                {
                    kind =
                        PdfTokenKind.StreamEnd;

                    return true;
                }

                if (keyword.SequenceEqual("startxref"u8))
                {
                    kind =
                        PdfTokenKind.StartCrossReference;

                    return true;
                }

                break;
        }

        kind =
            PdfTokenKind.None;

        return false;
    }

    private static bool IsRegularPdfByte(
        byte value)
    {
        return
            !IsPdfWhiteSpaceByte(value) &&
            !IsPdfDelimiterByte(value);
    }

    private static bool IsPdfWhiteSpaceByte(
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

    private static bool IsPdfDelimiterByte(
        byte value)
    {
        return value is
            (byte)'(' or
            (byte)')' or
            (byte)'<' or
            (byte)'>' or
            (byte)'[' or
            (byte)']' or
            (byte)'{' or
            (byte)'}' or
            (byte)'/' or
            (byte)'%';
    }

    private static PdfLexerException CreateUnknownKeywordException(
        ReadOnlySpan<byte> keywordBytes,
        int keywordLength,
        PdfSourcePosition position)
    {
        int displayedLength =
            Math.Min(
                keywordLength,
                keywordBytes.Length);

        string description =
            ConvertKeywordBytesToDisplayText(
                keywordBytes[..displayedLength]);

            byte firstByte = keywordLength > 0
                ? keywordBytes[0]
                : (byte)0;

        if (keywordLength > keywordBytes.Length)
        {
            description =
                string.Concat(
                    description,
                    "...");
        }

        return new PdfLexerException(
            string.Concat(
                "Unknown PDF keyword '",
                description,
                "' (0x",
                firstByte.ToString("X2", System.Globalization.CultureInfo.InvariantCulture),
                ")"),
                position);
    }

    private static string ConvertKeywordBytesToDisplayText(
        ReadOnlySpan<byte> keywordBytes)
    {
        Span<char> characters =
            stackalloc char[keywordBytes.Length];

        for (int index = 0; index < keywordBytes.Length; index++)
        {
            byte value =
                keywordBytes[index];

            characters[index] =
                value is >= 0x20 and <= 0x7E
                    ? (char)value
                    : '?';
        }

        return new string(
            characters);
    }
}
