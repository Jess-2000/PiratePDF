using BlackFlag.Engine.Document.Metadata;
using BlackFlag.Engine.Document.Model;
using BlackFlag.Engine.Document.Objects;
using BlackFlag.Engine.Document.Pages;
using BlackFlag.Engine.Editing.Documents;
using BlackFlag.Engine.Editing.Text;
using BlackFlag.Engine.Services.Text;
using BlackFlag.Engine.Writing;

namespace BlackFlag.Engine.Services.Tests.Text;

public sealed partial class PdfFormPlacementCloneProjectorTests
{
    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void ExistingResourceChangePrimitiveRollsBackOrReplaysTheExactCloneGraph(bool filtered, bool commit)
    {
        var document = CreateDocument(out var pageId, out var contentId, out var formId);
        var page = new PdfPage(100, 100);
        page.BindObjectIdentifier(pageId);
        document.Pages.Add(page);
        if (filtered)
        {
            MakeFlate(document.Objects.Get<PdfStream>(contentId));
            MakeFlate(document.Objects.Get<PdfStream>(formId));
        }
        var placement = CreatePlacementDescriptor(document, pageId, contentId, formId, "X1", 0, 6);
        byte[] before = GraphBytes(document);
        using var session = new PdfEditingSession(document);
        byte[] after;
        using (var transaction = session.BeginTransaction())
        {
            var projection = PdfFormPlacementCloneProjector.Create(PdfWritePlanner.Create(document), placement, "Edited"u8);
            var contentProjection = Assert.Single(projection.Projections.Projections, item => item.Identifier == contentId);
            var replacements = projection.Projections.Projections.Where(item => item.Identifier != contentId)
                .ToDictionary(item => item.Identifier, item => item.Value);
            var identity = new PdfExistingTextIdentity(session.Id, session.Document.Pages[0].Id,
                PdfExistingTextSourceKind.PageContent, pageId, contentId, 0, 0, 6, 0, 0);
            // Exercise the established internal resource-change transaction
            // primitive. This is not a new public clone-editing service.
            _ = session.Document.ApplyExistingTextResourceChange(identity,
                Assert.IsType<PdfStream>(contentProjection.Value).ToByteArray(), replacements, projection.ResourceName);
            after = GraphBytes(document);
            Assert.Equal(6, document.Objects.Count);
            if (commit)
            {
                Assert.Single(transaction.Commit());
            }
        }
        if (commit)
        {
            _ = session.History.Undo();
            Assert.Equal(before, GraphBytes(document));
            _ = session.History.Redo();
            Assert.Equal(after, GraphBytes(document));
        }
        else
        {
            Assert.Equal(before, GraphBytes(document));
            Assert.False(session.History.CanUndo);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void RejectionPreservesCallerTransactionAndItsNormalRollbackOrUndoRedo(bool commit)
    {
        var document = CreateDocument(out var pageId, out var contentId, out var formId);
        var placement = CreatePlacementDescriptor(document, pageId, contentId, formId, "X1", 0, 6);
        document.Objects.Get<PdfStream>(contentId).ReplaceData("0 0 m\n/X1 Do"u8);
        byte[] before = GraphBytes(document);
        using var session = new PdfEditingSession(document);
        using (var transaction = session.BeginTransaction())
        {
            _ = session.Document.SetDocumentInformation(PdfDocumentInformationField.Title, "Prior edit");
            AssertRejectedUnchanged(document, placement);
            Assert.Equal("Prior edit", document.Information.GetValue(PdfDocumentInformationField.Title));
            Assert.False(session.History.CanUndo);
            if (commit)
            {
                var changes = transaction.Commit();
                Assert.Single(changes);
            }
        }
        if (commit)
        {
            _ = session.History.Undo();
            Assert.Null(document.Information.GetValue(PdfDocumentInformationField.Title));
            Assert.Equal(before, GraphBytes(document));
            _ = session.History.Redo();
            Assert.Equal("Prior edit", document.Information.GetValue(PdfDocumentInformationField.Title));
        }
        else
        {
            Assert.Null(document.Information.GetValue(PdfDocumentInformationField.Title));
            Assert.False(session.History.CanUndo);
        }
        Assert.Equal(before, GraphBytes(document));
    }

    [Fact]
    public void ProjectionPreparationIsPureAndDoesNotInventAnEditingHistoryEntry()
    {
        var document = CreateDocument(out var pageId, out var contentId, out var formId);
        var placement = CreatePlacementDescriptor(document, pageId, contentId, formId, "X1", 0, 6);
        byte[] before = GraphBytes(document);
        using var session = new PdfEditingSession(document);
        using (var transaction = session.BeginTransaction())
        {
            var projection = PdfFormPlacementCloneProjector.Create(PdfWritePlanner.Create(document), placement, "Edited"u8);
            Assert.Equal(3, projection.Projections.Projections.Count);
            Assert.Equal(before, GraphBytes(document));
            Assert.True(transaction.Commit().IsEmpty);
        }
        Assert.False(session.History.CanUndo);
        Assert.False(session.History.CanRedo);
    }

    [Fact]
    public void StreamSnapshotMutationCannotForgeOrInvalidateRetainedPlacement()
    {
        var document = CreateDocument(out var pageId, out var contentId, out var formId);
        var placement = CreatePlacementDescriptor(document, pageId, contentId, formId, "X1", 0, 6);
        var content = document.Objects.Get<PdfStream>(contentId);
        byte[] exposed = content.ToByteArray();
        Array.Clear(exposed);
        _ = PdfFormPlacementCloneProjector.Create(PdfWritePlanner.Create(document), placement, "Edited"u8);
        // Even restored identical bytes require fresh discovery: lexical context
        // cannot be assumed to survive an arbitrary intervening replacement.
        content.ReplaceData(content.ToByteArray());
        AssertRejectedUnchanged(document, placement);
    }
}
