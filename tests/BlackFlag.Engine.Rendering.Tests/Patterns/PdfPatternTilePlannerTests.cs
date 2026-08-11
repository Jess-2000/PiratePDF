using System.IO;
using BlackFlag.Engine.Rendering.DisplayList;
using BlackFlag.Engine.Rendering.Nested;
using BlackFlag.Engine.Rendering.Patterns;

namespace BlackFlag.Engine.Rendering.Tests.Patterns;

public sealed class PdfPatternTilePlannerTests
{
    [Fact]
    public void PlansOnlyCellsIntersectingRequestedBounds()
    {
        var cell = new PdfTilingPatternCell(
            new PdfDisplayList([]),
            new PdfDisplayBounds(0, 0, 10, 10),
            10,
            10,
            PdfMatrix.Identity,
            1,
            1);

        PdfPatternTilePlan plan = PdfPatternTilePlanner.Plan(
            cell,
            PdfMatrix.Identity,
            new PdfDisplayBounds(0, 0, 25, 15),
            maximumCells: 100);

        Assert.True(plan.CellCount > 0);
        Assert.True(plan.CellCount <= 100);
        Assert.True(plan.MaximumXIndex >= plan.MinimumXIndex);
        Assert.True(plan.MaximumYIndex >= plan.MinimumYIndex);
    }

    [Fact]
    public void RejectsSingularPatternTransform()
    {
        var cell = new PdfTilingPatternCell(
            new PdfDisplayList([]),
            new PdfDisplayBounds(0, 0, 10, 10),
            10,
            10,
            PdfMatrix.Identity,
            1,
            1);

        Assert.Throws<InvalidDataException>(() =>
            PdfPatternTilePlanner.Plan(
                cell,
                new PdfMatrix(0, 0, 0, 0, 0, 0),
                new PdfDisplayBounds(0, 0, 100, 100),
                maximumCells: 100));
    }
}
