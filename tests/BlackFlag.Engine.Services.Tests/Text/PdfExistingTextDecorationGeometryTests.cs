using BlackFlag.Engine.Rendering.DisplayList;
using Xunit.Abstractions;

namespace BlackFlag.Engine.Services.Tests.Text;

public sealed partial class PdfExistingTextDecorationGeometryTests(ITestOutputHelper output)
{
    [Fact]
    public void TranslatedDecorationUsesTheTargetTextMatrix()
    {
        using var context = new PdfDecorationTestContext();
        context.Add("BT").Add("Tf", "/F1", 12).Add("Tm", 1, 0, 0, 1, 100, 200)
            .Add("Tj", "AB").Add("ET").Finish();
        context.Apply(PdfDecorationTestContext.Underline());
        output.WriteLine(context.Current);
        Assert.Contains("1 0 0 1 100 200 cm", context.Current, StringComparison.Ordinal);
        var path = Assert.Single(PdfDecorationSyntaxAssertions.ReadPaths(context.Current));
        PdfDecorationSyntaxAssertions.Point(new PdfPoint(100, 198), path.Path.Segments[0].EndPoint);
        PdfDecorationSyntaxAssertions.Point(new PdfPoint(112, 198), path.Path.Segments[1].EndPoint);
    }

    [Fact]
    public void DecorationClosesTextBeforePaintingAndRestoresLineAndAdvance()
    {
        using var context = new PdfDecorationTestContext();
        context.Add("BT").Add("Tf", "/F1", 12).Add("Tm", 1, 0, 0, 1, 100, 200)
            .Add("Tj", "AB").Add("Tj", "CD").Add("ET").Finish();
        context.Apply(PdfDecorationTestContext.Underline());
        output.WriteLine(context.Current);
        Assert.Contains("ET\nq\n", context.Current, StringComparison.Ordinal);
        Assert.Contains("Q\nBT\n", context.Current, StringComparison.Ordinal);
        _ = PdfDecorationSyntaxAssertions.ReadPaths(context.Current);
    }

    [Fact]
    public void InterpreterAdvancesRotatedTextInLocalCoordinates()
    {
        using var context = new PdfDecorationTestContext();
        context.Add("BT").Add("Tf", "/F1", 12).Add("Tm", 0, 1, -1, 0, 100, 200)
            .Add("Tj", "AB").Add("Tj", "CD").Add("ET").Finish();
        PdfDisplayTextRun following = context.DisplayList.OfType<PdfDisplayTextRun>().Last();
        Assert.Equal(new PdfPoint(100, 212), following.TextMatrix.Transform(new PdfPoint(0, 0)));
    }

    [Fact]
    public void InterpreterMovesRotatedTextLinesInLocalCoordinates()
    {
        using var context = new PdfDecorationTestContext();
        context.Add("BT").Add("Tf", "/F1", 12).Add("Tm", 0, 1, -1, 0, 100, 200)
            .Add("Td", 0, -20).Add("Tj", "AB").Add("ET").Finish();
        PdfDisplayTextRun run = Assert.IsType<PdfDisplayTextRun>(Assert.Single(context.DisplayList));
        Assert.Equal(new PdfPoint(120, 200), run.TextMatrix.Transform(new PdfPoint(0, 0)));
    }

    [Fact]
    public void InterpreterConcatenatesLocalTransformBeforeCurrentTransform()
    {
        using var context = new PdfDecorationTestContext();
        context.Add("cm", 2, 0, 0, 3, 10, 20).Add("cm", 1, 0, 0, 1, 100, 200)
            .Add("m", 0, -2).Add("l", 12, -2).Add("S").Finish();
        PdfDisplayPath path = Assert.IsType<PdfDisplayPath>(Assert.Single(context.DisplayList));
        Assert.Equal(new PdfPoint(210, 614), path.Path.Segments[0].EndPoint);
    }

    [Fact]
    public void GraphicsRestoreDoesNotRewindTextMatrices()
    {
        using var context = new PdfDecorationTestContext();
        context.Add("BT").Add("Tf", "/F1", 12).Add("Tm", 0, 1, -1, 0, 100, 200)
            .Add("q").Add("Tj", "AB").Add("Q").Add("Tj", "CD").Add("ET").Finish();
        var following = context.DisplayList.OfType<PdfDisplayTextRun>().Last();
        Assert.Equal(new PdfPoint(100, 212), following.TextMatrix.Transform(new PdfPoint(0, 0)));
    }

    [Fact]
    public void FiniteCoordinateAtSignedIntegerBoundarySerializesAsReal()
    {
        using var context = new PdfDecorationTestContext();
        context.Add("BT").Add("Tf", "/F1", 12)
            .Add("Tm", 1, 0, 0, 1, new BlackFlag.Engine.Objects.PdfReal(9223372036854775808d), 0)
            .Add("Tj", "AB").Add("ET").Finish();
        context.Apply(PdfDecorationTestContext.Underline());
        var path = Assert.Single(PdfDecorationSyntaxAssertions.ReadPaths(context.Current));
        Assert.Equal(9223372036854775808d, path.Path.Segments[0].EndPoint.X);
    }
}
