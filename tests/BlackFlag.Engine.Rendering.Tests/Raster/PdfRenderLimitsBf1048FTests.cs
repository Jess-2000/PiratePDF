
using BlackFlag.Engine.Rendering.Raster;

namespace BlackFlag.Engine.Rendering.Tests.Raster;

public sealed class PdfRenderLimitsBf1048FTests
{
    [Fact]
    public void ProductionLimitsIncludeNestedGraphicsBounds()
    {
        PdfRenderLimits limits = new();

        limits.Validate();

        Assert.True(limits.MaximumType3GlyphDepth > 0);
        Assert.True(limits.MaximumPatternCells > 0);
        Assert.True(limits.MaximumShadingSamples > 0);
    }
}
