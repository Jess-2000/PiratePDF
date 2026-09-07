using System.IO;
using BlackFlag.Engine.Rendering.DisplayList;
using BlackFlag.Engine.Rendering.Raster;

namespace BlackFlag.Engine.Rendering.Skia.Tests.Images;

public sealed class SkiaJpegRenderingTests
{
    [Fact]
    public void TolerantRendererReportsIntrinsicPixelLimitFailure()
    {
        var image = SkiaJpegTestData.Display(SkiaJpegTestData.Create());
        using var renderer = new SkiaPdfPageRenderer();

        PdfRenderResult result = renderer.Render(new PdfDisplayList([image]), Request(5, PdfRenderMode.Tolerant));

        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("IMAGE_DECODE_FAILED", diagnostic.Code);
        Assert.Equal("Decoded image exceeds the configured pixel limit.", diagnostic.Message);
        Assert.Equal(new byte[] { 255, 255, 255, 255 }, result.Surface.Pixels.ToArray());
    }

    [Fact]
    public void StrictRendererPreservesInvalidDataFailure()
    {
        var image = SkiaJpegTestData.Display(SkiaJpegTestData.Create());
        using var renderer = new SkiaPdfPageRenderer();

        InvalidDataException exception = Assert.Throws<InvalidDataException>(() =>
            renderer.Render(new PdfDisplayList([image]), Request(5, PdfRenderMode.Strict)));

        Assert.Equal("Decoded image exceeds the configured pixel limit.", exception.Message);
    }

    [Fact]
    public void ValidJpegAtExactPixelLimitRendersWithoutDiagnostic()
    {
        var image = SkiaJpegTestData.Display(SkiaJpegTestData.Create());
        using var renderer = new SkiaPdfPageRenderer();

        PdfRenderResult result = renderer.Render(new PdfDisplayList([image]), Request(6, PdfRenderMode.Strict));

        Assert.Empty(result.Diagnostics);
        Assert.Equal(new byte[] { 255, 255, 255, 255 }, result.Surface.Pixels.ToArray());
    }

    private static PdfRenderRequest Request(long maximumPixels, PdfRenderMode mode) => new()
    {
        Region = new PdfRenderRegion(0, 0, 1, 1),
        PixelWidth = 1,
        PixelHeight = 1,
        Mode = mode,
        Limits = new PdfRenderLimits { MaximumSurfacePixels = maximumPixels }
    };
}
