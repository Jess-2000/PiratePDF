using BlackFlag.Engine.Rendering.Raster;

namespace BlackFlag.Engine.Rendering.Tests.Raster;

public sealed class PdfRenderRequestTests
{
    [Fact]
    public void ValidateAcceptsBoundedRegion()
    {
        PdfRenderRequest request = new()
        {
            Region = new PdfRenderRegion(0, 0, 612, 792),
            PixelWidth = 612,
            PixelHeight = 792
        };

        request.Validate();
    }

    [Fact]
    public void ValidateRejectsOversizedSurface()
    {
        PdfRenderRequest request = new()
        {
            Region = new PdfRenderRegion(0, 0, 100, 100),
            PixelWidth = 100,
            PixelHeight = 100,
            Limits = new PdfRenderLimits
            {
                MaximumSurfacePixels = 9_999
            }
        };

        Assert.Throws<InvalidOperationException>(request.Validate);
    }
}
