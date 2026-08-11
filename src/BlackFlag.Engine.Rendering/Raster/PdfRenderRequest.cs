namespace BlackFlag.Engine.Rendering.Raster;

/// <summary>Defines one deterministic page-region rasterization request.</summary>
public sealed record PdfRenderRequest
{
    /// <summary>Gets the page-space region to rasterize.</summary>
    public required PdfRenderRegion Region { get; init; }

    /// <summary>Gets the output width in device pixels.</summary>
    public required int PixelWidth { get; init; }

    /// <summary>Gets the output height in device pixels.</summary>
    public required int PixelHeight { get; init; }

    /// <summary>Gets the backend-neutral quality policy.</summary>
    public PdfRenderQuality Quality { get; init; } = PdfRenderQuality.Normal;

    /// <summary>Gets strict or tolerant unsupported-content behavior.</summary>
    public PdfRenderMode Mode { get; init; } = PdfRenderMode.Tolerant;

    /// <summary>Gets bounded resource limits.</summary>
    public PdfRenderLimits Limits { get; init; } = PdfRenderLimits.Default;

    /// <summary>Validates the request.</summary>
    public void Validate()
    {
        Region.Validate();
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(PixelWidth);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(PixelHeight);
        ArgumentNullException.ThrowIfNull(Limits);
        Limits.Validate();

        long pixels = checked((long)PixelWidth * PixelHeight);
        if (pixels > Limits.MaximumSurfacePixels)
        {
            throw new InvalidOperationException(
                $"Requested surface contains {pixels} pixels; limit is {Limits.MaximumSurfacePixels}.");
        }
    }
}
