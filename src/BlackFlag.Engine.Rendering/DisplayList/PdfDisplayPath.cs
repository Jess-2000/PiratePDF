using BlackFlag.Engine.Rendering.Graphics;

namespace BlackFlag.Engine.Rendering.DisplayList;

/// <summary>Represents an interpreted path painting operation.</summary>
public sealed class PdfDisplayPath : PdfDisplayItem
{
    /// <summary>Initializes a path display item.</summary>
    public PdfDisplayPath(
        PdfPathGeometry path,
        bool fill,
        bool stroke,
        PdfFillRule fillRule,
        PdfGraphicsStateSnapshot graphicsState,
        PdfContentSourceSpan sourceSpan,
        PdfMarkedContentContext? markedContent)
        : base(graphicsState, sourceSpan, path.GetBounds(), markedContent)
    {
        ArgumentNullException.ThrowIfNull(path);
        Path = path;
        Fill = fill;
        Stroke = stroke;
        FillRule = fillRule;
    }

    /// <summary>Gets immutable path geometry.</summary>
    public PdfPathGeometry Path { get; }

    /// <summary>Gets a value indicating whether the path is filled.</summary>
    public bool Fill { get; }

    /// <summary>Gets a value indicating whether the path is stroked.</summary>
    public bool Stroke { get; }

    /// <summary>Gets the fill rule.</summary>
    public PdfFillRule FillRule { get; }
}
