using BlackFlag.Engine.Objects;
using BlackFlag.Engine.Rendering.Resources;

namespace BlackFlag.Engine.Rendering.Fonts;

/// <summary>
/// Encodes replacement Unicode through the existing mapping of one resolved
/// simple PDF font without changing that font or its resources.
/// </summary>
public sealed class PdfSameFontTextEncoder
{
    private static readonly PdfName BaseEncodingKey =
        new("BaseEncoding");
    private static readonly PdfName DifferencesKey =
        new("Differences");

    private readonly PdfFontStreamDecoder streamDecoder;

    /// <summary>Initializes the production same-font encoder.</summary>
    public PdfSameFontTextEncoder()
        : this(
            new PdfFontStreamDecoder())
    {
    }

    /// <summary>Initializes a same-font encoder with an explicit stream decoder.</summary>
    public PdfSameFontTextEncoder(
        PdfFontStreamDecoder streamDecoder)
    {
        ArgumentNullException.ThrowIfNull(streamDecoder);
        this.streamDecoder = streamDecoder;
    }

    /// <summary>
    /// Attempts to encode Unicode text using only the font's existing
    /// one-byte character-code mapping.
    /// </summary>
    public bool TryEncode(
        PdfResolvedFont font,
        string text,
        out byte[] encodedBytes,
        out string? failureReason,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(font);
        ArgumentNullException.ThrowIfNull(text);

        if (font.Technology == PdfFontTechnology.Type3)
        {
            encodedBytes = [];
            failureReason =
                "Type 3 fonts are not supported by direct same-font text replacement.";
            return false;
        }

        if (string.Equals(
                font.Subtype,
                "Type0",
                StringComparison.Ordinal))
        {
            encodedBytes = [];
            failureReason =
                "Composite Type 0 fonts require the dedicated composite-font encoding strategy.";
            return false;
        }

        if (text.Length == 0)
        {
            encodedBytes = [];
            failureReason = null;
            return true;
        }

        Dictionary<string, byte?> reverse =
            BuildReverseMapping(
                font,
                cancellationToken,
                out failureReason);

        if (failureReason is not null)
        {
            encodedBytes = [];
            return false;
        }

        (string Text, byte Code)[] candidates =
        [
            .. reverse
                .Where(
                    static pair =>
                        pair.Value.HasValue &&
                        pair.Key.Length > 0)
                .Select(
                    static pair =>
                        (pair.Key, pair.Value!.Value))
                .OrderByDescending(
                    static pair =>
                        pair.Key.Length)
                .ThenBy(
                    static pair =>
                        pair.Key,
                    StringComparer.Ordinal)
        ];

        var result =
            new List<byte>(
                text.Length);

        int index = 0;

        while (index < text.Length)
        {
            cancellationToken.ThrowIfCancellationRequested();

            bool matched = false;

            for (int candidateIndex = 0;
                candidateIndex < candidates.Length;
                candidateIndex++)
            {
                (string candidateText, byte code) =
                    candidates[candidateIndex];

                if (!text.AsSpan(index).StartsWith(
                        candidateText,
                        StringComparison.Ordinal))
                {
                    continue;
                }

                result.Add(code);
                index += candidateText.Length;
                matched = true;
                break;
            }

            if (matched)
            {
                continue;
            }

            encodedBytes = [];
            failureReason =
                $"The existing font '{font.FaceName}' cannot represent replacement text beginning at UTF-16 offset {index}.";
            return false;
        }

        encodedBytes =
            [.. result];
        failureReason = null;
        return true;
    }

    private Dictionary<string, byte?> BuildReverseMapping(
        PdfResolvedFont font,
        CancellationToken cancellationToken,
        out string? failureReason)
    {
        PdfCMap? toUnicode = null;

        if (font.ToUnicode is not null)
        {
            try
            {
                toUnicode =
                    PdfCMapParser.Parse(
                        streamDecoder.Decode(
                            font.ToUnicode,
                            cancellationToken).Span);
            }
            catch (Exception exception)
                when (exception is InvalidDataException
                    or FormatException
                    or NotSupportedException)
            {
                failureReason =
                    $"The existing font ToUnicode mapping cannot be decoded safely: {exception.Message}";
                return new Dictionary<string, byte?>(
                    StringComparer.Ordinal);
            }
        }

        Dictionary<int, string> differences =
            ReadDifferences(
                font.Encoding);
        string baseEncoding =
            ReadBaseEncoding(
                font.Encoding);

        var result =
            new Dictionary<string, byte?>(
                StringComparer.Ordinal);

        for (int code = 0;
            code <= byte.MaxValue;
            code++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            byte sourceCode =
                checked((byte)code);

            if (toUnicode?.TryGetUnicode(
                    new PdfCharacterCode(
                        [sourceCode]),
                    out string? unicode) != true)
            {
                if (differences.TryGetValue(
                        code,
                        out string? glyphName) &&
                    PdfGlyphNameUnicodeMap.TryGetUnicode(
                        glyphName,
                        out string? mapped))
                {
                    unicode = mapped;
                }
                else
                {
                    unicode =
                        baseEncoding switch
                        {
                            "WinAnsiEncoding" =>
                                PdfWinAnsiEncoding.GetUnicode(
                                    sourceCode),

                            _ when sourceCode is >= 0x20 and <= 0x7E =>
                                ((char)sourceCode).ToString(),

                            _ =>
                                null
                        };
                }
            }

            if (string.IsNullOrEmpty(
                    unicode))
            {
                continue;
            }

            if (!result.TryAdd(
                    unicode,
                    sourceCode) &&
                result[unicode] != sourceCode)
            {
                result[unicode] = null;
            }
        }

        failureReason = null;
        return result;
    }

    private static Dictionary<int, string> ReadDifferences(
        PdfObject? encoding)
    {
        var result =
            new Dictionary<int, string>();

        if (encoding is not PdfDictionary dictionary ||
            !dictionary.TryGetValue(
                DifferencesKey,
                out PdfObject value) ||
            value is not PdfArray array)
        {
            return result;
        }

        int code = -1;

        foreach (PdfObject item in array)
        {
            switch (item)
            {
                case PdfInteger integer:
                    code =
                        checked(
                            (int)integer.Value);
                    break;

                case PdfName name when code >= 0:
                    result[code++] =
                        name.Value;
                    break;
            }
        }

        return result;
    }

    private static string ReadBaseEncoding(
        PdfObject? encoding)
    {
        if (encoding is PdfName name)
        {
            return name.Value;
        }

        if (encoding is PdfDictionary dictionary &&
            dictionary.TryGetValue(
                BaseEncodingKey,
                out PdfObject value) &&
            value is PdfName baseName)
        {
            return baseName.Value;
        }

        return "StandardEncoding";
    }
}
