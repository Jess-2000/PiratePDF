using BlackFlag.Engine.Objects;
using BlackFlag.Engine.Rendering.DisplayList;
using BlackFlag.Engine.Rendering.Resources;

namespace BlackFlag.Engine.Rendering.Fonts;

/// <summary>Decodes PDF text runs into source-linked renderer-neutral glyph records.</summary>
public sealed class PdfFontDecoder
{
    private static readonly PdfName FirstCharKey = new("FirstChar");
    private static readonly PdfName WidthsKey = new("Widths");
    private static readonly PdfName MissingWidthKey = new("MissingWidth");
    private static readonly PdfName EncodingKey = new("Encoding");
    private static readonly PdfName BaseEncodingKey = new("BaseEncoding");
    private static readonly PdfName DifferencesKey = new("Differences");
    private static readonly PdfName DwKey = new("DW");
    private static readonly PdfName WKey = new("W");
    private static readonly PdfName CidToGidMapKey = new("CIDToGIDMap");

    private readonly PdfFontStreamDecoder streamDecoder;
    private readonly PdfCMapParser cmapParser;

    /// <summary>Initializes the production font decoder.</summary>
    public PdfFontDecoder()
        : this(new PdfFontStreamDecoder(), new PdfCMapParser())
    {
    }

    /// <summary>Initializes a font decoder with explicit stream/CMap services.</summary>
    public PdfFontDecoder(
        PdfFontStreamDecoder streamDecoder,
        PdfCMapParser cmapParser)
    {
        ArgumentNullException.ThrowIfNull(streamDecoder);
        ArgumentNullException.ThrowIfNull(cmapParser);
        this.streamDecoder = streamDecoder;
        this.cmapParser = cmapParser;
    }

    /// <summary>Decodes one semantic display text run.</summary>
    public PdfDecodedTextRun Decode(
        PdfDisplayTextRun run,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(run);

        var glyphs = new List<PdfDecodedGlyph>();
        var diagnostics = new List<PdfFontDecodeDiagnostic>();
        PdfCMap? toUnicode = ParseOptionalCMap(run.Font.ToUnicode, diagnostics, "TOUNICODE_MALFORMED", cancellationToken);

        double x = 0;
        foreach (PdfTextRunElement element in run.Elements)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (element.Kind == PdfTextRunElementKind.Adjustment)
            {
                x += (-element.Adjustment / 1000.0) * run.FontSize;
                continue;
            }

            if (string.Equals(run.Font.Subtype, "Type0", StringComparison.Ordinal))
            {
                DecodeComposite(run, element.EncodedBytes.Span, toUnicode, glyphs, diagnostics, ref x, cancellationToken);
            }
            else
            {
                DecodeSimple(run, element.EncodedBytes.Span, toUnicode, glyphs, diagnostics, ref x);
            }
        }

        return new PdfDecodedTextRun(run.Font, run, glyphs, diagnostics);
    }

    /// <summary>Decodes an embedded font program, if one is present.</summary>
    public ReadOnlyMemory<byte>? DecodeEmbeddedProgram(
        PdfResolvedFont font,
        CancellationToken cancellationToken = default) =>
        font.EmbeddedFontProgram is null
            ? null
            : streamDecoder.Decode(font.EmbeddedFontProgram, cancellationToken);

    private static void DecodeSimple(
        PdfDisplayTextRun run,
        ReadOnlySpan<byte> bytes,
        PdfCMap? toUnicode,
        List<PdfDecodedGlyph> glyphs,
        List<PdfFontDecodeDiagnostic> diagnostics,
        ref double x)
    {
        Dictionary<int, string> differences = ReadDifferences(run.Font.Encoding);
        string baseEncoding = ReadBaseEncoding(run.Font.Encoding);

        foreach (byte code in bytes)
        {
            PdfCharacterCode sourceCode = new([code]);
            string? unicode = null;

            if (toUnicode?.TryGetUnicode(sourceCode, out unicode) != true)
            {
                if (differences.TryGetValue(code, out string? glyphName) &&
                    PdfGlyphNameUnicodeMap.TryGetUnicode(glyphName, out string? mapped))
                {
                    unicode = mapped;
                }
                else
                {
                    unicode = baseEncoding switch
                    {
                        "WinAnsiEncoding" => PdfWinAnsiEncoding.GetUnicode(code),
                        _ when code is >= 0x20 and <= 0x7E => ((char)code).ToString(),
                        _ => null
                    };
                }
            }

            if (unicode is null)
            {
                diagnostics.Add(new PdfFontDecodeDiagnostic(
                    "UNICODE_MAPPING_UNAVAILABLE",
                    $"Font '{run.Font.FaceName}' has no Unicode mapping for code 0x{code:X2}."));
            }

            double advance = GetSimpleAdvance(run, code);
            glyphs.Add(new PdfDecodedGlyph([code], null, null, unicode, x, advance));
            x += advance;
        }
    }

    private void DecodeComposite(
        PdfDisplayTextRun run,
        ReadOnlySpan<byte> bytes,
        PdfCMap? toUnicode,
        List<PdfDecodedGlyph> glyphs,
        List<PdfFontDecodeDiagnostic> diagnostics,
        ref double x,
        CancellationToken cancellationToken)
    {
        PdfCMap encodingMap = ReadCompositeEncoding(run.Font, diagnostics, cancellationToken);
        int offset = 0;

        while (offset < bytes.Length)
        {
            PdfCharacterCode code = encodingMap.ReadCode(bytes, offset);
            ReadOnlyMemory<byte> codeBytes = code.Bytes;
            int codeLength = codeBytes.Length;
            int cid;

            if (!encodingMap.TryGetCid(code, out cid))
            {
                cid = ToBigEndianInteger(codeBytes.Span);
            }

            string? unicode = null;
            if (toUnicode?.TryGetUnicode(code, out unicode) != true)
            {
                diagnostics.Add(new PdfFontDecodeDiagnostic(
                    "UNICODE_MAPPING_UNAVAILABLE",
                    $"Composite font '{run.Font.FaceName}' has no ToUnicode mapping for code <{code}>."));
            }

            ushort? glyphId = GetCidGlyphId(run.Font, cid, diagnostics, cancellationToken);
            double advance = GetCompositeAdvance(run, cid);

            glyphs.Add(new PdfDecodedGlyph(codeBytes.Span, cid, glyphId, unicode, x, advance));
            x += advance;
            offset += codeLength;
        }
    }

    private PdfCMap ReadCompositeEncoding(
        PdfResolvedFont font,
        List<PdfFontDecodeDiagnostic> diagnostics,
        CancellationToken cancellationToken)
    {
        if (font.Encoding is PdfName name)
        {
            if (name.Value is "Identity-H" or "Identity-V")
            {
                return new PdfCMap(
                    [new PdfCodeSpaceRange([0x00, 0x00], [0xFF, 0xFF])],
                    new Dictionary<PdfCharacterCode, int>(),
                    new Dictionary<PdfCharacterCode, string>(),
                    name.Value == "Identity-V" ? 1 : 0);
            }

            throw new NotSupportedException(
                $"Predefined CMap '/{name.Value}' requires a registered predefined-CMap provider.");
        }

        if (font.Encoding is PdfStream stream)
        {
            return PdfCMapParser.Parse(streamDecoder.Decode(stream, cancellationToken).Span);
        }

        diagnostics.Add(new PdfFontDecodeDiagnostic(
            "CMAP_UNSUPPORTED",
            $"Composite font '{font.FaceName}' does not expose a usable encoding CMap."));

        throw new InvalidDataException("A Type 0 font requires an encoding CMap.");
    }

    private PdfCMap? ParseOptionalCMap(
        PdfStream? stream,
        List<PdfFontDecodeDiagnostic> diagnostics,
        string code,
        CancellationToken cancellationToken)
    {
        if (stream is null)
        {
            return null;
        }

        try
        {
            return PdfCMapParser.Parse(streamDecoder.Decode(stream, cancellationToken).Span);
        }
        catch (Exception exception) when (exception is InvalidDataException or FormatException)
        {
            diagnostics.Add(new PdfFontDecodeDiagnostic(code, exception.Message));
            return null;
        }
    }

    private static double GetSimpleAdvance(PdfDisplayTextRun run, byte code)
    {
        int firstChar = ReadInteger(run.Font.Dictionary, FirstCharKey) ?? 0;
        PdfArray? widths = ReadArray(run.Font.Dictionary, WidthsKey);
        double missingWidth = ReadNumber(run.Font.Descriptor, MissingWidthKey) ?? 0;

        int widthIndex = code - firstChar;
        double width = widths is not null &&
                    widthIndex >= 0 &&
                    widthIndex < widths.Count &&
                    TryReadNumber(widths[widthIndex], out double explicitWidth)
            ? explicitWidth
            : missingWidth;

        double spacing = run.GraphicsState.CharacterSpacing +
            (code == 0x20 ? run.GraphicsState.WordSpacing : 0);

        return (width / 1000.0) * run.FontSize + spacing;
    }

    private static double GetCompositeAdvance(PdfDisplayTextRun run, int cid)
    {
        PdfDictionary? descendant = GetDescendant(run.Font);
        double width = TryGetCidWidth(ReadArray(descendant, WKey), cid, out double explicitWidth)
            ? explicitWidth
            : ReadNumber(descendant, DwKey) ?? 1000;

        return (width / 1000.0) * run.FontSize + run.GraphicsState.CharacterSpacing;
    }

    private ushort? GetCidGlyphId(
        PdfResolvedFont font,
        int cid,
        List<PdfFontDecodeDiagnostic> diagnostics,
        CancellationToken cancellationToken)
    {
        PdfDictionary? descendant = GetDescendant(font);
        if (descendant is null ||
            !descendant.TryGetValue(CidToGidMapKey, out PdfObject value) ||
            value is PdfName { Value: "Identity" })
        {
            return cid is >= 0 and <= ushort.MaxValue ? (ushort)cid : null;
        }

        if (value is not PdfStream mapStream)
        {
            return null;
        }

        ReadOnlySpan<byte> map = streamDecoder.Decode(mapStream, cancellationToken).Span;
        int offset = checked(cid * 2);
        if (offset < 0 || offset + 1 >= map.Length)
        {
            diagnostics.Add(new PdfFontDecodeDiagnostic(
                "CID_MAPPING_FAILED",
                $"CID {cid} lies outside the CIDToGIDMap."));
            return null;
        }

        return (ushort)((map[offset] << 8) | map[offset + 1]);
    }

    private static Dictionary<int, string> ReadDifferences(PdfObject? encoding)
    {
        var result = new Dictionary<int, string>();
        if (encoding is not PdfDictionary dictionary ||
            !dictionary.TryGetValue(DifferencesKey, out PdfObject value) ||
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
                    code = checked((int)integer.Value);
                    break;

                case PdfName name when code >= 0:
                    result[code++] = name.Value;
                    break;
            }
        }

        return result;
    }

    private static string ReadBaseEncoding(PdfObject? encoding)
    {
        if (encoding is PdfName name)
        {
            return name.Value;
        }

        if (encoding is PdfDictionary dictionary &&
            dictionary.TryGetValue(BaseEncodingKey, out PdfObject value) &&
            value is PdfName baseName)
        {
            return baseName.Value;
        }

        return "StandardEncoding";
    }

    private static PdfDictionary? GetDescendant(PdfResolvedFont font) =>
        font.DescendantFonts is { Count: > 0 } && font.DescendantFonts[0] is PdfDictionary dictionary
            ? dictionary
            : null;

    private static int ToBigEndianInteger(ReadOnlySpan<byte> bytes)
    {
        int value = 0;
        foreach (byte item in bytes)
        {
            value = checked((value << 8) | item);
        }

        return value;
    }

    private static int? ReadInteger(PdfDictionary? dictionary, PdfName key)
    {
        if (dictionary is not null &&
            dictionary.TryGetValue(key, out PdfObject value) &&
            value is PdfInteger integer &&
            integer.Value is >= int.MinValue and <= int.MaxValue)
        {
            return checked((int)integer.Value);
        }

        return null;
    }

    private static PdfArray? ReadArray(PdfDictionary? dictionary, PdfName key) =>
        dictionary is not null &&
        dictionary.TryGetValue(key, out PdfObject value)
            ? value as PdfArray
            : null;

    private static double? ReadNumber(PdfDictionary? dictionary, PdfName key) =>
        dictionary is not null &&
        dictionary.TryGetValue(key, out PdfObject value) &&
        TryReadNumber(value, out double number)
            ? number
            : null;

    private static bool TryReadNumber(PdfObject value, out double number)
    {
        switch (value)
        {
            case PdfInteger integer:
                number = integer.Value;
                return true;
            case PdfReal real:
                number = real.Value;
                return true;
            default:
                number = 0;
                return false;
        }
    }

    private static bool TryGetCidWidth(PdfArray? widths, int cid, out double width)
    {
        width = 0;
        if (widths is null)
        {
            return false;
        }

        int index = 0;
        while (index < widths.Count)
        {
            if (widths[index++] is not PdfInteger first)
            {
                return false;
            }

            int firstCid = checked((int)first.Value);
            if (index >= widths.Count)
            {
                return false;
            }

            if (widths[index] is PdfArray sequential)
            {
                int widthIndex = cid - firstCid;
                if (widthIndex >= 0 &&
                    widthIndex < sequential.Count &&
                    TryReadNumber(sequential[widthIndex], out width))
                {
                    return true;
                }

                index++;
                continue;
            }

            if (widths[index++] is not PdfInteger last || index >= widths.Count)
            {
                return false;
            }

            int lastCid = checked((int)last.Value);
            if (!TryReadNumber(widths[index++], out double rangeWidth))
            {
                return false;
            }

            if (cid >= firstCid && cid <= lastCid)
            {
                width = rangeWidth;
                return true;
            }
        }

        return false;
    }
}
