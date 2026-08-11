namespace BlackFlag.Engine.Rendering.Raster;

/// <summary>Defines a page-space rectangle requested for rasterization.</summary>
public readonly record struct PdfRenderRegion(
    double Left,
    double Bottom,
    double Right,
    double Top)
{
    /// <summary>Gets the region width in page-space units.</summary>
    public double Width => Right - Left;

    /// <summary>Gets the region height in page-space units.</summary>
    public double Height => Top - Bottom;

    /// <summary>Validates that the region has finite positive dimensions.</summary>
    public void Validate()
    {
        if (!double.IsFinite(Left) ||
            !double.IsFinite(Bottom) ||
            !double.IsFinite(Right) ||
            !double.IsFinite(Top) ||
            Width <= 0 ||
            Height <= 0)
        {
            throw new InvalidOperationException(
                "Render regions must have finite positive dimensions.");
        }
    }
}
