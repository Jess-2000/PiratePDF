namespace BlackFlag.Engine.Fonts.Embedding;

internal sealed class PdfTrueTypeGlyphMetricsReader
{
    private readonly int unitsPerEm;
    private readonly int[] advances;

    private PdfTrueTypeGlyphMetricsReader(
        int unitsPerEm,
        int[] advances)
    {
        this.unitsPerEm = unitsPerEm;
        this.advances = advances;
    }

    public static PdfTrueTypeGlyphMetricsReader Create(
        ReadOnlySpan<byte> source,
        IReadOnlyDictionary<string, PdfTrueTypeTable> tables)
    {
        PdfTrueTypeTable head =
            PdfTrueTypeBinary.Require(tables, "head");
        PdfTrueTypeTable hhea =
            PdfTrueTypeBinary.Require(tables, "hhea");
        PdfTrueTypeTable hmtx =
            PdfTrueTypeBinary.Require(tables, "hmtx");
        PdfTrueTypeTable maxp =
            PdfTrueTypeBinary.Require(tables, "maxp");

        ReadOnlySpan<byte> headBytes =
            PdfTrueTypeBinary.Slice(source, head);
        ReadOnlySpan<byte> hheaBytes =
            PdfTrueTypeBinary.Slice(source, hhea);
        ReadOnlySpan<byte> hmtxBytes =
            PdfTrueTypeBinary.Slice(source, hmtx);
        ReadOnlySpan<byte> maxpBytes =
            PdfTrueTypeBinary.Slice(source, maxp);

        int unitsPerEm =
            PdfTrueTypeBinary.U16(
                headBytes,
                18);

        int glyphCount =
            PdfTrueTypeBinary.U16(
                maxpBytes,
                4);

        int metricCount =
            PdfTrueTypeBinary.U16(
                hheaBytes,
                34);

        if (unitsPerEm is < 16 or > 16384 ||
            glyphCount <= 0 ||
            metricCount <= 0 ||
            metricCount > glyphCount)
        {
            throw new InvalidDataException(
                "The sanitized TrueType metric tables are inconsistent.");
        }

        int required =
            checked(
                metricCount * 4 +
                (glyphCount - metricCount) * 2);

        if (hmtxBytes.Length < required)
        {
            throw new InvalidDataException(
                "The sanitized hmtx table is shorter than required.");
        }

        int[] advances =
            new int[glyphCount];

        int lastAdvance = 0;

        for (int glyph = 0;
            glyph < metricCount;
            glyph++)
        {
            lastAdvance =
                PdfTrueTypeBinary.U16(
                    hmtxBytes,
                    glyph * 4);

            advances[glyph] =
                lastAdvance;
        }

        for (int glyph = metricCount;
            glyph < glyphCount;
            glyph++)
        {
            advances[glyph] =
                lastAdvance;
        }

        return new PdfTrueTypeGlyphMetricsReader(
            unitsPerEm,
            advances);
    }

    public int GetPdfWidth(int glyph)
    {
        if (glyph < 0 ||
            glyph >= advances.Length)
        {
            throw new ArgumentOutOfRangeException(
                nameof(glyph));
        }

        return checked(
            (int)Math.Round(
                advances[glyph] *
                1000.0 /
                unitsPerEm,
                MidpointRounding.AwayFromZero));
    }
}
