using BlackFlag.Engine.Rendering.Graphics;
using BlackFlag.Engine.Rendering.Resources;

namespace BlackFlag.Engine.Rendering.DisplayList;

/// <summary>Represents a semantic shading paint operation.</summary>
public sealed class PdfDisplayShading : PdfDisplayItem
{
    /// <summary>Initializes a shading display item.</summary>
    public PdfDisplayShading(
        PdfResolvedShading shading,
        PdfGraphicsStateSnapshot graphicsState,
        PdfContentSourceSpan sourceSpan,
        PdfMarkedContentContext? markedContent)
        : base(graphicsState, sourceSpan, null, markedContent)
    {
        ArgumentNullException.ThrowIfNull(shading);
        Shading = shading;
    }

    /// <summary>Gets the resolved shading resource.</summary>
    public PdfResolvedShading Shading { get; }
}
