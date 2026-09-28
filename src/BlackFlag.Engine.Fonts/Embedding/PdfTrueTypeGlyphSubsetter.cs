using System.Text;
using BlackFlag.Engine.Fonts.Sanitization;

namespace BlackFlag.Engine.Fonts.Embedding;

/// <summary>
/// Creates a conservative glyph-outline subset from a sanitized TrueType
/// program while retaining original glyph IDs.
/// </summary>
public sealed class PdfTrueTypeGlyphSubsetter
{
    private readonly PdfOpenTypeFontSanitizer sanitizer;

    public PdfTrueTypeGlyphSubsetter()
        : this(new PdfOpenTypeFontSanitizer())
    {
    }

    public PdfTrueTypeGlyphSubsetter(PdfOpenTypeFontSanitizer sanitizer)
    {
        ArgumentNullException.ThrowIfNull(sanitizer);
        this.sanitizer = sanitizer;
    }

    public PdfTrueTypeSubsetResult CreateSubset(
        PdfSanitizedExternalFontProgram program,
        string requiredText,
        CancellationToken cancellationToken = default) =>
        CreateSubset(
            program,
            requiredText,
            new PdfTrueTypeSubsetOptions(),
            cancellationToken);

    public PdfTrueTypeSubsetResult CreateSubset(
        PdfSanitizedExternalFontProgram program,
        string requiredText,
        PdfTrueTypeSubsetOptions options,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(program);
        ArgumentNullException.ThrowIfNull(requiredText);
        ArgumentNullException.ThrowIfNull(options);
        options.Validate();

        if (program.Technology != PdfExternalFontTechnology.TrueType)
        {
            throw new NotSupportedException(
                "BF-1054L subsetting supports sanitized TrueType glyf/loca fonts only.");
        }

        cancellationToken.ThrowIfCancellationRequested();
        program.EmbeddingRestrictions.EnsureCanEmbed(subset: true);
        byte[] source = program.TrustedBytes.ToArray();
        Dictionary<string, PdfTrueTypeTable> tables =
            PdfTrueTypeBinary.ReadTables(source);

        PdfTrueTypeTable head = PdfTrueTypeBinary.Require(tables, "head");
        PdfTrueTypeTable maxp = PdfTrueTypeBinary.Require(tables, "maxp");
        PdfTrueTypeTable cmap = PdfTrueTypeBinary.Require(tables, "cmap");
        PdfTrueTypeTable loca = PdfTrueTypeBinary.Require(tables, "loca");
        PdfTrueTypeTable glyf = PdfTrueTypeBinary.Require(tables, "glyf");

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
            PdfTrueTypeBinary.Slice(source, glyf);

        uint[] offsets =
            PdfTrueTypeBinary.ReadLoca(
                PdfTrueTypeBinary.Slice(source, loca),
                glyphCount,
                locaFormat,
                glyf.Length);

        Func<int, int> glyphMapper =
            PdfTrueTypeCmap.CreateGlyphMapper(
                PdfTrueTypeBinary.Slice(source, cmap));

        var selectedGlyphs = new HashSet<int> { 0 };
        var unicodeByCode = new Dictionary<byte, int>();

        CollectRequestedGlyphs(
            requiredText,
            glyphCount,
            glyfBytes,
            offsets,
            glyphMapper,
            selectedGlyphs,
            unicodeByCode,
            options,
            cancellationToken);

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
                        .Slice(source, pair.Value)
                        .ToArray(),
                StringComparer.Ordinal);

        (rebuilt["glyf"], rebuilt["loca"]) =
            PdfTrueTypeSfntBuilder.RebuildGlyphTables(
                glyfBytes,
                offsets,
                selectedGlyphs,
                locaFormat,
                glyphCount);

        rebuilt["cmap"] =
            PdfTrueTypeCmap.BuildSubsetCmap(
                unicodeByCode,
                glyphMapper);

        byte[] sfnt =
            PdfTrueTypeSfntBuilder.Build(
                rebuilt,
                options.MaximumGeneratedBytes,
                "TrueType");

        using var stream = new MemoryStream(sfnt, writable: false);

        PdfSanitizedExternalFontProgram sanitized =
            sanitizer.Sanitize(
                stream,
                cancellationToken: cancellationToken);

        return new PdfTrueTypeSubsetResult(
            sanitized,
            unicodeByCode);
    }

    private static void CollectRequestedGlyphs(
        string requiredText,
        int glyphCount,
        ReadOnlySpan<byte> glyf,
        IReadOnlyList<uint> offsets,
        Func<int, int> glyphMapper,
        HashSet<int> selectedGlyphs,
        Dictionary<byte, int> unicodeByCode,
        PdfTrueTypeSubsetOptions options,
        CancellationToken cancellationToken)
    {
        foreach (Rune rune in requiredText.EnumerateRunes())
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!PdfTrueTypeCmap.TryEncodeWinAnsi(rune.Value, out byte code))
            {
                throw new NotSupportedException(
                    $"Unicode scalar U+{rune.Value:X} is outside the BF-1054K WinAnsi subset scope.");
            }

            int glyph = glyphMapper(rune.Value);

            if (glyph <= 0 || glyph >= glyphCount)
            {
                throw new NotSupportedException(
                    $"The selected TrueType font has no usable glyph for U+{rune.Value:X}.");
            }

            PdfTrueTypeCompositeClosure.EnsureUsableRootGlyph(
                glyf,
                offsets,
                glyph);

            PdfTrueTypeCompositeClosure.RetainGlyph(selectedGlyphs, glyph, options);
            unicodeByCode[code] = rune.Value;
        }
    }
}
