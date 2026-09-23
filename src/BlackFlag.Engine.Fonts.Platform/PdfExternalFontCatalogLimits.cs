namespace BlackFlag.Engine.Fonts.Platform;

/// <summary>
/// Positive safety ceilings for one filesystem provider's cold catalog build.
/// These defaults are resource policy, not benchmark-derived tuning.
/// </summary>
public sealed class PdfExternalFontCatalogLimits
{
    /// <summary>Maximum supplied directory values, including duplicates and blanks.</summary>
    public int MaximumSearchDirectories { get; init; } = 64;

    /// <summary>Maximum immediate filesystem entries, including directories, non-fonts and duplicates.</summary>
    public int MaximumDirectoryEntries { get; init; } = 65_536;

    /// <summary>
    /// Maximum distinct supported paths admitted for inspection, including rejected
    /// or unreadable fonts. Each supported standalone font yields at most one face.
    /// </summary>
    public int MaximumCandidates { get; init; } = 4_096;

    /// <summary>
    /// Maximum source bytes read across candidates, including rejected fonts and
    /// rereads. Length is preflighted; actual reads remain authoritative. At the
    /// boundary, one EOF-probe byte may be read but is never delivered for processing.
    /// </summary>
    public long MaximumSourceBytes { get; init; } = 1L << 30;

    internal void Validate()
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(MaximumSearchDirectories);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(MaximumDirectoryEntries);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(MaximumCandidates);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(MaximumSourceBytes);
    }
}
