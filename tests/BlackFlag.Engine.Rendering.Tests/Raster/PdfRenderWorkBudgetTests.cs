using BlackFlag.Engine.Rendering.Raster;

namespace BlackFlag.Engine.Rendering.Tests.Raster;

public sealed class PdfRenderWorkBudgetTests
{
    [Fact]
    public void SharedBudgetRejectsWorkBeyondLimit()
    {
        var budget = new PdfRenderWorkBudget(10);

        budget.Consume(6);
        budget.Consume(4);

        Assert.Equal(10, budget.ConsumedWorkUnits);
        Assert.Throws<InvalidOperationException>(() => budget.Consume(1));
        Assert.Equal(10, budget.ConsumedWorkUnits);
    }

    [Fact]
    public void FinalRenderLimitsValidate()
    {
        PdfRenderLimits limits = new();

        limits.Validate();

        Assert.True(limits.MaximumRenderWorkUnits > 0);
        Assert.True(limits.MaximumPatternNesting > 0);
        Assert.True(limits.MaximumMeshVertices > 0);
        Assert.True(limits.MaximumTransparencyGroupPixels > 0);
    }
}
