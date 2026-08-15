using BlackFlag.Engine.Rendering.DisplayList;
using BlackFlag.Engine.Rendering.Color;
using BlackFlag.Engine.Rendering.Fonts;
using BlackFlag.Engine.Rendering.Images;
using BlackFlag.Engine.Rendering.Skia.Images;
using BlackFlag.Engine.Rendering.Raster;
using BlackFlag.Engine.Rendering.Skia.Fonts;
using BlackFlag.Engine.Rendering.Shadings;
using BlackFlag.Engine.Rendering.Nested;
using BlackFlag.Engine.Rendering.Resources;
using SkiaSharp;

namespace BlackFlag.Engine.Rendering.Skia;

public sealed partial class SkiaPdfPageRenderer : IPdfPageRenderer, IDisposable
{
    private static SKMatrix ToSkMatrix(PdfMatrix matrix) =>
        new()
        {
            ScaleX = checked((float)matrix.A),
            SkewX = checked((float)matrix.C),
            TransX = checked((float)matrix.E),
            SkewY = checked((float)matrix.B),
            ScaleY = checked((float)matrix.D),
            TransY = checked((float)matrix.F),
            Persp0 = 0,
            Persp1 = 0,
            Persp2 = 1
        };

    private static void ApplyMatrix(
        SKCanvas canvas,
        PdfMatrix matrix)
    {
        SKMatrix skMatrix = new()
        {
            ScaleX = checked((float)matrix.A),
            SkewX = checked((float)matrix.C),
            TransX = checked((float)matrix.E),
            SkewY = checked((float)matrix.B),
            ScaleY = checked((float)matrix.D),
            TransY = checked((float)matrix.F),
            Persp0 = 0,
            Persp1 = 0,
            Persp2 = 1
        };

        canvas.Concat(skMatrix);
    }

    private static SKPoint ToPoint(PdfPoint point) =>
        new(checked((float)point.X), checked((float)point.Y));

    private static bool Intersects(
        PdfDisplayBounds? bounds,
        PdfRenderRegion region)
    {
        if (bounds is null)
        {
            return true;
        }

        PdfDisplayBounds value = bounds.Value;
        return value.Right >= region.Left &&
            value.Left <= region.Right &&
            value.Top >= region.Bottom &&
            value.Bottom <= region.Top;
    }

    private static void HandleUnsupported(
        PdfRenderRequest request,
        List<PdfRenderDiagnostic> diagnostics,
        int index,
        string itemType)
    {
        string message =
            $"BF-1048D renderer does not yet rasterize display item '{itemType}'.";

        if (request.Mode == PdfRenderMode.Strict)
        {
            throw new NotSupportedException(message);
        }

        diagnostics.Add(
            new PdfRenderDiagnostic(
                "BF1048D_UNSUPPORTED_DISPLAY_ITEM",
                message,
                index));
    }

    /// <summary>Releases cached backend font resources.</summary>
}
