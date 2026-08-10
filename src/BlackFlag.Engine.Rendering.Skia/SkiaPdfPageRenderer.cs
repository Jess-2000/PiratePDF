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

/// <summary>Provides the reference CPU raster backend using SkiaSharp.</summary>
public sealed class SkiaPdfPageRenderer : IPdfPageRenderer, IDisposable
{
    private readonly PdfFontDecoder fontDecoder;
    private readonly SkiaFontRealizer fontRealizer;
    private readonly PdfImageDecoder imageDecoder;
    private readonly PdfColorConverter colorConverter;
    private readonly PdfShadingDecoder shadingDecoder;
    private readonly IPdfNestedGraphicsProvider? nestedGraphicsProvider;

    /// <summary>Initializes the Skia page renderer and its document-local font services.</summary>
    public SkiaPdfPageRenderer()
        : this(null)
    {
    }

    /// <summary>
    /// Initializes the Skia renderer with an optional nested-content provider
    /// for Type 1 tiling patterns and Type 3 glyph programs.
    /// </summary>
    public SkiaPdfPageRenderer(IPdfNestedGraphicsProvider? nestedGraphicsProvider)
    {
        fontDecoder = new PdfFontDecoder();
        fontRealizer = new SkiaFontRealizer(fontDecoder);
        imageDecoder = new PdfImageDecoder([new SkiaDctImageCodec()]);
        colorConverter = new PdfColorConverter();
        shadingDecoder = new PdfShadingDecoder();
        this.nestedGraphicsProvider = nestedGraphicsProvider;
    }
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

    private void RenderList(
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

            canvas.Save();
            try
            {
                ApplyClipState(
                    canvas,
                    item.GraphicsState.Clip,
                    request,
                    diagnostics,
                    index,
                    cancellationToken);

                switch (item)
                {
                    case PdfDisplayPath path:
                        RenderPath(canvas, path);
                        break;

                    case PdfDisplayTextRun text:
                        RenderText(
                            canvas,
                            text,
                            request,
                            diagnostics,
                            index,
                            cancellationToken);
                        break;

                    case PdfDisplayImage image:
                        RenderImage(
                            canvas,
                            image,
                            request,
                            diagnostics,
                            index,
                            cancellationToken);
                        break;

                    case PdfDisplayShading shading:
                        RenderShading(
                            canvas,
                            shading,
                            request,
                            diagnostics,
                            index,
                            cancellationToken);
                        break;

                    case PdfDisplayForm form:
                        ApplyMatrix(canvas, form.Transform);
                        RenderList(
                            canvas,
                            form.Content,
                            request,
                            diagnostics,
                            checked(depth + 1),
                            cancellationToken);
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
            finally
            {
                canvas.Restore();
            }
        }
    }


    private void RenderImage(
        SKCanvas canvas,
        PdfDisplayImage item,
        PdfRenderRequest request,
        List<PdfRenderDiagnostic> diagnostics,
        int displayItemIndex,
        CancellationToken cancellationToken)
    {
        PdfImageDecodeResult decoded;
        try
        {
            decoded = imageDecoder.Decode(
                item,
                new PdfImageDecodeLimits
                {
                    MaximumDecodedPixels = request.Limits.MaximumSurfacePixels
                },
                cancellationToken);
        }
        catch (Exception exception) when (
            exception is InvalidDataException or
            NotSupportedException or
            BlackFlag.Engine.Filters.PdfFilterDecodeException)
        {
            if (request.Mode == PdfRenderMode.Strict)
            {
                throw;
            }

            diagnostics.Add(new PdfRenderDiagnostic(
                "IMAGE_DECODE_FAILED",
                exception.Message,
                displayItemIndex));
            return;
        }

        foreach (PdfImageDecodeDiagnostic diagnostic in decoded.Diagnostics)
        {
            diagnostics.Add(new PdfRenderDiagnostic(
                diagnostic.Code,
                diagnostic.Message,
                displayItemIndex));
        }

        PdfDecodedImage image = decoded.Image;
        SKImageInfo info = new(
            image.Width,
            image.Height,
            SKColorType.Rgba8888,
            SKAlphaType.Unpremul);

        using SKBitmap bitmap = new(info);
        byte[] pixels = image.Pixels.ToArray();
        System.Runtime.InteropServices.Marshal.Copy(
            pixels,
            0,
            bitmap.GetPixels(),
            pixels.Length);

        canvas.Save();
        try
        {
            ApplyMatrix(canvas, item.Transform);
            canvas.Scale(1f / image.Width, 1f / image.Height);
            canvas.Scale(1, -1);
            canvas.Translate(0, -image.Height);

            using SKPaint paint = new()
            {
                IsAntialias = item.ImageResource?.Interpolate ?? false,
                Color = new SKColor(255, 255, 255, ToByte(item.GraphicsState.NonstrokingAlpha)),
                BlendMode = ConvertBlendMode(item.GraphicsState.BlendMode)
            };

            canvas.DrawBitmap(
                bitmap,
                0,
                0,
                SKSamplingOptions.Default,
                paint);
        }
        finally
        {
            canvas.Restore();
        }
    }

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


    private void RenderShading(
        SKCanvas canvas,
        PdfDisplayShading item,
        PdfRenderRequest request,
        List<PdfRenderDiagnostic> diagnostics,
        int displayItemIndex,
        CancellationToken cancellationToken)
    {
        PdfShadingDescriptor shading;
        try
        {
            shading = shadingDecoder.Decode(item.Shading, cancellationToken);
        }
        catch (Exception exception) when (
            exception is InvalidDataException or NotSupportedException)
        {
            if (request.Mode == PdfRenderMode.Strict)
            {
                throw;
            }

            diagnostics.Add(new PdfRenderDiagnostic(
                "SHADING_UNSUPPORTED",
                exception.Message,
                displayItemIndex));
            return;
        }

        SKColor[] colors = shading.Stops
            .Select(stop => new SKColor(
                ToByte(stop.Color.Red),
                ToByte(stop.Color.Green),
                ToByte(stop.Color.Blue),
                ToByte(stop.Color.Alpha)))
            .ToArray();

        float[] positions = shading.Stops
            .Select(stop => checked((float)stop.Offset))
            .ToArray();

        PdfPoint start = item.GraphicsState.Transform.Transform(shading.Start);
        PdfPoint end = item.GraphicsState.Transform.Transform(shading.End);

        using SKShader shader = shading.ShadingType switch
        {
            2 => SKShader.CreateLinearGradient(
                ToPoint(start),
                ToPoint(end),
                colors,
                positions,
                shading.ExtendStart && shading.ExtendEnd
                    ? SKShaderTileMode.Clamp
                    : SKShaderTileMode.Decal),

            3 => SKShader.CreateTwoPointConicalGradient(
                ToPoint(start),
                ScaleRadius(item.GraphicsState.Transform, shading.StartRadius),
                ToPoint(end),
                ScaleRadius(item.GraphicsState.Transform, shading.EndRadius),
                colors,
                positions,
                shading.ExtendStart && shading.ExtendEnd
                    ? SKShaderTileMode.Clamp
                    : SKShaderTileMode.Decal),

            _ => throw new InvalidOperationException(
                $"Unsupported canonical shading type {shading.ShadingType}.")
        };

        using SKPaint paint = new()
        {
            IsAntialias = true,
            Style = SKPaintStyle.Fill,
            Shader = shader,
            BlendMode = ConvertBlendMode(item.GraphicsState.BlendMode)
        };

        canvas.DrawRect(
            checked((float)request.Region.Left),
            checked((float)request.Region.Bottom),
            checked((float)request.Region.Right),
            checked((float)request.Region.Top),
            paint);
    }

    private void RenderType3Text(
        SKCanvas canvas,
        PdfDisplayTextRun item,
        PdfDecodedTextRun decoded,
        PdfRenderRequest request,
        List<PdfRenderDiagnostic> diagnostics,
        int displayItemIndex,
        CancellationToken cancellationToken)
    {
        if (nestedGraphicsProvider is null)
        {
            if (request.Mode == PdfRenderMode.Strict)
            {
                throw new NotSupportedException(
                    "Type 3 rendering requires an IPdfNestedGraphicsProvider.");
            }

            diagnostics.Add(new PdfRenderDiagnostic(
                "TYPE3_PROVIDER_REQUIRED",
                $"Type 3 font '{item.Font.FaceName}' requires a nested-content provider.",
                displayItemIndex));
            return;
        }

        canvas.Save();
        try
        {
            ApplyMatrix(canvas, item.TextMatrix);
            canvas.Translate(0, checked((float)item.GraphicsState.TextRise));
            canvas.Scale(
                checked((float)(item.GraphicsState.HorizontalScaling / 100.0)),
                1);

            foreach (PdfDecodedGlyph glyph in decoded.Glyphs)
            {
                cancellationToken.ThrowIfCancellationRequested();

                PdfType3GlyphProgram program;
                try
                {
                    program = nestedGraphicsProvider.GetType3Glyph(
                        item.Font,
                        glyph.SourceCode,
                        cancellationToken);
                }
                catch (Exception exception) when (
                    exception is InvalidDataException or NotSupportedException)
                {
                    if (request.Mode == PdfRenderMode.Strict)
                    {
                        throw;
                    }

                    diagnostics.Add(new PdfRenderDiagnostic(
                        "TYPE3_GLYPH_FAILED",
                        exception.Message,
                        displayItemIndex));
                    continue;
                }

                canvas.Save();
                try
                {
                    canvas.Translate(checked((float)glyph.X), 0);
                    canvas.Scale(
                        checked((float)item.FontSize),
                        checked((float)item.FontSize));
                    ApplyMatrix(canvas, program.FontMatrix);

                    RenderList(
                        canvas,
                        program.Content,
                        request,
                        diagnostics,
                        depth: 1,
                        cancellationToken);
                }
                finally
                {
                    canvas.Restore();
                }
            }
        }
        finally
        {
            canvas.Restore();
        }
    }

    private static float ScaleRadius(PdfMatrix matrix, double radius)
    {
        double sx = Math.Sqrt((matrix.A * matrix.A) + (matrix.B * matrix.B));
        double sy = Math.Sqrt((matrix.C * matrix.C) + (matrix.D * matrix.D));
        double scale = (sx + sy) / 2;
        return checked((float)(Math.Abs(radius) * scale));
    }

    private void RenderText(
        SKCanvas canvas,
        PdfDisplayTextRun item,
        PdfRenderRequest request,
        List<PdfRenderDiagnostic> diagnostics,
        int displayItemIndex,
        CancellationToken cancellationToken)
    {
        PdfDecodedTextRun decoded;
        try
        {
            decoded = fontDecoder.Decode(item, cancellationToken);
        }
        catch (Exception exception) when (
            exception is InvalidDataException or
            NotSupportedException or
            BlackFlag.Engine.Filters.PdfFilterDecodeException)
        {
            if (request.Mode == PdfRenderMode.Strict)
            {
                throw;
            }

            diagnostics.Add(new PdfRenderDiagnostic(
                "FONT_DECODE_FAILED",
                exception.Message,
                displayItemIndex));
            return;
        }

        foreach (PdfFontDecodeDiagnostic diagnostic in decoded.Diagnostics)
        {
            diagnostics.Add(new PdfRenderDiagnostic(
                diagnostic.Code,
                diagnostic.Message,
                displayItemIndex));
        }

        if (item.Font.Technology == PdfFontTechnology.Type3)
        {
            RenderType3Text(
                canvas,
                item,
                decoded,
                request,
                diagnostics,
                displayItemIndex,
                cancellationToken);
            return;
        }

        SKTypeface typeface = fontRealizer.GetTypeface(
            item.Font,
            diagnostics,
            displayItemIndex,
            cancellationToken);

        using var font = new SKFont(typeface, checked((float)item.FontSize));
        using SKPaint fillPaint = CreatePaint(
            item.GraphicsState.FillColor,
            item.GraphicsState.NonstrokingAlpha,
            SKPaintStyle.Fill,
            item.GraphicsState);
        using SKPaint strokePaint = CreatePaint(
            item.GraphicsState.StrokeColor,
            item.GraphicsState.StrokingAlpha,
            SKPaintStyle.Stroke,
            item.GraphicsState);

        canvas.Save();
        try
        {
            ApplyMatrix(canvas, item.TextMatrix);
            canvas.Translate(0, checked((float)item.GraphicsState.TextRise));
            canvas.Scale(
                checked((float)(item.GraphicsState.HorizontalScaling / 100.0)),
                1);

            foreach (PdfDecodedGlyph glyph in decoded.Glyphs)
            {
                cancellationToken.ThrowIfCancellationRequested();

                ushort? glyphId = ResolveGlyphId(font, glyph);
                if (glyphId is null or 0)
                {
                    diagnostics.Add(new PdfRenderDiagnostic(
                        "GLYPH_MISSING",
                        $"Font '{item.Font.FaceName}' has no renderable glyph for source code <{Convert.ToHexString(glyph.SourceCode.Span)}>.",
                        displayItemIndex));
                    continue;
                }

                using SKPath? outline = font.GetGlyphPath(glyphId.Value);
                if (outline is null)
                {
                    continue;
                }

                outline.Transform(
                    SKMatrix.CreateTranslation(
                        checked((float)glyph.X),
                        0));

                switch (item.GraphicsState.TextRenderingMode)
                {
                    case 0:
                    case 4:
                        canvas.DrawPath(outline, fillPaint);
                        break;

                    case 1:
                    case 5:
                        canvas.DrawPath(outline, strokePaint);
                        break;

                    case 2:
                    case 6:
                        canvas.DrawPath(outline, fillPaint);
                        canvas.DrawPath(outline, strokePaint);
                        break;

                    case 3:
                    case 7:
                        break;

                    default:
                        throw new InvalidOperationException(
                            $"Unsupported text rendering mode {item.GraphicsState.TextRenderingMode}.");
                }
            }
        }
        finally
        {
            canvas.Restore();
        }
    }

    private void ApplyClipState(
        SKCanvas canvas,
        PdfClipState? clip,
        PdfRenderRequest request,
        List<PdfRenderDiagnostic> diagnostics,
        int displayItemIndex,
        CancellationToken cancellationToken)
    {
        if (clip is null)
        {
            return;
        }

        var nodes = new Stack<PdfClipState>();
        for (PdfClipState? current = clip; current is not null; current = current.Parent)
        {
            nodes.Push(current);
        }

        while (nodes.Count > 0)
        {
            PdfClipState node = nodes.Pop();

            if (node.Kind == PdfClipKind.Path)
            {
                if (node.Path is null)
                {
                    continue;
                }

                using SKPath path = CreatePath(node.Path, node.FillRule);
                canvas.ClipPath(path, SKClipOperation.Intersect, antialias: true);
                continue;
            }

            using SKPath? textPath = CreateTextClipPath(
                node.TextRuns,
                request,
                diagnostics,
                displayItemIndex,
                cancellationToken);

            if (textPath is not null)
            {
                canvas.ClipPath(
                    textPath,
                    SKClipOperation.Intersect,
                    antialias: true);
            }
        }
    }

    private SKPath? CreateTextClipPath(
        IReadOnlyList<PdfDisplayTextRun> runs,
        PdfRenderRequest request,
        List<PdfRenderDiagnostic> diagnostics,
        int displayItemIndex,
        CancellationToken cancellationToken)
    {
        using SKPathBuilder builder = new()
        {
            FillType = SKPathFillType.Winding
        };

        bool hasOutline = false;

        foreach (PdfDisplayTextRun run in runs)
        {
            PdfDecodedTextRun decoded;
            try
            {
                decoded = fontDecoder.Decode(run, cancellationToken);
            }
            catch (Exception exception) when (
                exception is InvalidDataException or
                NotSupportedException or
                BlackFlag.Engine.Filters.PdfFilterDecodeException)
            {
                if (request.Mode == PdfRenderMode.Strict)
                {
                    throw;
                }

                diagnostics.Add(new PdfRenderDiagnostic(
                    "TEXT_CLIP_DECODE_FAILED",
                    exception.Message,
                    displayItemIndex));
                continue;
            }

            SKTypeface typeface = fontRealizer.GetTypeface(
                run.Font,
                diagnostics,
                displayItemIndex,
                cancellationToken);

            using var font = new SKFont(typeface, checked((float)run.FontSize));

            foreach (PdfDecodedGlyph glyph in decoded.Glyphs)
            {
                ushort? glyphId = ResolveGlyphId(font, glyph);
                if (glyphId is null or 0)
                {
                    continue;
                }

                using SKPath? outline = font.GetGlyphPath(glyphId.Value);
                if (outline is null)
                {
                    continue;
                }

                outline.Transform(
                    new SKMatrix
                    {
                        ScaleX = checked((float)(run.GraphicsState.HorizontalScaling / 100.0)),
                        SkewX = 0,
                        TransX = checked((float)(glyph.X * run.GraphicsState.HorizontalScaling / 100.0)),
                        SkewY = 0,
                        ScaleY = 1,
                        TransY = checked((float)run.GraphicsState.TextRise),
                        Persp0 = 0,
                        Persp1 = 0,
                        Persp2 = 1
                    });

                outline.Transform(ToSkMatrix(run.TextMatrix));
                builder.AddPath(outline);
                hasOutline = true;
            }
        }

        return hasOutline ? builder.Detach() : null;
    }

    private static ushort? ResolveGlyphId(
        SKFont font,
        PdfDecodedGlyph glyph)
    {
        if (glyph.GlyphId is ushort explicitGlyph)
        {
            return explicitGlyph;
        }

        if (string.IsNullOrEmpty(glyph.UnicodeText))
        {
            return null;
        }

        ushort[] glyphs = font.GetGlyphs(glyph.UnicodeText);
        return glyphs.Length > 0 ? glyphs[0] : null;
    }

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
    public void Dispose() =>
        fontRealizer.Dispose();

    private static byte ToByte(double value) =>
        checked((byte)Math.Round(Clamp01(value) * byte.MaxValue));

    private static double Clamp01(double value) =>
        Math.Clamp(value, 0, 1);
}
