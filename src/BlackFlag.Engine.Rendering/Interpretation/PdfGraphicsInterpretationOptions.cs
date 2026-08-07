namespace BlackFlag.Engine.Rendering.Interpretation;

/// <summary>Configures defensive limits for graphics interpretation.</summary>
public sealed class PdfGraphicsInterpretationOptions
{
    /// <summary>Gets the maximum graphics-state stack depth.</summary>
    public int MaximumGraphicsStateDepth { get; init; } = 128;

    /// <summary>Gets the maximum Form XObject nesting depth.</summary>
    public int MaximumFormDepth { get; init; } = 64;

    /// <summary>Gets the maximum marked-content nesting depth.</summary>
    public int MaximumMarkedContentDepth { get; init; } = 128;

    /// <summary>Gets the maximum path-segment count before a path is consumed.</summary>
    public int MaximumPathSegmentCount { get; init; } = 1_000_000;

    /// <summary>Gets the maximum display-item count for one interpretation.</summary>
    public int MaximumDisplayItemCount { get; init; } = 5_000_000;

    /// <summary>Gets the maximum clipping depth.</summary>
    public int MaximumClipDepth { get; init; } = 256;

    /// <summary>Gets whether unknown compatibility operations are ignored.</summary>
    public bool IgnoreUnknownOperations { get; init; } = true;

    internal void Validate()
    {
        if (MaximumGraphicsStateDepth <= 0
            || MaximumFormDepth <= 0
            || MaximumMarkedContentDepth <= 0
            || MaximumPathSegmentCount <= 0
            || MaximumDisplayItemCount <= 0
            || MaximumClipDepth <= 0)
        {
            throw new InvalidOperationException(
                "All graphics interpretation limits must be greater than zero.");
        }
    }
}
