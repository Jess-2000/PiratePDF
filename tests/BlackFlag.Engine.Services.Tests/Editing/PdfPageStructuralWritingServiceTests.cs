using System.Text;

using BlackFlag.Engine.Document.Model;
using BlackFlag.Engine.Document.Objects;
using BlackFlag.Engine.Document.Objects.Primitives;
using BlackFlag.Engine.Document.Pages;
using BlackFlag.Engine.Editing.Documents;
using BlackFlag.Engine.Editing.Pages;
using BlackFlag.Engine.Editing.Transactions;
using BlackFlag.Engine.Services.Editing;
using BlackFlag.Engine.Writing;

namespace BlackFlag.Engine.Services.Tests.Editing;

public sealed class PdfPageStructuralWritingServiceTests
{
    private static readonly PdfName KidsName =
        PdfName.FromLatin1("Kids");
    private static readonly PdfName CountName =
        PdfName.FromLatin1("Count");

    [Fact]
    public void IncrementalInsertAllocatesNewPageAndTargetsPageTree()
    {
        (PdfDocument document, PdfObjectIdentifier treeId, _) =
            CreateBoundDocument();
        using PdfEditingSession session =
            PdfEditingService.OpenSession(document);

        PdfEditablePage added =
            session.Document.CreatePage(300, 400);

        PdfEditChangeSet changeSet;
        using (var transaction = session.BeginTransaction())
        {
            _ = session.Document.InsertPage(1, added);
            changeSet = transaction.Commit();
        }

        PdfWritePlan writePlan =
            CreateIncrementalWritePlan(document);
        int expectedObjectNumber =
            writePlan.NextObjectNumber;

        PdfIncrementalWritePlan plan =
            PdfPageStructuralWritingService.CreateIncrementalPlan(
                session,
                changeSet,
                writePlan,
                CreateSource());

        PdfObjectIdentifier addedId =
            Assert.IsType<PdfObjectIdentifier>(
                added.ObjectIdentifier);

        Assert.Equal(expectedObjectNumber, addedId.ObjectNumber);
        Assert.Contains(addedId, plan.ChangedObjectIdentifiers);
        Assert.Contains(treeId, plan.ChangedObjectIdentifiers);

        PdfDictionary tree =
            document.Objects.Get<PdfDictionary>(treeId);
        PdfArray kids =
            tree.Get<PdfArray>(KidsName);

        Assert.Equal(
            2L,
            tree.Get<PdfInteger>(CountName).Value);
        Assert.Equal(
            addedId,
            kids.Get<PdfIndirectReference>(1).Identifier);
    }

    [Fact]
    public void IncrementalRemoveTargetsOnlyPageTreeAndPreservesDetachedPageObject()
    {
        (PdfDocument document, PdfObjectIdentifier treeId, PdfObjectIdentifier firstId) =
            CreateBoundDocument();
        PdfPage second =
            AddBoundPage(document, new PdfObjectIdentifier(4));

        using PdfEditingSession session =
            PdfEditingService.OpenSession(document);

        PdfEditChangeSet changeSet;
        using (var transaction = session.BeginTransaction())
        {
            _ = session.Document.RemovePage(
                session.Document.Pages[0]);
            changeSet = transaction.Commit();
        }

        PdfIncrementalWritePlan plan =
            PdfPageStructuralWritingService.CreateIncrementalPlan(
                session,
                changeSet,
                CreateIncrementalWritePlan(document),
                CreateSource());

        Assert.Equal([treeId], plan.ChangedObjectIdentifiers);
        Assert.True(document.Objects.Contains(firstId));

        PdfArray kids =
            document.Objects
                .Get<PdfDictionary>(treeId)
                .Get<PdfArray>(KidsName);

        Assert.Single(kids);
        Assert.Equal(
            second.ObjectIdentifier,
            kids.Get<PdfIndirectReference>(0).Identifier);
    }

    [Fact]
    public void MoveReordersKidsWithoutRewritingPages()
    {
        (PdfDocument document, PdfObjectIdentifier treeId, PdfObjectIdentifier firstId) =
            CreateBoundDocument();
        PdfPage second =
            AddBoundPage(document, new PdfObjectIdentifier(4));

        using PdfEditingSession session =
            PdfEditingService.OpenSession(document);

        PdfEditChangeSet changeSet;
        using (var transaction = session.BeginTransaction())
        {
            _ = session.Document.MovePage(
                session.Document.Pages[1],
                0);
            changeSet = transaction.Commit();
        }

        PdfIncrementalWritePlan plan =
            PdfPageStructuralWritingService.CreateIncrementalPlan(
                session,
                changeSet,
                CreateIncrementalWritePlan(document),
                CreateSource());

        Assert.Equal([treeId], plan.ChangedObjectIdentifiers);
        Assert.DoesNotContain(firstId, plan.ChangedObjectIdentifiers);
        Assert.DoesNotContain(
            second.ObjectIdentifier!.Value,
            plan.ChangedObjectIdentifiers);
    }

    [Fact]
    public void ReplaceWithNewPageAllocatesReplacementAndPreservesRemovedObject()
    {
        (PdfDocument document, PdfObjectIdentifier treeId, PdfObjectIdentifier oldId) =
            CreateBoundDocument();
        using PdfEditingSession session =
            PdfEditingService.OpenSession(document);

        PdfEditablePage replacement =
            session.Document.CreatePage(500, 700);

        PdfEditChangeSet changeSet;
        using (var transaction = session.BeginTransaction())
        {
            _ = session.Document.ReplacePage(
                session.Document.Pages[0],
                replacement);
            changeSet = transaction.Commit();
        }

        PdfWritePlan plan =
            PdfPageStructuralWritingService.CreateFullRewritePlan(
                session,
                changeSet,
                PdfWritePlanner.Create(document));

        PdfObjectIdentifier replacementId =
            Assert.IsType<PdfObjectIdentifier>(
                replacement.ObjectIdentifier);

        Assert.Contains(
            replacementId,
            plan.ExistingObjectIdentifiers);
        Assert.True(document.Objects.Contains(oldId));

        PdfArray kids =
            document.Objects
                .Get<PdfDictionary>(treeId)
                .Get<PdfArray>(KidsName);

        Assert.Single(kids);
        Assert.Equal(
            replacementId,
            kids.Get<PdfIndirectReference>(0).Identifier);
    }

    [Fact]
    public void InsertThenRemoveDoesNotAllocateCancelledPage()
    {
        (PdfDocument document, PdfObjectIdentifier treeId, _) =
            CreateBoundDocument();
        using PdfEditingSession session =
            PdfEditingService.OpenSession(document);

        PdfEditablePage added =
            session.Document.CreatePage(300, 400);

        PdfEditChangeSet changeSet;
        using (var transaction = session.BeginTransaction())
        {
            _ = session.Document.InsertPage(1, added);
            _ = session.Document.RemovePage(added);
            changeSet = transaction.Commit();
        }

        PdfWritePlan writePlan =
            CreateIncrementalWritePlan(document);
        var wouldBeId =
            new PdfObjectIdentifier(writePlan.NextObjectNumber);

        PdfIncrementalWritePlan plan =
            PdfPageStructuralWritingService.CreateIncrementalPlan(
                session,
                changeSet,
                writePlan,
                CreateSource());

        Assert.Null(added.ObjectIdentifier);
        Assert.False(document.Objects.Contains(wouldBeId));
        Assert.Equal([treeId], plan.ChangedObjectIdentifiers);
    }

    [Fact]
    public void PlanningFailureLeavesNewPageUnbound()
    {
        (PdfDocument document, _, _) =
            CreateBoundDocument(PdfVersion.Pdf14);
        using PdfEditingSession session =
            PdfEditingService.OpenSession(document);

        PdfEditablePage added =
            session.Document.CreatePage(300, 400);

        PdfEditChangeSet changeSet;
        using (var transaction = session.BeginTransaction())
        {
            _ = session.Document.InsertPage(1, added);
            changeSet = transaction.Commit();
        }

        PdfWritePlan writePlan =
            PdfWritePlanner.Create(
                document,
                new PdfWriteOptions
                {
                    Mode = PdfWriteMode.IncrementalUpdate,
                    CrossReferenceMode =
                        PdfCrossReferenceWriteMode.CrossReferenceStream
                });
        var expectedId =
            new PdfObjectIdentifier(writePlan.NextObjectNumber);

        Assert.Throws<NotSupportedException>(
            () => PdfPageStructuralWritingService.CreateIncrementalPlan(
                session,
                changeSet,
                writePlan,
                CreateSource()));

        Assert.Null(added.ObjectIdentifier);
        Assert.False(document.Objects.Contains(expectedId));
    }

    private static (
        PdfDocument Document,
        PdfObjectIdentifier TreeId,
        PdfObjectIdentifier FirstPageId)
        CreateBoundDocument(
            PdfVersion? version = null)
    {
        PdfDocument document =
            PdfDocument.CreateEmpty(version ?? PdfVersion.Pdf17);

        var rootId = new PdfObjectIdentifier(1);
        var treeId = new PdfObjectIdentifier(2);
        var pageId = new PdfObjectIdentifier(3);

        document.BindCoreStructure(rootId, treeId);
        document.Objects.Add(rootId, new PdfDictionary());
        document.Objects.Add(
            treeId,
            document.Pages.CreateDictionarySnapshot());

        var page = new PdfPage(612, 792);
        page.BindObjectIdentifier(pageId);
        document.Pages.Add(page);
        document.Objects.Add(
            pageId,
            page.CreateDictionarySnapshot());
        document.Trailer.SetRootObject(rootId);

        ProjectCurrentTree(document, treeId);

        return (document, treeId, pageId);
    }

    private static PdfPage AddBoundPage(
        PdfDocument document,
        PdfObjectIdentifier identifier)
    {
        var page = new PdfPage(612, 792);
        page.BindObjectIdentifier(identifier);
        document.Pages.Add(page);
        document.Objects.Add(
            identifier,
            page.CreateDictionarySnapshot());

        ProjectCurrentTree(
            document,
            document.Pages.ObjectIdentifier!.Value);

        return page;
    }

    private static void ProjectCurrentTree(
        PdfDocument document,
        PdfObjectIdentifier treeId)
    {
        PdfDictionary tree =
            document.Objects.Get<PdfDictionary>(treeId);

        tree.Set(
            KidsName,
            new PdfArray(
                document.Pages.Select(
                    page =>
                        (PdfObject)new PdfIndirectReference(
                            page.ObjectIdentifier!.Value))));
        tree.Set(
            CountName,
            new PdfInteger(document.Pages.Count));
    }

    private static PdfWritePlan CreateIncrementalWritePlan(
        PdfDocument document) =>
        PdfWritePlanner.Create(
            document,
            new PdfWriteOptions
            {
                Mode = PdfWriteMode.IncrementalUpdate,
                CrossReferenceMode =
                    PdfCrossReferenceWriteMode.TraditionalTable
            });

    private static PdfMemorySourceRevision CreateSource() =>
        new(
            Encoding.ASCII.GetBytes(
                "%PDF-1.7\nxref\n%%EOF\n"),
            9,
            PdfCrossReferenceWriteMode.TraditionalTable);
}
