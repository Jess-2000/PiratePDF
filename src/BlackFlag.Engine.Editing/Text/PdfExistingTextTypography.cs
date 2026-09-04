namespace BlackFlag.Engine.Editing.Text;

/// <summary>
/// Captures the interpreted PDF text-state values active at one existing-text
/// target without changing font identity or resource ownership.
/// </summary>
public sealed class PdfExistingTextTypography
{
    public PdfExistingTextTypography(
        double fontSize,
        double characterSpacing,
        double wordSpacing,
        double horizontalScaling,
        int textRenderingMode,
        double textRise)
    {
        ValidateFinite(fontSize, nameof(fontSize));
        ValidateFinite(characterSpacing, nameof(characterSpacing));
        ValidateFinite(wordSpacing, nameof(wordSpacing));
        ValidateFinite(horizontalScaling, nameof(horizontalScaling));
        ValidateFinite(textRise, nameof(textRise));

        if (textRenderingMode is < 0 or > 7)
        {
            throw new ArgumentOutOfRangeException(
                nameof(textRenderingMode),
                textRenderingMode,
                "PDF text rendering mode must be between 0 and 7.");
        }

        FontSize = fontSize;
        CharacterSpacing = characterSpacing;
        WordSpacing = wordSpacing;
        HorizontalScaling = horizontalScaling;
        TextRenderingMode = textRenderingMode;
        TextRise = textRise;
    }

    public double FontSize { get; }
    public double CharacterSpacing { get; }
    public double WordSpacing { get; }
    public double HorizontalScaling { get; }
    public int TextRenderingMode { get; }
    public double TextRise { get; }

    private static void ValidateFinite(double value, string name)
    {
        if (!double.IsFinite(value))
        {
            throw new ArgumentOutOfRangeException(
                name,
                value,
                "Existing-text typography values must be finite.");
        }
    }
}
