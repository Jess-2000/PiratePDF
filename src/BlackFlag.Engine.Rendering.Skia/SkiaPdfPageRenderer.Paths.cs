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
    private void RenderPath(
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

    private SKPaint CreatePaint(
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
            BlendMode = ConvertBlendMode(state.BlendMode),
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

    private SKColor ConvertColor(PdfColorValue color, double alpha)
    {
        PdfSrgbColor converted = colorConverter.Convert(color, PdfColorTarget.Srgb, alpha);
        return new SKColor(ToByte(converted.Red), ToByte(converted.Green), ToByte(converted.Blue), ToByte(converted.Alpha));
    }

    private static SKBlendMode ConvertBlendMode(BlackFlag.Engine.Objects.PdfObject? source) =>
        PdfBlendModeParser.Parse(source) switch
        {
            PdfBlendMode.Multiply => SKBlendMode.Multiply, PdfBlendMode.Screen => SKBlendMode.Screen, PdfBlendMode.Overlay => SKBlendMode.Overlay,
            PdfBlendMode.Darken => SKBlendMode.Darken, PdfBlendMode.Lighten => SKBlendMode.Lighten, PdfBlendMode.ColorDodge => SKBlendMode.ColorDodge,
            PdfBlendMode.ColorBurn => SKBlendMode.ColorBurn, PdfBlendMode.HardLight => SKBlendMode.HardLight, PdfBlendMode.SoftLight => SKBlendMode.SoftLight,
            PdfBlendMode.Difference => SKBlendMode.Difference, PdfBlendMode.Exclusion => SKBlendMode.Exclusion, PdfBlendMode.Hue => SKBlendMode.Hue,
            PdfBlendMode.Saturation => SKBlendMode.Saturation, PdfBlendMode.Color => SKBlendMode.Color, PdfBlendMode.Luminosity => SKBlendMode.Luminosity,
            _ => SKBlendMode.SrcOver
        };


}
