namespace BlackFlag.Engine.Rendering.Raster;

/// <summary>Defines bounded resource limits for one render request.</summary>
public sealed record PdfRenderLimits
{
    /// <summary>Gets the default production rendering limits.</summary>
    public static PdfRenderLimits Default { get; } = new();

    /// <summary>Gets the maximum raster pixel count.</summary>
    public long MaximumSurfacePixels { get; init; } = 100_000_000;

    /// <summary>Gets the maximum recursive Form depth.</summary>
    public int MaximumFormDepth { get; init; } = 64;

    /// <summary>Gets the maximum nested Type 3 glyph depth.</summary>
    public int MaximumType3GlyphDepth { get; init; } = 32;

    /// <summary>Gets the maximum tiling-pattern cells rendered per operation.</summary>
    public int MaximumPatternCells { get; init; } = 100_000;

    /// <summary>Gets the maximum sampled stops used for one shading.</summary>
    public int MaximumShadingSamples { get; init; } = 4096;

    /// <summary>Validates the configured limits.</summary>
    public void Validate()
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(MaximumSurfacePixels);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(MaximumFormDepth);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(MaximumType3GlyphDepth);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(MaximumPatternCells);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(MaximumShadingSamples);
    }
}
