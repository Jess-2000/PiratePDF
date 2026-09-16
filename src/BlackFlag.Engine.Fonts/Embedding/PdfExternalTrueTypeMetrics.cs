namespace BlackFlag.Engine.Fonts.Embedding;

/// <summary>
/// Immutable writer-facing metrics extracted only from a sanitized TrueType
/// font program.
/// </summary>
public sealed class PdfExternalTrueTypeMetrics
{
    internal PdfExternalTrueTypeMetrics(
        int unitsPerEm,
        int ascent,
        int descent,
        int xMin,
        int yMin,
        int xMax,
        int yMax,
        IReadOnlyList<int> winAnsiWidths,
        PdfExternalFontProgramRights restrictions)
    {
        UnitsPerEm = unitsPerEm;
        Ascent = ascent;
        Descent = descent;
        XMin = xMin;
        YMin = yMin;
        XMax = xMax;
        YMax = yMax;
        WinAnsiWidths = Array.AsReadOnly(winAnsiWidths.ToArray());
        EmbeddingRights = restrictions.Level;
        NoSubsetting = restrictions.NoSubsetting;
        BitmapEmbeddingOnly = restrictions.BitmapEmbeddingOnly;
    }

    public int UnitsPerEm { get; }
    public int Ascent { get; }
    public int Descent { get; }
    public int XMin { get; }
    public int YMin { get; }
    public int XMax { get; }
    public int YMax { get; }
    public IReadOnlyList<int> WinAnsiWidths { get; }
    public PdfExternalFontEmbeddingRights EmbeddingRights { get; }
    public bool NoSubsetting { get; }
    public bool BitmapEmbeddingOnly { get; }
}
