using BlackFlag.Engine.Document.Objects;
using BlackFlag.Engine.Editing.Documents;
using BlackFlag.Engine.Rendering.DisplayList;
using BlackFlag.Engine.Rendering.Raster;
using BlackFlag.Engine.Services.Editing;
using BlackFlag.Engine.Services.Text;
using BlackFlag.Engine.Writing;
using G = BlackFlag.Engine.Objects;

namespace BlackFlag.Engine.Services.Tests.Conformance;

public sealed class PdfTextReopenConformanceTests
{
    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void RotatedReplacementKeepsBaselineAndUprightPaintingOrClipping(bool streamXref, bool clip)
    {
        string text = "q BT /F1 20 Tf " + (clip ? "7 Tr " : "")
            + "0 1 -1 0 100 40 Tm (A) Tj ET " + (clip ? "0 0 200 200 re f " : "") + "Q\n";
        using var opened = new ReopenedPdf(ConformanceFixture.Create(streamXref, text), streamXref);
        var originalInk = Ink(opened.Render());
        Assert.InRange(originalInk.Left, 85, 87);
        Assert.InRange(originalInk.Right, 98, 100);
        Assert.InRange(originalInk.Bottom, 39, 41);
        Assert.InRange(originalInk.Top, 48, 50);
        var document = opened.ToEditableFixture();
        using var session = new PdfEditingSession(document);
        var source = opened.TextSource();
        var descriptor = Assert.Single(PdfExistingTextDiscoveryService.Discover(session, session.Document.Pages[0], source));
        using var transaction = session.BeginTransaction();
        PdfExistingTextReplacementService.Replace(session, session.Document.Pages[0], source, descriptor, "B");
        using var reopened = new ReopenedPdf(PdfEditingSaveService.Write(session, transaction.Commit(),
            ConformanceFixture.Options(streamXref)), streamXref);
        var run = Assert.Single(reopened.TextSource().DisplayList.OfType<PdfDisplayTextRun>());
        Assert.Equal("B", run.UnicodeText);
        Assert.Equal(new PdfMatrix(0, 1, -1, 0, 100, 40), run.TextMatrix);
        var changedInk = Ink(reopened.Render());
        Assert.InRange((double)changedInk.Count / originalInk.Count, 0.35, 0.65);
        Assert.Equal(originalInk.Left, changedInk.Left);
        Assert.Equal(originalInk.Bottom, changedInk.Bottom);
        Assert.Equal(originalInk.Right, changedInk.Right);
        Assert.InRange(changedInk.Top, 43, 45);
        AssertPreserved(opened, reopened);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void ReplacementReopensRendersAndPreservesUntouchedGraph(bool streamXref, bool incremental)
    {
        byte[] original = ConformanceFixture.Create(streamXref);
        using var opened = new ReopenedPdf(original, streamXref);
        var source = opened.TextSource();
        var originalRun = Assert.Single(source.DisplayList.OfType<PdfDisplayTextRun>());
        Assert.Equal("A", originalRun.UnicodeText);
        Assert.Equal(new PdfMatrix(1, 0.25, 0, 1, 40, 67.5), originalRun.TextMatrix);
        Assert.Equal(new PdfMatrix(1, 0.25, 0, 1, 10, 20), originalRun.GraphicsState.Transform);
        var originalPixels = Ink(opened.Render());
        Assert.InRange(originalPixels.Count, 110, 190);
        Assert.InRange(originalPixels.Left, 39, 41);
        Assert.InRange(originalPixels.Right, 48, 51);
        Assert.InRange(originalPixels.Bottom, 66, 69);
        Assert.InRange(originalPixels.Top, 82, 86);

        var document = opened.ToEditableFixture();
        var before = ConformanceFixture.Snapshot(document);
        using var session = new PdfEditingSession(document);
        var descriptor = Assert.Single(PdfExistingTextDiscoveryService.Discover(session, session.Document.Pages[0], source));
        using var transaction = session.BeginTransaction();
        PdfExistingTextReplacementService.Replace(session, session.Document.Pages[0], source, descriptor, "B");
        var changes = transaction.Commit();
        byte[] saved = PdfEditingSaveService.Write(session, changes, ConformanceFixture.Options(streamXref, incremental),
            new PdfMemorySourceRevision(original, opened.XrefOffset, ConformanceFixture.Options(streamXref).CrossReferenceMode));
        if (incremental) Assert.Equal(original, saved.AsSpan(0, original.Length).ToArray());

        using var reopened = new ReopenedPdf(saved, streamXref);
        Assert.Equal(incremental ? 2 : 1, reopened.RevisionCount);
        var run = Assert.Single(reopened.TextSource().DisplayList.OfType<PdfDisplayTextRun>());
        Assert.Equal("B", run.UnicodeText);
        Assert.Equal(originalRun.TextMatrix, run.TextMatrix);
        Assert.Equal(originalRun.GraphicsState.Transform, run.GraphicsState.Transform);
        AssertPreserved(opened, reopened);
        if (incremental) AssertIncrementalLocations(opened, reopened, original.Length);
        var changedPixels = Ink(reopened.Render());
        Assert.InRange((double)changedPixels.Count / originalPixels.Count, 0.35, 0.65);
        Assert.Equal(originalPixels.Left, changedPixels.Left);
        Assert.InRange(changedPixels.Bottom, originalPixels.Bottom - 1, originalPixels.Bottom + 1);
        Assert.Equal("B", Assert.Single(reopened.TextSource(1).DisplayList.OfType<PdfDisplayTextRun>()).UnicodeText);
        Assert.Equal(Ink(opened.Render(1)), Ink(reopened.Render(1)));

        session.History.Undo();
        ConformanceFixture.AssertSnapshot(document, before);
        using var undone = new ReopenedPdf(PdfFullRewriteWriter.Write(
            PdfWritePlanner.Create(document, ConformanceFixture.Options(streamXref))), streamXref);
        Assert.Equal("A", Assert.Single(undone.TextSource().DisplayList.OfType<PdfDisplayTextRun>()).UnicodeText);
        session.History.Redo();
        using var redone = new ReopenedPdf(PdfFullRewriteWriter.Write(
            PdfWritePlanner.Create(document, ConformanceFixture.Options(streamXref))), streamXref);
        Assert.Equal("B", Assert.Single(redone.TextSource().DisplayList.OfType<PdfDisplayTextRun>()).UnicodeText);
        Assert.Equal(changedPixels, Ink(redone.Render()));
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void TransformedDecorationReopensAsSemanticPathAndRedInk(bool streamXref, bool incremental)
    {
        byte[] original = ConformanceFixture.Create(streamXref);
        using var opened = new ReopenedPdf(original, streamXref);
        var document = opened.ToEditableFixture();
        using var session = new PdfEditingSession(document);
        var source = opened.TextSource();
        var descriptor = Assert.Single(PdfExistingTextDiscoveryService.Discover(session, session.Document.Pages[0], source));
        using var transaction = session.BeginTransaction();
        PdfExistingTextStyleService.Apply(session, session.Document.Pages[0], source, descriptor,
            new PdfExistingTextStyleUpdate
            {
                FillColor = PdfExistingTextColor.Rgb(1, 0, 0),
                Decoration = PdfExistingTextDecoration.Automatic(PdfExistingTextDecorationKind.Underline, -2, 1)
            });
        byte[] saved = PdfEditingSaveService.Write(session, transaction.Commit(), ConformanceFixture.Options(streamXref, incremental),
            new PdfMemorySourceRevision(original, opened.XrefOffset, ConformanceFixture.Options(streamXref).CrossReferenceMode));
        if (incremental) Assert.Equal(original, saved.AsSpan(0, original.Length).ToArray());
        using var reopened = new ReopenedPdf(saved, streamXref);
        var display = reopened.TextSource().DisplayList;
        var textRuns = display.OfType<PdfDisplayTextRun>().ToArray();
        Assert.Equal(2, textRuns.Length);
        Assert.Equal("A", textRuns[0].UnicodeText);
        Assert.Empty(textRuns[1].EncodedBytes.ToArray()); // Explicit text-position continuation.
        var decoration = Assert.Single(display.OfType<PdfDisplayPath>());
        Assert.True(decoration.Stroke);
        Assert.False(decoration.Fill);
        Assert.Equal(12, decoration.Path.GetBounds()!.Value.Width, 8); // 600 units at 20 pt.
        Assert.Equal(1, decoration.GraphicsState.LineWidth);
        Assert.Equal(new PdfMatrix(1, 0.25, 0, 1, 40, 67.5), decoration.GraphicsState.Transform);
        AssertPreserved(opened, reopened);
        if (incremental) AssertIncrementalLocations(opened, reopened, original.Length);
        var raster = reopened.Render();
        var ink = Ink(raster);
        Assert.InRange(ink.Count, 120, 220);
        Assert.True(CountRed(raster) >= ink.Count * 0.95);
        Assert.InRange(ink.Left, 39, 41);
        Assert.InRange(ink.Right, 50, 53);
        Assert.InRange(ink.Bottom, 64, 66);
    }

    internal static void AssertPreserved(ReopenedPdf before, ReopenedPdf after)
    {
        Assert.Equal(before.Dictionary(1), after.Dictionary(1));
        Assert.Equal(before.Dictionary(6, 3), after.Dictionary(6, 3));
        Assert.Equal(before.Dictionary(5), after.Dictionary(5));
        Assert.Equal(before.Dictionary(10, 7), after.Dictionary(10, 7));
        Assert.Equal(before.Stream(7, 1).Data.ToArray(), after.Stream(7, 1).Data.ToArray());
        Assert.Equal(before.Stream(9, 4).Data.ToArray(), after.Stream(9, 4).Data.ToArray());
        foreach (var entry in before.Pages[0].Dictionary)
            if (entry.Key.Value != "Resources") Assert.Equal(entry.Value, after.Pages[0].Dictionary[entry.Key]);
        Assert.Equal(83, Assert.IsType<G.PdfInteger>(after.Pages[0].Resources.Value[new("PrivateResource")]).Value);
        Assert.Equal(17, Assert.IsType<G.PdfInteger>(after.Dictionary(3)[new("PrivatePageEntry")]).Value);
        var content = Assert.IsType<G.PdfIndirectReference>(after.Dictionary(3)[new("Contents")]);
        Assert.Equal(4, content.ObjectNumber);
        Assert.Equal(2, content.GenerationNumber);
        foreach (var entry in before.Stream(4, 2).Dictionary)
            if (entry.Key.Value != "Length") Assert.Equal(entry.Value, after.Stream(4, 2).Dictionary[entry.Key]);
    }

    internal static void AssertIncrementalLocations(ReopenedPdf before, ReopenedPdf after, int originalLength)
    {
        foreach (int number in new[] { 1, 2, 5, 6, 7, 8, 9, 10 })
            Assert.Equal(before.Locations.Locations[number], after.Locations.Locations[number]);
        Assert.True(after.Locations.Locations[4].Offset >= originalLength);
        Assert.Equal(2, after.Locations.Locations[4].ObjectId.GenerationNumber);
        Assert.Equal(before.XrefOffset, Assert.IsType<G.PdfInteger>(after.Trailer[new("Prev")]).Value);
    }

    // Compare geometry/areas, not platform-specific antialiasing bytes. Ink threshold
    // excludes almost-white fringes; position tolerance is <= 2 pixels at 1 px/pt.
    internal static (int Count, int Left, int Bottom, int Right, int Top) Ink(PdfRenderResult result)
    {
        var surface = result.Surface;
        var pixels = surface.Pixels.Span;
        int count = 0, left = 200, right = 0, bottom = 200, top = 0;
        for (int y = 0; y < 200; y++)
        for (int x = 0; x < 200; x++)
        {
            int offset = y * surface.Stride + x * 4;
            if (pixels[offset] > 240 && pixels[offset + 1] > 240 && pixels[offset + 2] > 240) continue;
            count++;
            left = Math.Min(left, x); right = Math.Max(right, x);
            bottom = Math.Min(bottom, 199 - y); top = Math.Max(top, 199 - y);
        }
        Assert.True(count > 0, "Expected embedded outlines to render visible ink.");
        return (count, left, bottom, right, top);
    }

    private static int CountRed(PdfRenderResult result)
    {
        var pixels = result.Surface.Pixels.Span;
        int red = 0;
        for (int i = 0; i < pixels.Length; i += 4)
            if (pixels[i] >= 240 && pixels[i + 1] < 240 && pixels[i + 2] < 240) red++;
        return red;
    }
}
