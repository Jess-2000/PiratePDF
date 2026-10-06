namespace BlackFlag.Engine.Fonts;

/// <summary>
/// Describes one immutable face discovered by a platform-neutral external-font
/// provider.
/// </summary>
public sealed class PdfExternalFontFace
{
    public PdfExternalFontFace(
        string providerId,
        string faceId,
        string familyName,
        string faceName,
        int weight,
        int stretch,
        PdfExternalFontSlant slant,
        PdfExternalFontTechnology technology,
        PdfExternalFontEmbeddingRights embeddingRights)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(providerId);
        ArgumentException.ThrowIfNullOrWhiteSpace(faceId);
        ArgumentException.ThrowIfNullOrWhiteSpace(familyName);
        ArgumentException.ThrowIfNullOrWhiteSpace(faceName);

        if (weight is < 1 or > 1000)
        {
            throw new ArgumentOutOfRangeException(
                nameof(weight),
                weight,
                "External font weight must be between 1 and 1000.");
        }

        if (stretch is < 1 or > 9)
        {
            throw new ArgumentOutOfRangeException(
                nameof(stretch),
                stretch,
                "External font stretch must be between 1 and 9.");
        }

        if (!Enum.IsDefined(slant))
        {
            throw new ArgumentOutOfRangeException(nameof(slant));
        }

        if (!Enum.IsDefined(technology))
        {
            throw new ArgumentOutOfRangeException(nameof(technology));
        }

        if (!Enum.IsDefined(embeddingRights))
        {
            throw new ArgumentOutOfRangeException(nameof(embeddingRights));
        }

        ProviderId = providerId;
        FaceId = faceId;
        FamilyName = familyName;
        FaceName = faceName;
        Weight = weight;
        Stretch = stretch;
        Slant = slant;
        Technology = technology;
        EmbeddingRights = embeddingRights;
    }

    public string ProviderId { get; }
    public string FaceId { get; }
    public string FamilyName { get; }
    public string FaceName { get; }
    public int Weight { get; }
    public int Stretch { get; }
    public PdfExternalFontSlant Slant { get; }
    public PdfExternalFontTechnology Technology { get; }
    public PdfExternalFontEmbeddingRights EmbeddingRights { get; }
    /// <summary>Independent provider restriction against creating a subset.</summary>
    public bool NoSubsetting { get; init; }
    /// <summary>Independent provider restriction excluding outline embedding.</summary>
    public bool BitmapEmbeddingOnly { get; init; }
}
