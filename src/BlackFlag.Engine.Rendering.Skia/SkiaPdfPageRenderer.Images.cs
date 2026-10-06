using BlackFlag.Engine.Rendering.DisplayList;
using BlackFlag.Engine.Rendering.Images;
using BlackFlag.Engine.Rendering.Raster;
using SkiaSharp;

namespace BlackFlag.Engine.Rendering.Skia;

public sealed partial class SkiaPdfPageRenderer : IPdfPageRenderer, IDisposable
{
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

}
