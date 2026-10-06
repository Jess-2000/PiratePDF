namespace BlackFlag.Engine.Fonts;

/// <summary>Describes deterministic external-font matching preferences.</summary>
public sealed class PdfExternalFontMatchRequest
{
    public PdfExternalFontMatchRequest(
        string familyName,
        int weight = 400,
        int stretch = 5,
        PdfExternalFontSlant slant = PdfExternalFontSlant.Upright,
        bool requireEmbeddable = true)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(familyName);

        if (weight is < 1 or > 1000)
        {
            throw new ArgumentOutOfRangeException(nameof(weight));
        }

        if (stretch is < 1 or > 9)
        {
            throw new ArgumentOutOfRangeException(nameof(stretch));
        }

        if (!Enum.IsDefined(slant))
        {
            throw new ArgumentOutOfRangeException(nameof(slant));
        }

        FamilyName = familyName;
        Weight = weight;
        Stretch = stretch;
        Slant = slant;
        RequireEmbeddable = requireEmbeddable;
    }

    public string FamilyName { get; }
    public int Weight { get; }
    public int Stretch { get; }
    public PdfExternalFontSlant Slant { get; }
    public bool RequireEmbeddable { get; }
}
