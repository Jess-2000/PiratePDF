using BlackFlag.Engine.Rendering.DisplayList;
using BlackFlag.Engine.Rendering.Graphics;
using BlackFlag.Engine.Rendering.Raster;
namespace BlackFlag.Engine.Rendering.Skia.Tests;

public sealed class SkiaPdfPageRendererTests
{
    [Fact]
    public void RenderProducesExpectedSurfaceDimensions()
    {
        PdfPathGeometry geometry = new(
        [
            new(PdfPathSegmentKind.MoveTo, new PdfPoint(10, 10)),
            new(PdfPathSegmentKind.LineTo, new PdfPoint(90, 10)),
            new(PdfPathSegmentKind.LineTo, new PdfPoint(90, 90)),
            new(PdfPathSegmentKind.LineTo, new PdfPoint(10, 90)),
            new(PdfPathSegmentKind.ClosePath, new PdfPoint(10, 10))
        ]);

        PdfDisplayPath path = new(
            geometry,
            fill: true,
            stroke: false,
            PdfFillRule.Nonzero,
            new PdfGraphicsStateSnapshot
            {
                FillColor = new PdfColorValue("DeviceRGB", [1, 0, 0])
            },
            new PdfContentSourceSpan(0, 0, 1),
            markedContent: null);

        PdfDisplayList displayList = new([path]);

        PdfRenderRequest request = new()
        {
            Region = new PdfRenderRegion(0, 0, 100, 100),
            PixelWidth = 100,
            PixelHeight = 100
        };

        SkiaPdfPageRenderer renderer = new();
        PdfRenderResult result = renderer.Render(displayList, request);

        Assert.Equal(100, result.Surface.Width);
        Assert.Equal(100, result.Surface.Height);
        Assert.Empty(result.Diagnostics);
        Assert.Contains(result.Surface.Pixels.Span.ToArray(), value => value != byte.MaxValue);
    }
}
