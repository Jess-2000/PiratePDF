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

public sealed class PdfPageTransactionWritingServiceTests
{
    private static readonly PdfName KidsName =
        PdfName.FromLatin1("Kids");
    private static readonly PdfName RotateName =
        PdfName.FromLatin1("Rotate");
    private static readonly PdfName MediaBoxName =
        PdfName.FromLatin1("MediaBox");

    [Fact]
    public void IncrementalInsertResizeRotateUsesOneAtomicProjection()
    {
        (PdfDocument document, PdfObjectIdentifier treeId, _) =
            CreateBoundDocument();
        using PdfEditingSession session =
            PdfEditingService.OpenSession(document);

        PdfEditablePage added =
            session.Document.CreatePage(
                300,
                400);

        PdfEditChangeSet changeSet;
        using (var transaction = session.BeginTransaction())
        {
            _ = session.Document.InsertPage(
                1,
                added);
            _ = session.Document.ResizePage(
                added,
                500,
                700);
            _ = session.Document.SetPageRotation(
                added,
                PdfPageRotation.Clockwise90);

            changeSet = transaction.Commit();
        }

        PdfWritePlan writePlan =
            CreateIncrementalWritePlan(document);
        int expectedObjectNumber =
            writePlan.NextObjectNumber;

        PdfIncrementalWritePlan plan =
            PdfPageTransactionWritingService.CreateIncrementalPlan(
                session,
                changeSet,
                writePlan,
                CreateSource());

        PdfObjectIdentifier addedId =
            Assert.IsType<PdfObjectIdentifier>(
                added.ObjectIdentifier);

        Assert.Equal(
            expectedObjectNumber,
            addedId.ObjectNumber);
        Assert.Contains(
            treeId,
            plan.ChangedObjectIdentifiers);
        Assert.Contains(
            addedId,
            plan.ChangedObjectIdentifiers);

        PdfDictionary addedDictionary =
            document.Objects.Get<PdfDictionary>(
                addedId);

        Assert.Equal(
            90L,
            addedDictionary
                .Get<PdfInteger>(RotateName)
                .Value);

        PdfArray mediaBox =
            addedDictionary.Get<PdfArray>(
                MediaBoxName);

        Assert.Equal(
            500L,
            Assert.IsType<PdfInteger>(
                mediaBox[2]).Value);
        Assert.Equal(
            700L,
            Assert.IsType<PdfInteger>(
                mediaBox[3]).Value);
    }

    [Fact]
    public void MoveExistingAndRotateAnotherTargetsTreeAndRotatedPage()
    {
        (PdfDocument document, PdfObjectIdentifier treeId, PdfObjectIdentifier firstId) =
            CreateBoundDocument();
        PdfPage second =
            AddBoundPage(
                document,
                new PdfObjectIdentifier(4));

        using PdfEditingSession session =
            PdfEditingService.OpenSession(document);

        PdfEditChangeSet changeSet;
        using (var transaction = session.BeginTransaction())
        {
            _ = session.Document.MovePage(
                session.Document.Pages[1],
                0);
            _ = session.Document.SetPageRotation(
                session.Document.Pages[0],
                PdfPageRotation.Clockwise270);

            changeSet = transaction.Commit();
        }

        PdfIncrementalWritePlan plan =
            PdfPageTransactionWritingService.CreateIncrementalPlan(
                session,
                changeSet,
                CreateIncrementalWritePlan(document),
                CreateSource());

        Assert.Contains(
            treeId,
            plan.ChangedObjectIdentifiers);
        Assert.Contains(
            second.ObjectIdentifier!.Value,
            plan.ChangedObjectIdentifiers);
        Assert.DoesNotContain(
            firstId,
            plan.ChangedObjectIdentifiers);
    }

    [Fact]
    public void ReplaceWithNewPageAndResizeProjectsReplacementFinalState()
    {
        (PdfDocument document, PdfObjectIdentifier treeId, PdfObjectIdentifier oldId) =
            CreateBoundDocument();
        using PdfEditingSession session =
            PdfEditingService.OpenSession(document);

        PdfEditablePage replacement =
            session.Document.CreatePage(
                300,
                400);

        PdfEditChangeSet changeSet;
        using (var transaction = session.BeginTransaction())
        {
            _ = session.Document.ReplacePage(
                session.Document.Pages[0],
                replacement);
            _ = session.Document.ResizePage(
                replacement,
                450,
                650);

            changeSet = transaction.Commit();
        }

        PdfWritePlan plan =
            PdfPageTransactionWritingService.CreateFullRewritePlan(
                session,
                changeSet,
                PdfWritePlanner.Create(document));

        PdfObjectIdentifier replacementId =
            Assert.IsType<PdfObjectIdentifier>(
                replacement.ObjectIdentifier);

        Assert.Contains(
            replacementId,
            plan.ExistingObjectIdentifiers);
        Assert.True(
            document.Objects.Contains(
                oldId));

        PdfArray kids =
            document.Objects
                .Get<PdfDictionary>(treeId)
                .Get<PdfArray>(KidsName);

        Assert.Equal(
            replacementId,
            kids.Get<PdfIndirectReference>(0).Identifier);

        PdfArray mediaBox =
            document.Objects
                .Get<PdfDictionary>(replacementId)
                .Get<PdfArray>(MediaBoxName);

        Assert.Equal(
            450L,
            Assert.IsType<PdfInteger>(
                mediaBox[2]).Value);
        Assert.Equal(
            650L,
            Assert.IsType<PdfInteger>(
                mediaBox[3]).Value);
    }

    [Fact]
    public void InsertThenRemoveWithStateEditsAllocatesNothing()
    {
        (PdfDocument document, PdfObjectIdentifier treeId, _) =
            CreateBoundDocument();
        using PdfEditingSession session =
            PdfEditingService.OpenSession(document);

        PdfEditablePage added =
            session.Document.CreatePage(
                300,
                400);

        PdfEditChangeSet changeSet;
        using (var transaction = session.BeginTransaction())
        {
            _ = session.Document.InsertPage(
                1,
                added);
            _ = session.Document.SetPageRotation(
                added,
                PdfPageRotation.Clockwise90);
            _ = session.Document.RemovePage(
                added);

            changeSet = transaction.Commit();
        }

        PdfWritePlan writePlan =
            CreateIncrementalWritePlan(document);
        var wouldBeId =
            new PdfObjectIdentifier(
                writePlan.NextObjectNumber);

        PdfIncrementalWritePlan plan =
            PdfPageTransactionWritingService.CreateIncrementalPlan(
                session,
                changeSet,
                writePlan,
                CreateSource());

        Assert.Null(
            added.ObjectIdentifier);
        Assert.False(
            document.Objects.Contains(
                wouldBeId));
        Assert.Equal(
            [treeId],
            plan.ChangedObjectIdentifiers);
    }

    [Fact]
    public void PageStateOnlyTransactionStillOmitsPageTree()
    {
        (PdfDocument document, PdfObjectIdentifier treeId, PdfObjectIdentifier pageId) =
            CreateBoundDocument();
        using PdfEditingSession session =
            PdfEditingService.OpenSession(document);

        PdfEditChangeSet changeSet;
        using (var transaction = session.BeginTransaction())
        {
            _ = session.Document.CropPage(
                session.Document.Pages[0],
                new PdfPageBox(
                    10,
                    20,
                    500,
                    700));
            _ = session.Document.SetPageRotation(
                session.Document.Pages[0],
                PdfPageRotation.Clockwise180);

            changeSet = transaction.Commit();
        }

        PdfIncrementalWritePlan plan =
            PdfPageTransactionWritingService.CreateIncrementalPlan(
                session,
                changeSet,
                CreateIncrementalWritePlan(document),
                CreateSource());

        Assert.Equal(
            [pageId],
            plan.ChangedObjectIdentifiers);
        Assert.DoesNotContain(
            treeId,
            plan.ChangedObjectIdentifiers);
    }

    [Fact]
    public void PlanningFailureLeavesMixedNewPageUnbound()
    {
        (PdfDocument document, _, _) =
            CreateBoundDocument(
                PdfVersion.Pdf14);
        using PdfEditingSession session =
            PdfEditingService.OpenSession(document);

        PdfEditablePage added =
            session.Document.CreatePage(
                300,
                400);

        PdfEditChangeSet changeSet;
        using (var transaction = session.BeginTransaction())
        {
            _ = session.Document.InsertPage(
                1,
                added);
            _ = session.Document.ResizePage(
                added,
                500,
                700);

            changeSet = transaction.Commit();
        }

        PdfWritePlan writePlan =
            PdfWritePlanner.Create(
                document,
                new PdfWriteOptions
                {
                    Mode =
                        PdfWriteMode.IncrementalUpdate,
                    CrossReferenceMode =
                        PdfCrossReferenceWriteMode.CrossReferenceStream
                });
        var expectedId =
            new PdfObjectIdentifier(
                writePlan.NextObjectNumber);

        Assert.Throws<NotSupportedException>(
            () => PdfPageTransactionWritingService.CreateIncrementalPlan(
                session,
                changeSet,
                writePlan,
                CreateSource()));

        Assert.Null(
            added.ObjectIdentifier);
        Assert.False(
            document.Objects.Contains(
                expectedId));
    }

    private static (
        PdfDocument Document,
        PdfObjectIdentifier TreeId,
        PdfObjectIdentifier FirstPageId)
        CreateBoundDocument(
            PdfVersion? version = null)
    {
        PdfDocument document =
            PdfDocument.CreateEmpty(
                version ?? PdfVersion.Pdf17);

        var rootId =
            new PdfObjectIdentifier(1);
        var treeId =
            new PdfObjectIdentifier(2);
        var pageId =
            new PdfObjectIdentifier(3);

        document.BindCoreStructure(
            rootId,
            treeId);
        document.Objects.Add(
            rootId,
            new PdfDictionary());
        document.Objects.Add(
            treeId,
            document.Pages.CreateDictionarySnapshot());

        var page =
            new PdfPage(612, 792);
        page.BindObjectIdentifier(
            pageId);
        document.Pages.Add(
            page);
        document.Objects.Add(
            pageId,
            page.CreateDictionarySnapshot());
        document.Trailer.SetRootObject(
            rootId);

        ProjectCurrentTree(
            document,
            treeId);

        return (
            document,
            treeId,
            pageId);
    }

    private static PdfPage AddBoundPage(
        PdfDocument document,
        PdfObjectIdentifier identifier)
    {
        var page =
            new PdfPage(612, 792);
        page.BindObjectIdentifier(
            identifier);
        document.Pages.Add(
            page);
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
            document.Objects.Get<PdfDictionary>(
                treeId);

        tree.Set(
            KidsName,
            new PdfArray(
                document.Pages.Select(
                    page =>
                        (PdfObject)new PdfIndirectReference(
                            page.ObjectIdentifier!.Value))));

        tree.Set(
            PdfName.FromLatin1("Count"),
            new PdfInteger(
                document.Pages.Count));
    }

    private static PdfWritePlan CreateIncrementalWritePlan(
        PdfDocument document) =>
        PdfWritePlanner.Create(
            document,
            new PdfWriteOptions
            {
                Mode =
                    PdfWriteMode.IncrementalUpdate,
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
