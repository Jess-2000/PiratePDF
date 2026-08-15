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
public sealed partial class SkiaPdfPageRenderer : IPdfPageRenderer, IDisposable
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


    public void Dispose() =>
        fontRealizer.Dispose();

    private static byte ToByte(double value) =>
        checked((byte)Math.Round(Clamp01(value) * byte.MaxValue));

    private static double Clamp01(double value) =>
        Math.Clamp(value, 0, 1);
}
