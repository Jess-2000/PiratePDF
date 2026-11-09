using BlackFlag.Engine.Document.Objects;
using BlackFlag.Engine.Editing.Documents;
using BlackFlag.Engine.Fonts;
using BlackFlag.Engine.Fonts.Sanitization;
using BlackFlag.Engine.Parsing.Resources;
using BlackFlag.Engine.Rendering.DisplayList;
using BlackFlag.Engine.Rendering.Fonts;
using BlackFlag.Engine.Services.Editing;
using BlackFlag.Engine.Services.Text;
using BlackFlag.Engine.Writing;
using G = BlackFlag.Engine.Objects;

namespace BlackFlag.Engine.Services.Tests.Conformance;

public sealed class PdfFontAndFormReopenConformanceTests
{
    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void ExternalChangeFontReopensEmbeddedGraphAndPreservesAppearance(bool streamXref, bool incremental)
    {
        byte[] original = ConformanceFixture.Create(streamXref);
        using var opened = new ReopenedPdf(original, streamXref);
        var document = opened.ToEditableFixture();
        var before = ConformanceFixture.Snapshot(document);
        using var session = new PdfEditingSession(document);
        var source = opened.TextSource();
        var descriptor = Assert.Single(PdfExistingTextDiscoveryService.Discover(session, session.Document.Pages[0], source));
        var match = Assert.Single(new PdfExternalFontCatalog([new ConformanceFixture.FontProvider()])
            .Match(new PdfExternalFontMatchRequest("B10 Rectangles"), "A"));
        using var transaction = session.BeginTransaction();
        var operation = PdfExternalExistingTextFontService.ChangeFont(session, session.Document.Pages[0], source,
            descriptor, match, new PdfOpenTypeFontSanitizer());
        Assert.NotEqual("FExt1", operation.ResourceName);
        byte[] saved = PdfEditingSaveService.Write(session, transaction.Commit(), ConformanceFixture.Options(streamXref, incremental),
            new PdfMemorySourceRevision(original, opened.XrefOffset, ConformanceFixture.Options(streamXref).CrossReferenceMode));
        if (incremental) Assert.Equal(original, saved.AsSpan(0, original.Length).ToArray());
        using var reopened = new ReopenedPdf(saved, streamXref);
        PdfTextReopenConformanceTests.AssertPreserved(opened, reopened);
        if (incremental) PdfTextReopenConformanceTests.AssertIncrementalLocations(opened, reopened, original.Length);
        var fonts = Assert.IsType<G.PdfDictionary>(reopened.Pages[0].Resources.Value[new("Font")]);
        Assert.Equal(new G.PdfIndirectReference(10, 7), fonts[new("FExt1")]);
        Assert.Equal(new G.PdfIndirectReference(5, 0), fonts[new("KeepAlias")]);
        var type0 = reopened.Dictionary(Assert.IsType<G.PdfIndirectReference>(fonts[new(operation.ResourceName)]));
        Assert.Equal(new G.PdfName("Type0"), type0[new("Subtype")]);
        Assert.Equal(new G.PdfName("Identity-H"), type0[new("Encoding")]);
        var descendants = Assert.IsType<G.PdfArray>(type0[new("DescendantFonts")]);
        var cidFont = reopened.Dictionary(Assert.IsType<G.PdfIndirectReference>(Assert.Single(descendants)));
        Assert.Equal(new G.PdfName("CIDFontType2"), cidFont[new("Subtype")]);
        var fontDescriptor = reopened.Dictionary(Assert.IsType<G.PdfIndirectReference>(cidFont[new("FontDescriptor")]));
        var fontFile = Assert.IsType<G.PdfIndirectReference>(fontDescriptor[new("FontFile2")]);
        Assert.NotEmpty(reopened.Stream(fontFile.ObjectNumber, fontFile.GenerationNumber).Data.ToArray());
        var unicode = Assert.IsType<G.PdfIndirectReference>(type0[new("ToUnicode")]);
        Assert.NotEmpty(reopened.Stream(unicode.ObjectNumber, unicode.GenerationNumber).Data.ToArray());
        var run = Assert.Single(reopened.TextSource().DisplayList.OfType<PdfDisplayTextRun>());
        Assert.Equal("A", run.UnicodeText);
        Assert.Equal("Type0", run.Font.Subtype);
        Assert.NotNull(run.Font.EmbeddedFontProgram);
        Assert.NotNull(run.Font.Descriptor);
        var resolvedDescendant = Assert.IsType<G.PdfDictionary>(Assert.Single(run.Font.DescendantFonts));
        Assert.IsType<G.PdfStream>(resolvedDescendant[new("CIDToGIDMap")]);
        Assert.Equal(12, Assert.Single(new PdfFontDecoder().Decode(run).Glyphs).Advance, 8);
        var limited = new PdfResourceResolver(reopened.Objects, new() { MaximumEmbeddedFontBytes = 1 });
        var bound = Assert.Throws<PdfResourceResolutionException>(() => limited.CreateScope(reopened.Pages[0].Resources.Value)
            .ResolveFont(new(operation.ResourceName)));
        Assert.Equal(PdfResourceResolutionFailure.LimitExceeded, bound.Failure);
        Assert.Equal(Assert.Single(source.DisplayList.OfType<PdfDisplayTextRun>()).TextMatrix, run.TextMatrix);
        // Same owned outline before/after subsetting: compare area and bounds rather
        // than requiring platform-independent antialiasing byte identity.
        var originalInk = PdfTextReopenConformanceTests.Ink(opened.Render());
        var changedInk = PdfTextReopenConformanceTests.Ink(reopened.Render());
        Assert.InRange((double)changedInk.Count / originalInk.Count, 0.98, 1.02);
        Assert.Equal(originalInk.Left, changedInk.Left);
        Assert.Equal(originalInk.Bottom, changedInk.Bottom);
        Assert.Equal(originalInk.Right, changedInk.Right);
        Assert.Equal(originalInk.Top, changedInk.Top);
        Assert.Equal(PdfTextReopenConformanceTests.Ink(opened.Render(1)), PdfTextReopenConformanceTests.Ink(reopened.Render(1)));
        session.History.Undo();
        ConformanceFixture.AssertSnapshot(document, before);
        session.History.Redo();
        using var redone = new ReopenedPdf(PdfFullRewriteWriter.Write(
            PdfWritePlanner.Create(document, ConformanceFixture.Options(streamXref))), streamXref);
        Assert.Equal("Type0", Assert.Single(redone.TextSource().DisplayList.OfType<PdfDisplayTextRun>()).Font.Subtype);
        Assert.Equal(changedInk, PdfTextReopenConformanceTests.Ink(redone.Render()));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void PlacementCloneReopensTwoFormsAndChangesOnlySelectedPlacement(bool streamXref)
    {
        using var opened = new ReopenedPdf(ConformanceFixture.Create(streamXref, forms: true), streamXref);
        var document = opened.ToEditableFixture();
        var originalGraph = ConformanceFixture.Snapshot(document);
        var source = opened.TextSource();
        var forms = source.DisplayList.OfType<PdfDisplayForm>().ToArray();
        Assert.Equal(2, forms.Length);
        Assert.Equal(forms[0].Form.Identity, forms[1].Form.Identity);
        Assert.Equal(new PdfMatrix(1, 0, 0, 1, 20, 30), forms[0].Transform);
        Assert.Equal(new PdfMatrix(1, 0, 0, 1, 80, 100), forms[1].Transform);
        var placement = PdfFormPlacementDescriptor.Create(document, ConformanceFixture.PageId, source, forms[0]);
        var projection = PdfFormPlacementCloneProjector.Create(PdfWritePlanner.Create(document), placement,
            "BT /F1 20 Tf (B) Tj ET\n"u8);
        ConformanceFixture.AssertSnapshot(document, originalGraph); // Planning does not mutate the source.
        projection.Projections.ApplyTo(document.Objects);
        byte[] saved = PdfFullRewriteWriter.Write(PdfWritePlanner.Create(document, ConformanceFixture.Options(streamXref)));
        using var reopened = new ReopenedPdf(saved, streamXref);
        Assert.Equal(opened.Stream(12, 5).Data.ToArray(), reopened.Stream(12, 5).Data.ToArray());
        Assert.Equal(opened.Stream(12, 5).Dictionary, reopened.Stream(12, 5).Dictionary);
        Assert.Equal(opened.Dictionary(6, 3), reopened.Dictionary(6, 3));
        var placements = reopened.TextSource().DisplayList.OfType<PdfDisplayForm>().ToArray();
        Assert.Equal(2, placements.Length);
        Assert.NotEqual(placements[0].Form.Identity, placements[1].Form.Identity);
        Assert.Equal(forms[1].Form.Identity, placements[1].Form.Identity);
        Assert.Equal(forms[0].Transform, placements[0].Transform);
        Assert.Equal(forms[1].Transform, placements[1].Transform);
        Assert.Equal("B", Assert.Single(placements[0].Content.OfType<PdfDisplayTextRun>()).UnicodeText);
        Assert.Equal("A", Assert.Single(placements[1].Content.OfType<PdfDisplayTextRun>()).UnicodeText);
        var originalInk = PdfTextReopenConformanceTests.Ink(opened.Render());
        var clonedInk = PdfTextReopenConformanceTests.Ink(reopened.Render());
        Assert.InRange(originalInk.Count, 270, 300);
        Assert.InRange(clonedInk.Count, 200, 225);
        Assert.InRange(originalInk.Left, 19, 21);
        Assert.InRange(originalInk.Bottom, 29, 31);
        Assert.InRange(originalInk.Right, 88, 90);
        Assert.InRange(originalInk.Top, 112, 114);
        Assert.InRange((double)clonedInk.Count / originalInk.Count, 0.70, 0.80);
        Assert.Equal(originalInk.Left, clonedInk.Left);
        Assert.Equal(originalInk.Top, clonedInk.Top);
        Assert.Equal(originalInk.Right, clonedInk.Right);
        Assert.Equal(originalInk.Bottom, clonedInk.Bottom);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void TextClippingUsesTheSameUprightGlyphGeometryAsPainting(bool streamXref)
    {
        const string clip = "q\n1 0.25 0 1 10 20 cm\nBT /F1 20 Tf 7 Tr 1 0 0 1 30 40 Tm (A) Tj ET\n0 0 200 200 re f Q\n";
        using var painted = new ReopenedPdf(ConformanceFixture.Create(streamXref), streamXref);
        using var clipped = new ReopenedPdf(ConformanceFixture.Create(streamXref, clip), streamXref);
        var expected = PdfTextReopenConformanceTests.Ink(painted.Render());
        var actual = PdfTextReopenConformanceTests.Ink(clipped.Render());
        Assert.Equal(expected.Left, actual.Left);
        Assert.Equal(expected.Bottom, actual.Bottom);
        Assert.Equal(expected.Right, actual.Right);
        Assert.Equal(expected.Top, actual.Top);
        Assert.InRange((double)actual.Count / expected.Count, 0.95, 1.05);
    }
}
