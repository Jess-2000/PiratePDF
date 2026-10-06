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

public sealed class PdfPageStateWritingServiceTests
{
    private static readonly PdfName MediaBoxName =
        PdfName.FromLatin1("MediaBox");
    private static readonly PdfName RotateName =
        PdfName.FromLatin1("Rotate");

    [Fact]
    public void IncrementalMixedGeometryAndRotationTargetsPageOnce()
    {
        (PdfDocument document, PdfObjectIdentifier pageId) =
            CreateBoundDocument();
        using PdfEditingSession session =
            PdfEditingService.OpenSession(document);

        PdfEditChangeSet changeSet;
        using (var transaction = session.BeginTransaction())
        {
            var page = session.Document.Pages[0];

            _ = session.Document.ResizePage(
                page,
                500,
                700);
            _ = session.Document.SetPageRotation(
                page,
                PdfPageRotation.Clockwise90);
            _ = session.Document.SetPageBox(
                page,
                PdfPageBoxKind.BleedBox,
                new PdfPageBox(5, 10, 495, 690));

            changeSet = transaction.Commit();
        }

        PdfIncrementalWritePlan plan =
            PdfPageStateWritingService.CreateIncrementalPlan(
                session,
                changeSet,
                CreateIncrementalWritePlan(document),
                CreateSource());

        Assert.Equal(
            [pageId],
            plan.ChangedObjectIdentifiers);

        PdfDictionary dictionary =
            document.Objects.Get<PdfDictionary>(pageId);

        Assert.Equal(
            90L,
            dictionary.Get<PdfInteger>(RotateName).Value);

        PdfArray mediaBox =
            dictionary.Get<PdfArray>(MediaBoxName);

        Assert.Equal(
            500L,
            Assert.IsType<PdfInteger>(mediaBox[2]).Value);
        Assert.Equal(
            700L,
            Assert.IsType<PdfInteger>(mediaBox[3]).Value);
    }

    [Fact]
    public void FullRewriteMixedTransactionUsesFinalAuthoritativeState()
    {
        (PdfDocument document, PdfObjectIdentifier pageId) =
            CreateBoundDocument();
        using PdfEditingSession session =
            PdfEditingService.OpenSession(document);

        PdfEditChangeSet changeSet;
        using (var transaction = session.BeginTransaction())
        {
            var page = session.Document.Pages[0];

            _ = session.Document.SetPageRotation(
                page,
                PdfPageRotation.Clockwise90);
            _ = session.Document.SetPageRotation(
                page,
                PdfPageRotation.Clockwise180);
            _ = session.Document.CropPage(
                page,
                new PdfPageBox(10, 20, 500, 700));

            changeSet = transaction.Commit();
        }

        PdfWritePlan plan =
            PdfPageStateWritingService.CreateFullRewritePlan(
                session,
                changeSet,
                PdfWritePlanner.Create(document));

        Assert.Contains(
            pageId,
            plan.ExistingObjectIdentifiers);

        PdfDictionary dictionary =
            document.Objects.Get<PdfDictionary>(pageId);

        Assert.Equal(
            180L,
            dictionary.Get<PdfInteger>(RotateName).Value);
    }

    [Fact]
    public void MixedTwoPageStateTransactionTargetsEachPageOnce()
    {
        PdfDocument document =
            CreateTwoPageDocument(
                out PdfObjectIdentifier firstId,
                out PdfObjectIdentifier secondId);

        using PdfEditingSession session =
            PdfEditingService.OpenSession(document);

        PdfEditChangeSet changeSet;
        using (var transaction = session.BeginTransaction())
        {
            _ = session.Document.SetPageRotation(
                session.Document.Pages[0],
                PdfPageRotation.Clockwise90);
            _ = session.Document.CropPage(
                session.Document.Pages[1],
                new PdfPageBox(5, 5, 400, 600));
            _ = session.Document.SetPageRotation(
                session.Document.Pages[1],
                PdfPageRotation.Clockwise270);

            changeSet = transaction.Commit();
        }

        PdfIncrementalWritePlan plan =
            PdfPageStateWritingService.CreateIncrementalPlan(
                session,
                changeSet,
                CreateIncrementalWritePlan(document),
                CreateSource());

        Assert.Equal(
            [firstId, secondId],
            plan.ChangedObjectIdentifiers);
    }

    [Fact]
    public void StructuralPageOperationIsRejected()
    {
        (PdfDocument document, _) =
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
            changeSet = transaction.Commit();
        }

        Assert.Throws<NotSupportedException>(
            () => PdfPageStateWritingService.CreateFullRewritePlan(
                session,
                changeSet,
                PdfWritePlanner.Create(document)));
    }

    [Fact]
    public void AnnotationOperationIsRejected()
    {
        (PdfDocument document, _) =
            CreateBoundDocument();
        using PdfEditingSession session =
            PdfEditingService.OpenSession(document);

        var annotation =
            session.Document.CreateAnnotation(
                Document.Annotations.PdfAnnotationSubtype.Text,
                new Document.Annotations.PdfAnnotationRectangle(
                    0,
                    0,
                    20,
                    20));

        PdfEditChangeSet changeSet;
        using (var transaction = session.BeginTransaction())
        {
            _ = session.Document.AddAnnotation(
                session.Document.Pages[0],
                annotation);
            changeSet = transaction.Commit();
        }

        Assert.Throws<NotSupportedException>(
            () => PdfPageStateWritingService.CreateFullRewritePlan(
                session,
                changeSet,
                PdfWritePlanner.Create(document)));
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

    private static PdfDocument CreateTwoPageDocument(
        out PdfObjectIdentifier firstId,
        out PdfObjectIdentifier secondId)
    {
        PdfDocument document =
            PdfDocument.CreateEmpty();
        var rootId =
            new PdfObjectIdentifier(1);
        var treeId =
            new PdfObjectIdentifier(2);
        firstId =
            new PdfObjectIdentifier(3);
        secondId =
            new PdfObjectIdentifier(4);

        document.BindCoreStructure(
            rootId,
            treeId);
        document.Objects.Add(
            rootId,
            new PdfDictionary());
        document.Objects.Add(
            treeId,
            document.Pages.CreateDictionarySnapshot());

        var first =
            new PdfPage(612, 792);
        first.BindObjectIdentifier(firstId);
        document.Pages.Add(first);
        document.Objects.Add(
            firstId,
            first.CreateDictionarySnapshot());

        var second =
            new PdfPage(612, 792);
        second.BindObjectIdentifier(secondId);
        document.Pages.Add(second);
        document.Objects.Add(
            secondId,
            second.CreateDictionarySnapshot());

        document.Trailer.SetRootObject(rootId);

        return document;
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
