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
                PdfRenderingDiagnosticCodes.ShadingMalformed,
                $"The PDF shading could not be rendered because its definition is malformed or unsupported: {exception.Message}",
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

}
