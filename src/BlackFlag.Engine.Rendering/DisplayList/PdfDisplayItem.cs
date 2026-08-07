using BlackFlag.Engine.Rendering.Graphics;

namespace BlackFlag.Engine.Rendering.DisplayList;

/// <summary>Provides the immutable base contract for semantic display-list items.</summary>
public abstract class PdfDisplayItem
{
    /// <summary>Initializes a display-list item.</summary>
    protected PdfDisplayItem(
        PdfGraphicsStateSnapshot graphicsState,
        PdfContentSourceSpan sourceSpan,
        PdfDisplayBounds? bounds,
        PdfMarkedContentContext? markedContent)
    {
        ArgumentNullException.ThrowIfNull(graphicsState);
        GraphicsState = graphicsState;
        SourceSpan = sourceSpan;
        Bounds = bounds;
        MarkedContent = markedContent;
    }

    /// <summary>Gets the graphics state effective for this item.</summary>
    public PdfGraphicsStateSnapshot GraphicsState { get; }

    /// <summary>Gets the source operation range.</summary>
    public PdfContentSourceSpan SourceSpan { get; }

    /// <summary>Gets conservative page-space bounds when known.</summary>
    public PdfDisplayBounds? Bounds { get; }

    /// <summary>Gets active marked-content context when present.</summary>
    public PdfMarkedContentContext? MarkedContent { get; }
}
