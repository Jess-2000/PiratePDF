using System.IO;
using BlackFlag.Engine.Editing.Documents;
using BlackFlag.Engine.Fonts;
using BlackFlag.Engine.Fonts.Sanitization;
using BlackFlag.Engine.Parsing.Resources;
using BlackFlag.Engine.Rendering.DisplayList;
using BlackFlag.Engine.SDK.Adapters.Editing;
using BlackFlag.Engine.SDK.Editing;
using BlackFlag.Engine.Services.Editing;
using BlackFlag.Engine.Services.Text;
using BlackFlag.Engine.Writing;
using G = BlackFlag.Engine.Objects;

namespace BlackFlag.Engine.Services.Tests.Conformance;

public sealed class PdfTransactionalReopenConformanceTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void UnsupportedShapingFailsWithoutInstallingResourcesAndValidSaveRemainsPossible(bool streamXref)
    {
        using var opened = new ReopenedPdf(ConformanceFixture.Create(streamXref, shaping: true), streamXref);
        var document = opened.ToEditableFixture();
        var snapshot = ConformanceFixture.Snapshot(document);
        using var session = new PdfEditingSession(document);
        var source = opened.TextSource();
        var descriptor = Assert.Single(PdfExistingTextDiscoveryService.Discover(session, session.Document.Pages[0], source));
        Assert.Equal("a\u0301", descriptor.UnicodeText);
        var match = Assert.Single(new PdfExternalFontCatalog([new ConformanceFixture.FontProvider()])
            .Match(new PdfExternalFontMatchRequest("B10 Rectangles"), "A"));
        using var transaction = session.BeginTransaction();
        Assert.Throws<NotSupportedException>(() => PdfExternalExistingTextFontService.ChangeFont(
            session, session.Document.Pages[0], source, descriptor, match, new PdfOpenTypeFontSanitizer()));
        Assert.Equal(0, transaction.ChangeCount);
        ConformanceFixture.AssertSnapshot(document, snapshot);
        transaction.Rollback();
        using var unchanged = new ReopenedPdf(PdfFullRewriteWriter.Write(
            PdfWritePlanner.Create(document, ConformanceFixture.Options(streamXref))), streamXref);
        Assert.Equal("a\u0301", Assert.Single(unchanged.TextSource().DisplayList.OfType<PdfDisplayTextRun>()).UnicodeText);
        Assert.Equal(opened.Stream(4, 2).Data.ToArray(), unchanged.Stream(4, 2).Data.ToArray());
        Assert.Equal(opened.Stream(13).Data.ToArray(), unchanged.Stream(13).Data.ToArray());
        // This deliberately unsupported multi-scalar mapping is a rejection/
        // preservation fixture, not a visual shaping conformance claim.
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void DestinationFailurePreservesCommittedGraphAllowsRetryAndUndo(bool streamXref, bool incremental)
    {
        byte[] original = ConformanceFixture.Create(streamXref);
        using var opened = new ReopenedPdf(original, streamXref);
        var document = opened.ToEditableFixture();
        var before = ConformanceFixture.Snapshot(document);
        using var session = new PdfEditingSession(document);
        var source = opened.TextSource();
        var descriptor = Assert.Single(PdfExistingTextDiscoveryService.Discover(session, session.Document.Pages[0], source));
        using var transaction = session.BeginTransaction();
        Assert.Throws<NotSupportedException>(() => PdfExistingTextReplacementService.Replace(
            session, session.Document.Pages[0], source, descriptor, "\u4E00"));
        Assert.Equal(0, transaction.ChangeCount);
        ConformanceFixture.AssertSnapshot(document, before);
        PdfExistingTextReplacementService.Replace(session, session.Document.Pages[0], source, descriptor, "B");
        var changes = transaction.Commit();
        var committed = ConformanceFixture.Snapshot(document);
        var revision = new PdfMemorySourceRevision(original, opened.XrefOffset, ConformanceFixture.Options(streamXref).CrossReferenceMode);
        using var destination = new FailingDestination();
        Assert.Throws<IOException>(() => PdfEditingSaveService.Write(destination, session, changes,
            ConformanceFixture.Options(streamXref, incremental), revision));
        Assert.True(destination.CanWrite); // Caller ownership survives failure.
        Assert.Equal(64, destination.Length); // Stream output is not an atomic file replacement.
        ConformanceFixture.AssertSnapshot(document, committed);
        byte[] retry = PdfEditingSaveService.Write(session, changes, ConformanceFixture.Options(streamXref, incremental), revision);
        using var reopened = new ReopenedPdf(retry, streamXref);
        Assert.Equal("B", Assert.Single(reopened.TextSource().DisplayList.OfType<PdfDisplayTextRun>()).UnicodeText);
        PdfTextReopenConformanceTests.AssertPreserved(opened, reopened);
        _ = reopened.Render();
        session.History.Undo();
        ConformanceFixture.AssertSnapshot(document, before);
        using var undone = new ReopenedPdf(PdfFullRewriteWriter.Write(
            PdfWritePlanner.Create(document, ConformanceFixture.Options(streamXref))), streamXref);
        Assert.Equal("A", Assert.Single(undone.TextSource().DisplayList.OfType<PdfDisplayTextRun>()).UnicodeText);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void SdkPublicInterfaceReopensEditsAndRejectsStaleTargetsAcrossRollbackUndoRedo(bool streamXref)
    {
        using var opened = new ReopenedPdf(ConformanceFixture.Create(streamXref), streamXref);
        var document = opened.ToEditableFixture();
        var before = ConformanceFixture.Snapshot(document);
        using var sdk = new PdfEditingSdkSession(document);
        IPdfExistingTextSdkSession text = sdk;
        Guid pageId = sdk.Pages[0].Id;
        sdk.RegisterExistingTextSource(pageId, opened.TextSource());
        var target = Assert.Single(text.DiscoverExistingText(pageId));
        sdk.BeginTransaction();
        Assert.Throws<KeyNotFoundException>(() => text.ChangeExistingTextFont(target.Id, "Unregistered"));
        ConformanceFixture.AssertSnapshot(document, before);
        Assert.Equal(target.Id, Assert.Single(text.DiscoverExistingText(pageId)).Id);
        text.ReplaceExistingText(target.Id, "B");
        Assert.Empty(text.DiscoverExistingText(pageId));
        Assert.Throws<InvalidOperationException>(() => text.ReplaceExistingText(target.Id, "A"));
        sdk.RollbackTransaction();
        ConformanceFixture.AssertSnapshot(document, before);
        Assert.Throws<InvalidOperationException>(() => text.ReplaceExistingText(target.Id, "A"));

        sdk.RegisterExistingTextSource(pageId, opened.TextSource());
        var fresh = Assert.Single(text.DiscoverExistingText(pageId));
        Assert.NotEqual(target.Id, fresh.Id);
        sdk.BeginTransaction();
        text.ReplaceExistingText(fresh.Id, "B");
        sdk.CommitTransaction();
        using var saved = ReopenModel();
        Assert.Equal("B", Assert.Single(saved.TextSource().DisplayList.OfType<PdfDisplayTextRun>()).UnicodeText);
        PdfTextReopenConformanceTests.AssertPreserved(opened, saved);
        _ = saved.Render();
        sdk.RegisterExistingTextSource(pageId, saved.TextSource());
        var afterEdit = Assert.Single(text.DiscoverExistingText(pageId));
        sdk.Undo();
        ConformanceFixture.AssertSnapshot(document, before);
        Assert.Empty(text.DiscoverExistingText(pageId));
        Assert.Throws<InvalidOperationException>(() => text.ReplaceExistingText(afterEdit.Id, "A"));
        using var undone = ReopenModel();
        Assert.Equal("A", Assert.Single(undone.TextSource().DisplayList.OfType<PdfDisplayTextRun>()).UnicodeText);
        sdk.Redo();
        Assert.Empty(text.DiscoverExistingText(pageId));
        using var redone = ReopenModel();
        Assert.Equal("B", Assert.Single(redone.TextSource().DisplayList.OfType<PdfDisplayTextRun>()).UnicodeText);
        Assert.Equal(PdfTextReopenConformanceTests.Ink(saved.Render()), PdfTextReopenConformanceTests.Ink(redone.Render()));

        ReopenedPdf ReopenModel() => new(PdfFullRewriteWriter.Write(
            PdfWritePlanner.Create(document, ConformanceFixture.Options(streamXref))), streamXref);
    }

    [Fact]
    public void DescendantCeilingIsCheckedBeforeResolvingAnyDescendant()
    {
        using var opened = new ReopenedPdf(ConformanceFixture.Create(false), false);
        var font = new G.PdfDictionary(new Dictionary<G.PdfName, G.PdfObject>
        {
            [new("Subtype")] = new G.PdfName("Type0"),
            [new("DescendantFonts")] = new G.PdfArray([new G.PdfIndirectReference(999, 0), new G.PdfIndirectReference(999, 0)])
        });
        var resources = new G.PdfDictionary(new Dictionary<G.PdfName, G.PdfObject>
        {
            [new("Font")] = new G.PdfDictionary(new Dictionary<G.PdfName, G.PdfObject> { [new("F")] = font })
        });
        var resolver = new PdfResourceResolver(opened.Objects, new() { MaximumFontDescendantCount = 1 });
        var exception = Assert.Throws<PdfResourceResolutionException>(() => resolver.CreateScope(resources).ResolveFont(new("F")));
        Assert.Equal(PdfResourceResolutionFailure.LimitExceeded, exception.Failure);
    }

    private sealed class FailingDestination : MemoryStream
    {
        private int remaining = 64;
        public override void Write(ReadOnlySpan<byte> buffer)
        {
            int accepted = Math.Min(remaining, buffer.Length);
            // Call the byte-array base implementation directly; MemoryStream's
            // span fallback on a derived type dispatches back to the override.
            base.Write(buffer[..accepted].ToArray(), 0, accepted);
            remaining -= accepted;
            if (accepted != buffer.Length) throw new IOException("Injected destination failure.");
        }
        public override void Write(byte[] buffer, int offset, int count) => Write(buffer.AsSpan(offset, count));
        public override void WriteByte(byte value) => Write([value]);
    }
}
