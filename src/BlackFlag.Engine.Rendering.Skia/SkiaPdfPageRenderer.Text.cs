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

}
