using BlackFlag.Engine.Objects;
using BlackFlag.Engine.Rendering.DisplayList;
using BlackFlag.Engine.Rendering.Graphics;
using BlackFlag.Engine.Rendering.Resources;

namespace BlackFlag.Engine.Rendering.Tests.DisplayList;

public sealed class PdfClipStateTests
{
    [Fact]
    public void TextClipPreservesRunsAndParent()
    {
        PdfResolvedFont font = new(
            PdfResourceIdentity.ForLocal(99),
            PdfDictionary.Empty,
            "TrueType",
            "Test",
            "Test",
            400,
            5,
            PdfFontSlant.Upright,
            PdfFontTechnology.TrueType,
            PdfFontEmbeddingRights.Unknown,
            false,
            null,
            null,
            null,
            null,
            null,
            []);

        var run = new PdfDisplayTextRun(
            font,
            12,
            [PdfTextRunElement.CreateText([0x41])],
            PdfMatrix.Identity,
            new PdfGraphicsStateSnapshot(),
            new PdfContentSourceSpan(0, 0, 1),
            null);

        PdfClipState parent = new(
            new PdfPathGeometry(
            [
                new PdfPathSegment(PdfPathSegmentKind.MoveTo, new PdfPoint(0, 0)),
                new PdfPathSegment(PdfPathSegmentKind.LineTo, new PdfPoint(10, 0))
            ]),
            PdfFillRule.Nonzero,
            null);

        PdfClipState clip = new([run], parent);

        Assert.Equal(PdfClipKind.Text, clip.Kind);
        Assert.Same(parent, clip.Parent);
        Assert.Same(run, Assert.Single(clip.TextRuns));
        Assert.Equal(2, clip.Depth);
    }
    [Fact]
    public void DisplayListCollectionsRejectMutationThroughConcreteListInterfaces()
    {
        PdfResolvedFont font = new(
            PdfResourceIdentity.ForLocal(100),
            PdfDictionary.Empty,
            "TrueType",
            "Test",
            "Test",
            400,
            5,
            PdfFontSlant.Upright,
            PdfFontTechnology.TrueType,
            PdfFontEmbeddingRights.Unknown,
            false,
            null,
            null,
            null,
            null,
            null,
            []);

        var element = PdfTextRunElement.CreateText([0x41], "A");
        var run = new PdfDisplayTextRun(
            font,
            12,
            [element],
            PdfMatrix.Identity,
            new PdfGraphicsStateSnapshot(),
            new PdfContentSourceSpan(0, 0, 1),
            null);
        var path = new PdfPathGeometry(
            [new PdfPathSegment(PdfPathSegmentKind.MoveTo, new PdfPoint(0, 0))]);
        var clip = new PdfClipState([run], null);
        var color = new PdfColorValue("DeviceRGB", [0.25, 0.5, 0.75]);
        var dash = new PdfDashPattern([1, 2], 0);

        Assert.Throws<NotSupportedException>(() => ((IList<PdfTextRunElement>)run.Elements)[0] = element);
        Assert.Throws<NotSupportedException>(() => ((IList<PdfPathSegment>)path.Segments)[0] = path.Segments[0]);
        Assert.Throws<NotSupportedException>(() => ((IList<PdfDisplayTextRun>)clip.TextRuns)[0] = run);
        Assert.Throws<NotSupportedException>(() => ((IList<double>)color.Components)[0] = 1);
        Assert.Throws<NotSupportedException>(() => ((IList<double>)dash.Elements)[0] = 3);
    }

}
