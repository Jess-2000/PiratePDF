using System.Collections.ObjectModel;
using BlackFlag.Engine.Rendering.Resources;

namespace BlackFlag.Engine.Rendering.DisplayList;

/// <summary>
/// Preserves a color value in its canonical PDF color-space semantics.
/// </summary>
public sealed class PdfColorValue
{
    private readonly ReadOnlyCollection<double> components;

    /// <summary>Initializes a color value.</summary>
    public PdfColorValue(
        string colorSpaceName,
        IEnumerable<double> components,
        PdfResolvedColorSpace? resource = null,
        PdfResolvedPattern? pattern = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(colorSpaceName);
        ArgumentNullException.ThrowIfNull(components);

        ColorSpaceName = colorSpaceName;
        this.components = Array.AsReadOnly(components.ToArray());
        Resource = resource;
        Pattern = pattern;
    }

    /// <summary>Gets the canonical or PDF color-space name.</summary>
    public string ColorSpaceName { get; }

    /// <summary>Gets color components without converting them to RGB.</summary>
    public IReadOnlyList<double> Components => components;

    /// <summary>Gets a resolved custom color-space resource when applicable.</summary>
    public PdfResolvedColorSpace? Resource { get; }

    /// <summary>Gets the resolved pattern when a pattern color is active.</summary>
    public PdfResolvedPattern? Pattern { get; }
}
