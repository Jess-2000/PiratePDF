namespace BlackFlag.Engine.Services.Text;

/// <summary>
/// Describes a partial same-font text-state update. Null values preserve the
/// corresponding interpreted state.
/// </summary>
public sealed class PdfExistingTextTypographyUpdate
{
    public double? FontSize { get; init; }
    public double? CharacterSpacing { get; init; }
    public double? WordSpacing { get; init; }
    public double? HorizontalScaling { get; init; }
    public int? TextRenderingMode { get; init; }
    public double? TextRise { get; init; }

    internal void Validate()
    {
        ValidateFinite(FontSize, nameof(FontSize));
        ValidateFinite(CharacterSpacing, nameof(CharacterSpacing));
        ValidateFinite(WordSpacing, nameof(WordSpacing));
        ValidateFinite(HorizontalScaling, nameof(HorizontalScaling));
        ValidateFinite(TextRise, nameof(TextRise));

        if (TextRenderingMode is < 0 or > 7)
        {
            throw new ArgumentOutOfRangeException(
                nameof(TextRenderingMode),
                TextRenderingMode,
                "PDF text rendering mode must be between 0 and 7.");
        }

        if (FontSize is not null && FontSize == 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(FontSize),
                FontSize,
                "A user-requested font size cannot be zero.");
        }
    }

    internal bool HasChanges =>
        FontSize.HasValue ||
        CharacterSpacing.HasValue ||
        WordSpacing.HasValue ||
        HorizontalScaling.HasValue ||
        TextRenderingMode.HasValue ||
        TextRise.HasValue;

    private static void ValidateFinite(double? value, string name)
    {
        if (value.HasValue &&
            !double.IsFinite(value.Value))
        {
            throw new ArgumentOutOfRangeException(
                name,
                value,
                "Typography update values must be finite.");
        }
    }
}
