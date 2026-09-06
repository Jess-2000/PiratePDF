using System.Text;

using BlackFlag.Engine.Fonts.Sanitization;

namespace BlackFlag.Engine.Fonts.Embedding;

/// <summary>
/// Creates a sanitizer-backed TrueType glyph subset for arbitrary Unicode
/// scalars and assigns deterministic two-byte PDF CIDs independently of GIDs.
/// </summary>
public sealed class PdfUnicodeTrueTypeGlyphSubsetter
{
    private readonly PdfOpenTypeFontSanitizer sanitizer;

    public PdfUnicodeTrueTypeGlyphSubsetter()
        : this(new PdfOpenTypeFontSanitizer())
    {
    }

    public PdfUnicodeTrueTypeGlyphSubsetter(
        PdfOpenTypeFontSanitizer sanitizer)
    {
        ArgumentNullException.ThrowIfNull(sanitizer);
        this.sanitizer = sanitizer;
    }

    public PdfUnicodeTrueTypeSubsetResult CreateSubset(
        PdfSanitizedExternalFontProgram program,
        string requiredText,
        PdfTrueTypeSubsetOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(program);
        ArgumentNullException.ThrowIfNull(requiredText);

        options ??=
            new PdfTrueTypeSubsetOptions();

        options.Validate();

        if (program.Technology !=
            PdfExternalFontTechnology.TrueType)
        {
            throw new NotSupportedException(
                "BF-1054M Unicode subsetting supports sanitized TrueType glyf/loca fonts only.");
        }

        byte[] source =
            program.Bytes.ToArray();

        Dictionary<string, PdfTrueTypeTable> tables =
            PdfTrueTypeBinary.ReadTables(source);

        PdfTrueTypeTable head =
            PdfTrueTypeBinary.Require(tables, "head");
        PdfTrueTypeTable maxp =
            PdfTrueTypeBinary.Require(tables, "maxp");
        PdfTrueTypeTable cmap =
            PdfTrueTypeBinary.Require(tables, "cmap");
        PdfTrueTypeTable loca =
            PdfTrueTypeBinary.Require(tables, "loca");
        PdfTrueTypeTable glyf =
            PdfTrueTypeBinary.Require(tables, "glyf");

        int glyphCount =
            PdfTrueTypeBinary.U16(
                PdfTrueTypeBinary.Slice(source, maxp),
                4);

        int locaFormat =
            PdfTrueTypeBinary.I16(
                PdfTrueTypeBinary.Slice(source, head),
                50);

        if (locaFormat is not 0 and not 1)
        {
            throw new InvalidDataException(
                "The sanitized TrueType font has an unsupported loca format.");
        }

        ReadOnlySpan<byte> glyfBytes =
            PdfTrueTypeBinary.Slice(
                source,
                glyf);

        uint[] offsets =
            PdfTrueTypeBinary.ReadLoca(
                PdfTrueTypeBinary.Slice(
                    source,
                    loca),
                glyphCount,
                locaFormat,
                glyf.Length);

        Func<int, int> glyphMapper =
            PdfTrueTypeCmap.CreateGlyphMapper(
                PdfTrueTypeBinary.Slice(
                    source,
                    cmap));

        PdfTrueTypeGlyphMetricsReader metrics =
            PdfTrueTypeGlyphMetricsReader.Create(
                source,
                tables);

        int[] requiredScalars =
        [
            .. requiredText
                .EnumerateRunes()
                .Select(
                    static rune =>
                        rune.Value)
                .Distinct()
                .Order()
        ];

        if (requiredScalars.Length >
            ushort.MaxValue - 1)
        {
            throw new NotSupportedException(
                "The Unicode subset requires more CIDs than Identity-H can represent in BF-1054M.");
        }

        var selectedGlyphs =
            new HashSet<int> { 0 };

        var cidByUnicode =
            new Dictionary<int, ushort>();

        var unicodeByCid =
            new Dictionary<ushort, int>();

        var glyphByCid =
            new Dictionary<ushort, int>();

        var widthByCid =
            new Dictionary<ushort, int>();

        ushort nextCid = 1;

        for (int index = 0;
            index < requiredScalars.Length;
            index++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            int unicode =
                requiredScalars[index];

            int glyph =
                glyphMapper(unicode);

            if (glyph <= 0 ||
                glyph >= glyphCount)
            {
                throw new NotSupportedException(
                    $"The selected TrueType font has no usable glyph for U+{unicode:X}.");
            }

            PdfTrueTypeCompositeClosure
                .EnsureUsableRootGlyph(
                    glyfBytes,
                    offsets,
                    glyph);

            ushort cid =
                nextCid++;

            cidByUnicode.Add(
                unicode,
                cid);

            unicodeByCid.Add(
                cid,
                unicode);

            glyphByCid.Add(
                cid,
                glyph);

            widthByCid.Add(
                cid,
                metrics.GetPdfWidth(
                    glyph));

            _ = selectedGlyphs.Add(
                glyph);
        }

        PdfTrueTypeCompositeClosure.Expand(
            glyfBytes,
            offsets,
            glyphCount,
            selectedGlyphs,
            options,
            cancellationToken);

        Dictionary<string, byte[]> rebuilt =
            tables.ToDictionary(
                static pair => pair.Key,
                pair =>
                    PdfTrueTypeBinary
                        .Slice(
                            source,
                            pair.Value)
                        .ToArray(),
                StringComparer.Ordinal);

        (rebuilt["glyf"], rebuilt["loca"]) =
            PdfTrueTypeSfntBuilder.RebuildGlyphTables(
                glyfBytes,
                offsets,
                selectedGlyphs,
                locaFormat,
                glyphCount);

        byte[] sfnt =
            PdfTrueTypeSfntBuilder.Build(
                rebuilt);

        if (sfnt.Length >
            options.MaximumGeneratedBytes)
        {
            throw new InvalidDataException(
                $"The generated Unicode TrueType subset is {sfnt.Length} bytes, exceeding the configured {options.MaximumGeneratedBytes}-byte limit.");
        }

        using var stream =
            new MemoryStream(
                sfnt,
                writable: false);

        PdfSanitizedExternalFontProgram sanitized =
            sanitizer.Sanitize(
                stream,
                cancellationToken:
                    cancellationToken);

        return new PdfUnicodeTrueTypeSubsetResult(
            sanitized,
            cidByUnicode,
            unicodeByCid,
            glyphByCid,
            widthByCid);
    }
}
