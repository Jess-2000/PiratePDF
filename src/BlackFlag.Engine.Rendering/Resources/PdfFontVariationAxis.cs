namespace BlackFlag.Engine.Rendering.Resources;

/// <summary>
/// Describes one OpenType variable-font design axis.
/// </summary>
public readonly record struct PdfFontVariationAxis
{
    /// <summary>
    /// Initializes a variable-font axis descriptor.
    /// </summary>
    public PdfFontVariationAxis(
        string tag,
        double minimum,
        double defaultValue,
        double maximum)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tag);

        if (tag.Length != 4)
        {
            throw new ArgumentException(
                "An OpenType variation-axis tag must contain exactly four characters.",
                nameof(tag));
        }

        if (minimum > defaultValue || defaultValue > maximum)
        {
            throw new ArgumentOutOfRangeException(
                nameof(defaultValue),
                defaultValue,
                "The default axis value must lie within the axis range.");
        }

        Tag = tag;
        Minimum = minimum;
        DefaultValue = defaultValue;
        Maximum = maximum;
    }

    /// <summary>Gets the four-character OpenType axis tag.</summary>
    public string Tag { get; }

    /// <summary>Gets the minimum supported axis value.</summary>
    public double Minimum { get; }

    /// <summary>Gets the default axis value.</summary>
    public double DefaultValue { get; }

    /// <summary>Gets the maximum supported axis value.</summary>
    public double Maximum { get; }
}
