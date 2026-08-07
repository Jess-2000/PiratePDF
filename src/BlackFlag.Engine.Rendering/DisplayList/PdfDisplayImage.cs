using BlackFlag.Engine.Rendering.Graphics;
using BlackFlag.Engine.Rendering.Resources;

namespace BlackFlag.Engine.Rendering.DisplayList;

/// <summary>
/// Represents one canonical image placement from an Image XObject or inline
/// image without eagerly decoding pixels.
/// </summary>
public sealed class PdfDisplayImage : PdfDisplayItem
{
    /// <summary>Initializes an image display item.</summary>
    public PdfDisplayImage(
        PdfResolvedImageXObject? imageResource,
        PdfInlineImage? inlineImage,
        PdfMatrix transform,
        PdfGraphicsStateSnapshot graphicsState,
        PdfContentSourceSpan sourceSpan,
        PdfMarkedContentContext? markedContent)
        : base(
            graphicsState,
            sourceSpan,
            GetUnitSquareBounds(transform),
            markedContent)
    {
        if ((imageResource is null) == (inlineImage is null))
        {
            throw new ArgumentException(
                "Exactly one image source must be supplied.");
        }

        ImageResource = imageResource;
        InlineImage = inlineImage;
        Transform = transform;
    }

    /// <summary>Gets the resolved Image XObject when this is an XObject placement.</summary>
    public PdfResolvedImageXObject? ImageResource { get; }

    /// <summary>Gets inline-image syntax when this placement came from BI/ID/EI.</summary>
    public PdfInlineImage? InlineImage { get; }

    /// <summary>Gets the image-to-page transformation.</summary>
    public PdfMatrix Transform { get; }

    private static PdfDisplayBounds GetUnitSquareBounds(PdfMatrix transform)
    {
        PdfPoint p0 = transform.Transform(new PdfPoint(0, 0));
        PdfPoint p1 = transform.Transform(new PdfPoint(1, 0));
        PdfPoint p2 = transform.Transform(new PdfPoint(0, 1));
        PdfPoint p3 = transform.Transform(new PdfPoint(1, 1));

        double left = Math.Min(Math.Min(p0.X, p1.X), Math.Min(p2.X, p3.X));
        double right = Math.Max(Math.Max(p0.X, p1.X), Math.Max(p2.X, p3.X));
        double bottom = Math.Min(Math.Min(p0.Y, p1.Y), Math.Min(p2.Y, p3.Y));
        double top = Math.Max(Math.Max(p0.Y, p1.Y), Math.Max(p2.Y, p3.Y));

        return new PdfDisplayBounds(left, bottom, right, top);
    }
}
