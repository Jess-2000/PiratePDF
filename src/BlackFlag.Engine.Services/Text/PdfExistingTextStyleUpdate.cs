namespace BlackFlag.Engine.Services.Text;

/// <summary>
/// Describes higher-level existing-text style semantics layered over the
/// existing typography-state editor.
/// </summary>
public sealed class PdfExistingTextStyleUpdate
{
    public PdfExistingTextScriptStyle? ScriptStyle { get; init; }
    public double SuperscriptScale { get; init; } = 0.65;
    public double SuperscriptRise { get; init; } = 0.35;
    public double SubscriptScale { get; init; } = 0.65;
    public double SubscriptDrop { get; init; } = 0.20;
    public PdfExistingTextColor? FillColor { get; init; }
    public PdfExistingTextColor? StrokeColor { get; init; }
    public PdfExistingTextDecoration? Decoration { get; init; }

    internal void Validate()
    {
        if (ScriptStyle.HasValue &&
            !Enum.IsDefined(ScriptStyle.Value))
        {
            throw new ArgumentOutOfRangeException(nameof(ScriptStyle));
        }

        ValidateFraction(SuperscriptScale, nameof(SuperscriptScale));
        ValidateFraction(SubscriptScale, nameof(SubscriptScale));
        ValidateNonNegative(SuperscriptRise, nameof(SuperscriptRise));
        ValidateNonNegative(SubscriptDrop, nameof(SubscriptDrop));

        if (!HasChanges)
        {
            throw new ArgumentException(
                "At least one existing-text style value must change.");
        }
    }

    internal bool HasChanges =>
        ScriptStyle.HasValue ||
        FillColor is not null ||
        StrokeColor is not null ||
        Decoration is not null;

    private static void ValidateFraction(double value, string name)
    {
        if (!double.IsFinite(value) ||
            value <= 0 ||
            value > 1)
        {
            throw new ArgumentOutOfRangeException(
                name,
                value,
                "Script scale values must be finite and greater than zero through one.");
        }
    }

    private static void ValidateNonNegative(double value, string name)
    {
        if (!double.IsFinite(value) || value < 0)
        {
            throw new ArgumentOutOfRangeException(
                name,
                value,
                "Script offset factors must be finite and non-negative.");
        }
    }
}
