using System.Text;

namespace BlackFlag.Engine.Fonts;

/// <summary>
/// Immutable metadata derived only from a sanitized external font program.
/// </summary>
public sealed class PdfSanitizedExternalFontMetadata
{
    private readonly Func<int, int> glyphMapper;

    internal PdfSanitizedExternalFontMetadata(
        string familyName,
        string faceName,
        int weight,
        int stretch,
        PdfExternalFontSlant slant,
        PdfExternalFontEmbeddingRights embeddingRights,
        Func<int, int> glyphMapper)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(familyName);
        ArgumentException.ThrowIfNullOrWhiteSpace(faceName);
        ArgumentNullException.ThrowIfNull(glyphMapper);

        FamilyName = familyName;
        FaceName = faceName;
        Weight = weight;
        Stretch = stretch;
        Slant = slant;
        EmbeddingRights = embeddingRights;
        this.glyphMapper = glyphMapper;
    }

    public string FamilyName { get; }
    public string FaceName { get; }
    public int Weight { get; }
    public int Stretch { get; }
    public PdfExternalFontSlant Slant { get; }
    public PdfExternalFontEmbeddingRights EmbeddingRights { get; }

    public bool SupportsRune(Rune rune) =>
        glyphMapper(rune.Value) > 0;
}
