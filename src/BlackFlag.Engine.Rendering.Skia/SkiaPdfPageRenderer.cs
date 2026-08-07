using BlackFlag.Engine.Rendering.DisplayList;
using BlackFlag.Engine.Rendering.Raster;
using SkiaSharp;

namespace BlackFlag.Engine.Rendering.Skia;

/// <summary>Provides the reference CPU raster backend using SkiaSharp.</summary>
public sealed class SkiaPdfPageRenderer : IPdfPageRenderer
{
    /// <inheritdoc />
    public PdfRenderResult Render(
        PdfDisplayList displayList,
        PdfRenderRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(displayList);
        ArgumentNullException.ThrowIfNull(request);
        request.Validate();

        List<PdfRenderDiagnostic> diagnostics = [];

        SKImageInfo info = new(
            request.PixelWidth,
            request.PixelHeight,
            SKColorType.Rgba8888,
            SKAlphaType.Premul);

        using SKBitmap bitmap = new(info);
        using SKCanvas canvas = new(bitmap);
        canvas.Clear(SKColors.White);

        ApplyViewport(canvas, request);

        RenderList(
            canvas,
            displayList,
            request,
            diagnostics,
            0,
            cancellationToken);

        int byteCount = checked(info.RowBytes * info.Height);
        byte[] pixels = new byte[byteCount];
        System.Runtime.InteropServices.Marshal.Copy(
            bitmap.GetPixels(),
            pixels,
            0,
            byteCount);

        PdfRasterSurface surface = new(
            info.Width,
            info.Height,
            info.RowBytes,
            PdfPixelFormat.Rgba8888Premultiplied,
            pixels);

        return new PdfRenderResult(surface, diagnostics);
    }

    private static void ApplyViewport(
        SKCanvas canvas,
        PdfRenderRequest request)
    {
        float scaleX = checked((float)(request.PixelWidth / request.Region.Width));
        float scaleY = checked((float)(request.PixelHeight / request.Region.Height));

        canvas.Scale(scaleX, -scaleY);
        canvas.Translate(
            checked((float)-request.Region.Left),
            checked((float)-request.Region.Top));
    }

    private static void RenderList(
        SKCanvas canvas,
        PdfDisplayList displayList,
        PdfRenderRequest request,
        List<PdfRenderDiagnostic> diagnostics,
        int depth,
        CancellationToken cancellationToken)
    {
        if (depth > request.Limits.MaximumFormDepth)
        {
            throw new InvalidOperationException(
                $"Form rendering depth exceeds {request.Limits.MaximumFormDepth}.");
        }

        for (int index = 0; index < displayList.Count; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            PdfDisplayItem item = displayList[index];
            if (!Intersects(item.Bounds, request.Region))
            {
                continue;
            }

            switch (item)
            {
                case PdfDisplayPath path:
                    RenderPath(canvas, path);
                    break;

                case PdfDisplayForm form:
                    canvas.Save();
                    try
                    {
                        ApplyMatrix(canvas, form.Transform);
                        RenderList(
                            canvas,
                            form.Content,
                            request,
                            diagnostics,
                            checked(depth + 1),
                            cancellationToken);
                    }
                    finally
                    {
                        canvas.Restore();
                    }

                    break;

                default:
                    HandleUnsupported(
                        request,
                        diagnostics,
                        index,
                        item.GetType().Name);
                    break;
            }
        }
    }

    private static void RenderPath(
        SKCanvas canvas,
        PdfDisplayPath item)
    {
        using SKPath path = CreatePath(item.Path, item.FillRule);

        if (item.Fill)
        {
            using SKPaint fillPaint = CreatePaint(
                item.GraphicsState.FillColor,
                item.GraphicsState.NonstrokingAlpha,
                SKPaintStyle.Fill,
                item.GraphicsState);

            canvas.DrawPath(path, fillPaint);
        }

        if (item.Stroke)
        {
            using SKPaint strokePaint = CreatePaint(
                item.GraphicsState.StrokeColor,
                item.GraphicsState.StrokingAlpha,
                SKPaintStyle.Stroke,
                item.GraphicsState);

            canvas.DrawPath(path, strokePaint);
        }
    }

    private static SKPath CreatePath(
        PdfPathGeometry geometry,
        PdfFillRule fillRule)
    {
        using SKPathBuilder builder = new()
        {
            FillType = fillRule == PdfFillRule.EvenOdd
                ? SKPathFillType.EvenOdd
                : SKPathFillType.Winding
        };

        foreach (PdfPathSegment segment in geometry.Segments)
        {
            switch (segment.Kind)
            {
                case PdfPathSegmentKind.MoveTo:
                    builder.MoveTo(ToPoint(segment.EndPoint));
                    break;

                case PdfPathSegmentKind.LineTo:
                    builder.LineTo(ToPoint(segment.EndPoint));
                    break;

                case PdfPathSegmentKind.CubicBezier:
                    if (segment.ControlPoint1 is not PdfPoint control1 ||
                        segment.ControlPoint2 is not PdfPoint control2)
                    {
                        throw new InvalidOperationException(
                            "Cubic Bézier segments require two control points.");
                    }

                    builder.CubicTo(
                        ToPoint(control1),
                        ToPoint(control2),
                        ToPoint(segment.EndPoint));
                    break;

                case PdfPathSegmentKind.ClosePath:
                    builder.Close();
                    break;

                default:
                    throw new InvalidOperationException(
                        $"Unsupported path segment kind '{segment.Kind}'.");
            }
        }

        return builder.Detach();
    }

    private static SKPaint CreatePaint(
        PdfColorValue color,
        double alpha,
        SKPaintStyle style,
        PdfGraphicsStateSnapshot state)
    {
        SKPaint paint = new()
        {
            IsAntialias = true,
            Style = style,
            Color = ConvertColor(color, alpha),
            StrokeWidth = state.LineWidth == 0
                ? 0
                : checked((float)state.LineWidth),
            StrokeMiter = checked((float)state.MiterLimit),
            StrokeCap = state.LineCap switch
            {
                PdfLineCapStyle.Butt => SKStrokeCap.Butt,
                PdfLineCapStyle.Round => SKStrokeCap.Round,
                PdfLineCapStyle.ProjectingSquare => SKStrokeCap.Square,
                _ => SKStrokeCap.Butt
            },
            StrokeJoin = state.LineJoin switch
            {
                PdfLineJoinStyle.Miter => SKStrokeJoin.Miter,
                PdfLineJoinStyle.Round => SKStrokeJoin.Round,
                PdfLineJoinStyle.Bevel => SKStrokeJoin.Bevel,
                _ => SKStrokeJoin.Miter
            }
        };

        IReadOnlyList<double> dash = state.DashPattern.Elements;
        if (style == SKPaintStyle.Stroke && dash.Count > 0)
        {
            float[] intervals = dash.Select(value => checked((float)value)).ToArray();
            paint.PathEffect = SKPathEffect.CreateDash(
                intervals,
                checked((float)state.DashPattern.Phase));
        }

        return paint;
    }

    private static SKColor ConvertColor(
        PdfColorValue color,
        double alpha)
    {
        byte a = ToByte(alpha);

        if (string.Equals(color.ColorSpaceName, "DeviceGray", StringComparison.Ordinal) &&
            color.Components.Count >= 1)
        {
            byte gray = ToByte(color.Components[0]);
            return new SKColor(gray, gray, gray, a);
        }

        if (string.Equals(color.ColorSpaceName, "DeviceRGB", StringComparison.Ordinal) &&
            color.Components.Count >= 3)
        {
            return new SKColor(
                ToByte(color.Components[0]),
                ToByte(color.Components[1]),
                ToByte(color.Components[2]),
                a);
        }

        if (string.Equals(color.ColorSpaceName, "DeviceCMYK", StringComparison.Ordinal) &&
            color.Components.Count >= 4)
        {
            double c = Clamp01(color.Components[0]);
            double m = Clamp01(color.Components[1]);
            double y = Clamp01(color.Components[2]);
            double k = Clamp01(color.Components[3]);

            return new SKColor(
                ToByte((1 - c) * (1 - k)),
                ToByte((1 - m) * (1 - k)),
                ToByte((1 - y) * (1 - k)),
                a);
        }

        return new SKColor(0, 0, 0, a);
    }

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
            $"BF-1048B core renderer does not yet rasterize display item '{itemType}'.";

        if (request.Mode == PdfRenderMode.Strict)
        {
            throw new NotSupportedException(message);
        }

        diagnostics.Add(
            new PdfRenderDiagnostic(
                "BF1048B_UNSUPPORTED_DISPLAY_ITEM",
                message,
                index));
    }

    private static byte ToByte(double value) =>
        checked((byte)Math.Round(Clamp01(value) * byte.MaxValue));

    private static double Clamp01(double value) =>
        Math.Clamp(value, 0, 1);
}
