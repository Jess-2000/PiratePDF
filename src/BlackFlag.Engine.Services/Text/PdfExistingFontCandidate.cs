using BlackFlag.Engine.Rendering.Resources;

namespace BlackFlag.Engine.Services.Text;

/// <summary>
/// Describes one already-resolved PDF font resource that can be considered
/// for existing-text font selection without importing a new font program.
/// </summary>
public sealed class PdfExistingFontCandidate
{
    internal PdfExistingFontCandidate(
        string resourceName,
        PdfResolvedFont font)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(resourceName);
        ArgumentNullException.ThrowIfNull(font);

        ResourceName = resourceName;
        FamilyName = font.FamilyName;
        FaceName = font.FaceName;
        Weight = font.Weight;
        Stretch = font.Stretch;
        Slant = font.Slant;
        Technology = font.Technology;
        EmbeddingPermission = font.EmbeddingPermission;
        IsSubset = font.IsSubset;
        HasToUnicodeMap = font.HasToUnicodeMap;
    }

    public string ResourceName { get; }
    public string FamilyName { get; }
    public string FaceName { get; }
    public int Weight { get; }
    public int Stretch { get; }
    public PdfFontSlant Slant { get; }
    public PdfFontTechnology Technology { get; }
    public PdfFontEmbeddingRights EmbeddingPermission { get; }
    public bool IsSubset { get; }
    public bool HasToUnicodeMap { get; }
}
