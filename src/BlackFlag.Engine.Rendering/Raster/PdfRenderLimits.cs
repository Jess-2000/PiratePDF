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

    /// <summary>Gets the maximum shared work units for one render request.</summary>
    public long MaximumRenderWorkUnits { get; init; } = 5_000_000;

    /// <summary>Gets the maximum nested tiling-pattern depth.</summary>
    public int MaximumPatternNesting { get; init; } = 32;

    /// <summary>Gets the maximum mesh vertices decoded for one shading.</summary>
    public int MaximumMeshVertices { get; init; } = 1_000_000;

    /// <summary>Gets the maximum mesh patches decoded for one shading.</summary>
    public int MaximumMeshPatches { get; init; } = 250_000;

    /// <summary>Gets the maximum transparency nesting depth.</summary>
    public int MaximumTransparencyDepth { get; init; } = 32;

    /// <summary>Gets the maximum offscreen transparency-group pixel count.</summary>
    public long MaximumTransparencyGroupPixels { get; init; } = 100_000_000;

    /// <summary>Gets the maximum soft-mask pixel count.</summary>
    public long MaximumSoftMaskPixels { get; init; } = 100_000_000;

    /// <summary>Validates the configured limits.</summary>
    public void Validate()
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(MaximumSurfacePixels);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(MaximumFormDepth);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(MaximumType3GlyphDepth);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(MaximumPatternCells);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(MaximumShadingSamples);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(MaximumRenderWorkUnits);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(MaximumPatternNesting);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(MaximumMeshVertices);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(MaximumMeshPatches);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(MaximumTransparencyDepth);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(MaximumTransparencyGroupPixels);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(MaximumSoftMaskPixels);
    }
}
