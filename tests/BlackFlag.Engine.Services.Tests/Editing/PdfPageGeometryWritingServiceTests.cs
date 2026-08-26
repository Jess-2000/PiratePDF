using System.Text;
using BlackFlag.Engine.Document.Model;
using BlackFlag.Engine.Document.Objects;
using BlackFlag.Engine.Document.Objects.Primitives;
using BlackFlag.Engine.Document.Pages;
using BlackFlag.Engine.Editing.Documents;
using BlackFlag.Engine.Editing.Ownership;
using BlackFlag.Engine.Editing.Transactions;
using BlackFlag.Engine.Services.Editing;
using BlackFlag.Engine.Writing;

namespace BlackFlag.Engine.Services.Tests.Editing;

public sealed class PdfPageGeometryWritingServiceTests
{
    [Fact]
    public void IncrementalPlanDeduplicatesRepeatedGeometryChangesToPage()
    {
        (PdfDocument document, PdfObjectIdentifier pageId) =
            CreateBoundDocument();
        using PdfEditingSession session =
            PdfEditingService.OpenSession(document);

        PdfEditChangeSet changeSet;
        using (var transaction = session.BeginTransaction())
        {
            var page = session.Document.Pages[0];

            _ = session.Document.CropPage(
                page,
                new PdfPageBox(10, 20, 500, 700));
            _ = session.Document.SetPageBox(
                page,
                PdfPageBoxKind.BleedBox,
                new PdfPageBox(5, 10, 505, 710));
            changeSet = transaction.Commit();
        }

        PdfIncrementalWritePlan plan =
            PdfPageGeometryWritingService.CreateIncrementalPlan(
                session,
                changeSet,
                CreateIncrementalWritePlan(document),
                CreateSource());

        Assert.Equal(
            [pageId],
            plan.ChangedObjectIdentifiers);
    }

    [Fact]
    public void FullRewritePlanProjectsAuthoritativeGeometry()
    {
        (PdfDocument document, PdfObjectIdentifier pageId) =
            CreateBoundDocument();
        using PdfEditingSession session =
            PdfEditingService.OpenSession(document);

        PdfEditChangeSet changeSet;
        using (var transaction = session.BeginTransaction())
        {
            _ = session.Document.ResizePage(
                session.Document.Pages[0],
                500,
                700);
            changeSet = transaction.Commit();
        }

        _ = PdfPageGeometryWritingService.CreateFullRewritePlan(
            session,
            changeSet,
            PdfWritePlanner.Create(document));

        PdfArray mediaBox =
            document.Objects
                .Get<PdfDictionary>(pageId)
                .Get<PdfArray>(PdfName.FromLatin1("MediaBox"));

        Assert.Equal(
            500L,
            Assert.IsType<PdfInteger>(mediaBox[2]).Value);
        Assert.Equal(
            700L,
            Assert.IsType<PdfInteger>(mediaBox[3]).Value);
    }

    [Fact]
    public void NonGeometryOperationIsRejected()
    {
        (PdfDocument document, _) =
            CreateBoundDocument();
        using PdfEditingSession session =
            PdfEditingService.OpenSession(document);

        PdfEditChangeSet changeSet;
        using (var transaction = session.BeginTransaction())
        {
            _ = session.Document.SetPageRotation(
                session.Document.Pages[0],
                PdfPageRotation.Clockwise90);
            changeSet = transaction.Commit();
        }

        Assert.Throws<NotSupportedException>(
            () => PdfPageGeometryWritingService.CreateFullRewritePlan(
                session,
                changeSet,
                PdfWritePlanner.Create(document)));
    }

    [Fact]
    public void ChangeSetFromAnotherSessionIsRejected()
    {
        (PdfDocument firstDocument, _) =
            CreateBoundDocument();
        (PdfDocument secondDocument, _) =
            CreateBoundDocument();

        PdfEditChangeSet changeSet;
        using (PdfEditingSession first =
            PdfEditingService.OpenSession(firstDocument))
        {
            using var transaction = first.BeginTransaction();

            _ = first.Document.CropPage(
                first.Document.Pages[0],
                new PdfPageBox(10, 10, 500, 700));
            changeSet = transaction.Commit();
        }

        using PdfEditingSession second =
            PdfEditingService.OpenSession(secondDocument);

        Assert.Throws<PdfEditingOwnershipException>(
            () => PdfPageGeometryWritingService.CreateFullRewritePlan(
                second,
                changeSet,
                PdfWritePlanner.Create(secondDocument)));
    }

    private static (
        PdfDocument Document,
        PdfObjectIdentifier PageId)
        CreateBoundDocument()
    {
        PdfDocument document =
            PdfDocument.CreateEmpty();
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
        page.BindObjectIdentifier(pageId);
        document.Pages.Add(page);
        document.Objects.Add(
            pageId,
            page.CreateDictionarySnapshot());
        document.Trailer.SetRootObject(rootId);

        return (document, pageId);
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
