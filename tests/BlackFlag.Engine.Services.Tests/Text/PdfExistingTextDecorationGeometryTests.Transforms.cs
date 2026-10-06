using BlackFlag.Engine.Rendering.DisplayList;
using BlackFlag.Engine.Services.Text;
using G = BlackFlag.Engine.Objects;

namespace BlackFlag.Engine.Services.Tests.Text;

public sealed partial class PdfExistingTextDecorationGeometryTests
{
    public static IEnumerable<object[]> Matrices()
    {
        yield return [1d, 0d, 0d, 1d, 0d, 0d];
        yield return [1d, 0d, 0d, 1d, 100d, 200d];
        yield return [2d, 0d, 0d, 2d, 10d, 20d];
        yield return [2d, 0d, 0d, 3d, 10d, 20d];
        yield return [0d, 1d, -1d, 0d, 100d, 200d];
        yield return [0.6, 0.8, -0.8, 0.6, 100d, 200d];
        yield return [1d, 0.25, 0.5, 1d, 10d, 20d];
        yield return [1.2, 0.4, -0.7, 2.1, 100d, -20d];
        yield return [0d, 0d, 0d, 0d, 10d, 20d];
    }

    [Theory]
    [MemberData(nameof(Matrices))]
    public void AffineGeometryIsReinterpretedWithNestedCtm(double a, double b, double c, double d, double e, double f)
    {
        using var context = new PdfDecorationTestContext();
        context.Add("cm", 2, 0, 0, 3, 10, 20).Add("BT").Add("Tf", "/F1", 12)
            .Add("Tm", a, b, c, d, e, f).Add("Tj", "AB").Add("ET").Finish();
        context.Apply(PdfDecorationTestContext.Underline());
        var path = Assert.Single(PdfDecorationSyntaxAssertions.ReadPaths(context.Current));
        // Independent scalar endpoint oracle, including the outer CTM exactly once.
        PdfDecorationSyntaxAssertions.Point(new PdfPoint((e - 2 * c) * 2 + 10, (f - 2 * d) * 3 + 20),
            path.Path.Segments[0].EndPoint);
        PdfDecorationSyntaxAssertions.Point(new PdfPoint((12 * a - 2 * c + e) * 2 + 10, (12 * b - 2 * d + f) * 3 + 20),
            path.Path.Segments[1].EndPoint);
        Assert.Equal(1, path.GraphicsState.LineWidth);
    }

    [Theory]
    [InlineData(0, 100)]
    [InlineData(4, 50)]
    [InlineData(-3, 150)]
    [InlineData(0, -100)]
    public void AutomaticAdvanceIncludesSpacingAndScalingOnce(double rise, double scale)
    {
        using var context = new PdfDecorationTestContext();
        context.Add("BT").Add("Tf", "/F1", 12).Add("Tc", 1).Add("Tw", 2)
            .Add("Tz", scale).Add("Ts", rise).Add("Tj", "A B").Add("ET").Finish();
        context.Apply(new PdfExistingTextStyleUpdate
        {
            Decoration = PdfExistingTextDecoration.Automatic(PdfExistingTextDecorationKind.Underline, -2, 1)
        });
        var path = Assert.Single(PdfDecorationSyntaxAssertions.ReadPaths(context.Current));
        PdfDecorationSyntaxAssertions.Point(new PdfPoint(0, rise - 2), path.Path.Segments[0].EndPoint);
        PdfDecorationSyntaxAssertions.Point(new PdfPoint(23 * scale / 100, rise - 2), path.Path.Segments[1].EndPoint);
    }

    [Theory]
    [InlineData(PdfExistingTextScriptStyle.Superscript, 5.2)]
    [InlineData(PdfExistingTextScriptStyle.Subscript, -1.4)]
    public void ScriptDecorationUsesEffectiveSizeRiseWidthAndColor(PdfExistingTextScriptStyle script, double rise)
    {
        using var context = new PdfDecorationTestContext();
        context.Add("BT").Add("Tf", "/F1", 12).Add("Tc", 1).Add("Tw", 2)
            .Add("Tz", 80).Add("Ts", 1).Add("Tj", "A B").Add("ET").Finish();
        context.Apply(new PdfExistingTextStyleUpdate
        {
            ScriptStyle = script,
            FillColor = PdfExistingTextColor.Rgb(1, 0, 0),
            Decoration = PdfExistingTextDecoration.Automatic(PdfExistingTextDecorationKind.Strikeout)
        });
        var path = Assert.Single(PdfDecorationSyntaxAssertions.ReadPaths(context.Current));
        PdfDecorationSyntaxAssertions.Point(new PdfPoint(0, rise + 2.34), path.Path.Segments[0].EndPoint);
        PdfDecorationSyntaxAssertions.Point(new PdfPoint(13.36, rise + 2.34), path.Path.Segments[1].EndPoint);
        Assert.Equal("DeviceRGB", path.GraphicsState.StrokeColor.ColorSpaceName);
        Assert.Equal(new double[] { 1, 0, 0 }, path.GraphicsState.StrokeColor.Components);
        Assert.Equal(0.5, path.GraphicsState.LineWidth);
    }

    [Theory]
    [InlineData("Tj", 200, 12)]
    [InlineData("TJ", 200, 13.44)]
    [InlineData("'", 180, 12)]
    [InlineData("\"", 180, 16)]
    public void TextOperatorsRetainPlacementAndPositioningAdjustments(string code, double y, double width)
    {
        using var context = new PdfDecorationTestContext();
        context.Add("BT").Add("Tf", "/F1", 12).Add("TL", 20).Add("Tm", 1, 0, 0, 1, 100, 200);
        if (code == "TJ")
            context.Add(code, new G.PdfArray([new G.PdfLiteralString("A"), new G.PdfInteger(-120), new G.PdfLiteralString("B")]));
        else if (code == "\"") context.Add(code, 3, 2, "AB");
        else context.Add(code, "AB");
        context.Add("T*" ).Add("Tj", "Next").Add("ET").Finish();
        context.Apply(new PdfExistingTextStyleUpdate
        {
            Decoration = PdfExistingTextDecoration.Automatic(PdfExistingTextDecorationKind.Underline, -2, 1)
        });
        var path = Assert.Single(PdfDecorationSyntaxAssertions.ReadPaths(context.Current));
        PdfDecorationSyntaxAssertions.Point(new PdfPoint(100, y - 2), path.Path.Segments[0].EndPoint);
        PdfDecorationSyntaxAssertions.Point(new PdfPoint(100 + width, y - 2), path.Path.Segments[1].EndPoint);
        Assert.Contains($"1 0 0 1 100 {y} Tm", context.Current, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(PdfExistingTextDecorationKind.Underline, -1.44)]
    [InlineData(PdfExistingTextDecorationKind.Strikeout, 3.6)]
    public void DefaultOffsetsAndExplicitWidthRemainStable(PdfExistingTextDecorationKind kind, double offset)
    {
        using var context = new PdfDecorationTestContext();
        context.Add("BT").Add("Tf", "/F1", 12).Add("Tz", 50).Add("Tj", "AB").Add("ET").Finish();
        context.Apply(new PdfExistingTextStyleUpdate { Decoration = new PdfExistingTextDecoration(kind, 42) });
        var path = Assert.Single(PdfDecorationSyntaxAssertions.ReadPaths(context.Current));
        PdfDecorationSyntaxAssertions.Point(new PdfPoint(42, offset), path.Path.Segments[1].EndPoint);
        Assert.Equal(0.6, path.GraphicsState.LineWidth, 6);
    }

    [Fact]
    public void WhitespaceIsMeasurableAndAutomaticZeroAdvanceRemainsUnsupported()
    {
        using var spaces = new PdfDecorationTestContext();
        spaces.Add("BT").Add("Tf", "/F1", 12).Add("Tj", "  ").Add("ET").Finish();
        spaces.Apply(new PdfExistingTextStyleUpdate
        {
            Decoration = PdfExistingTextDecoration.Automatic(PdfExistingTextDecorationKind.Underline)
        });
        var path = Assert.Single(PdfDecorationSyntaxAssertions.ReadPaths(spaces.Current));
        Assert.Equal(12, path.Path.Segments[1].EndPoint.X);
        using var zero = new PdfDecorationTestContext();
        zero.Add("BT").Add("Tf", "/F1", 0).Add("Tj", "AB").Add("ET").Finish();
        Assert.Throws<NotSupportedException>(() => zero.Apply(new PdfExistingTextStyleUpdate
        {
            Decoration = PdfExistingTextDecoration.Automatic(PdfExistingTextDecorationKind.Underline)
        }));
        Assert.Equal(zero.Original, zero.Current);
        zero.Apply(PdfDecorationTestContext.Underline());
        _ = PdfDecorationSyntaxAssertions.ReadPaths(zero.Current);
    }
}
