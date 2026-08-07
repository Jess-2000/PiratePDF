namespace BlackFlag.Engine.Rendering.DisplayList;

/// <summary>
/// Represents one immutable clipping node with optional ancestor clipping.
/// </summary>
public sealed class PdfClipState
{
    /// <summary>Initializes a clipping state.</summary>
    public PdfClipState(
        PdfPathGeometry path,
        PdfFillRule fillRule,
        PdfClipState? parent)
    {
        ArgumentNullException.ThrowIfNull(path);
        Path = path;
        FillRule = fillRule;
        Parent = parent;
        Depth = parent is null ? 1 : checked(parent.Depth + 1);
    }

    /// <summary>Gets the clipping path.</summary>
    public PdfPathGeometry Path { get; }

    /// <summary>Gets the clipping fill rule.</summary>
    public PdfFillRule FillRule { get; }

    /// <summary>Gets the preceding clipping state.</summary>
    public PdfClipState? Parent { get; }

    /// <summary>Gets the one-based clipping depth.</summary>
    public int Depth { get; }
}
