using System.Text;
using BlackFlag.Engine.Document.Annotations;
using BlackFlag.Engine.Document.Model;
using BlackFlag.Engine.Document.Objects;
using BlackFlag.Engine.Document.Objects.Primitives;
using BlackFlag.Engine.Document.Pages;
using BlackFlag.Engine.Editing.Annotations;
using BlackFlag.Engine.Editing.Documents;
using BlackFlag.Engine.Editing.Pages;
using BlackFlag.Engine.Editing.Transactions;
using BlackFlag.Engine.Services.Editing;
using BlackFlag.Engine.Writing;

namespace BlackFlag.Engine.Services.Tests.Editing;

public sealed class PdfAnnotationTransactionWritingServiceTests
{
    private static readonly PdfName AnnotsName =
        PdfName.FromLatin1("Annots");
    private static readonly PdfName ContentsName =
        PdfName.FromLatin1("Contents");

    [Fact]
    public void IncrementalMixedTransactionTargetsPageExistingEditAndNewAnnotation()
    {
        (PdfDocument document, PdfObjectIdentifier pageId, PdfObjectIdentifier existingId) =
            CreateBoundDocument();
        using PdfEditingSession session =
            PdfEditingService.OpenSession(document);

        PdfEditableAnnotation added =
            session.Document.CreateAnnotation(
                PdfAnnotationSubtype.Link,
                new PdfAnnotationRectangle(10, 20, 50, 60));

        PdfEditChangeSet changeSet;
        using (var transaction = session.BeginTransaction())
        {
            PdfEditablePage page =
                session.Document.Pages[0];

            _ = session.Document.SetAnnotationContents(
                page,
                page.Annotations[0],
                "updated existing");
            _ = session.Document.AddAnnotation(
                page,
                added);
            _ = session.Document.SetAnnotationContents(
                page,
                added,
                "new annotation");
            changeSet = transaction.Commit();
        }

        PdfWritePlan writePlan =
            CreateIncrementalWritePlan(document);
        int newObjectNumber =
            writePlan.NextObjectNumber;

        PdfIncrementalWritePlan plan =
            PdfAnnotationTransactionWritingService.CreateIncrementalPlan(
                session,
                changeSet,
                writePlan,
                CreateSource());

        PdfObjectIdentifier addedId =
            new PdfObjectIdentifier(newObjectNumber);

        Assert.Equal(
            newObjectNumber,
            addedId.ObjectNumber);
        Assert.Contains(
            pageId,
            plan.ChangedObjectIdentifiers);
        Assert.Contains(
            existingId,
            plan.ChangedObjectIdentifiers);
        Assert.Contains(
            addedId,
            plan.ChangedObjectIdentifiers);

        Assert.Equal(
            "new annotation",
            document.Objects
                .Get<PdfDictionary>(addedId)
                .Get<PdfString>(ContentsName)
                .GetLatin1Text());
    }

    [Fact]
    public void CommonStateOnlyTransactionDoesNotRewritePage()
    {
        (PdfDocument document, PdfObjectIdentifier pageId, PdfObjectIdentifier annotationId) =
            CreateBoundDocument();
        using PdfEditingSession session =
            PdfEditingService.OpenSession(document);

        PdfEditChangeSet changeSet;
        using (var transaction = session.BeginTransaction())
        {
            _ = session.Document.SetAnnotationFlags(
                session.Document.Pages[0],
                session.Document.Pages[0].Annotations[0],
                PdfAnnotationOptions.Print);
            changeSet = transaction.Commit();
        }

        PdfIncrementalWritePlan plan =
            PdfAnnotationTransactionWritingService.CreateIncrementalPlan(
                session,
                changeSet,
                CreateIncrementalWritePlan(document),
                CreateSource());

        Assert.Equal(
            [annotationId],
            plan.ChangedObjectIdentifiers);
        Assert.DoesNotContain(
            pageId,
            plan.ChangedObjectIdentifiers);
    }

    [Fact]
    public void AddThenRemoveWithCommonEditProducesPageOnlyProjection()
    {
        (PdfDocument document, PdfObjectIdentifier pageId, _) =
            CreateBoundDocument();
        using PdfEditingSession session =
            PdfEditingService.OpenSession(document);

        PdfEditableAnnotation added =
            session.Document.CreateAnnotation(
                PdfAnnotationSubtype.Text,
                new PdfAnnotationRectangle(5, 5, 15, 15));

        PdfEditChangeSet changeSet;
        using (var transaction = session.BeginTransaction())
        {
            PdfEditablePage page =
                session.Document.Pages[0];

            _ = session.Document.AddAnnotation(
                page,
                added);
            _ = session.Document.SetAnnotationContents(
                page,
                added,
                "temporary");
            _ = session.Document.RemoveAnnotation(
                page,
                added);
            changeSet = transaction.Commit();
        }

        PdfWritePlan writePlan =
            CreateIncrementalWritePlan(document);
        var wouldBeId =
            new PdfObjectIdentifier(
                writePlan.NextObjectNumber);

        PdfIncrementalWritePlan plan =
            PdfAnnotationTransactionWritingService.CreateIncrementalPlan(
                session,
                changeSet,
                writePlan,
                CreateSource());

        Assert.False(
            document.Objects.Contains(wouldBeId));
        Assert.Equal(
            [pageId],
            plan.ChangedObjectIdentifiers);
    }

    [Fact]
    public void CommonEditThenRemoveDoesNotRewriteDetachedAnnotation()
    {
        (PdfDocument document, PdfObjectIdentifier pageId, PdfObjectIdentifier annotationId) =
            CreateBoundDocument();
        PdfDictionary original =
            document.Objects.Get<PdfDictionary>(
                annotationId);

        using PdfEditingSession session =
            PdfEditingService.OpenSession(document);

        PdfEditChangeSet changeSet;
        using (var transaction = session.BeginTransaction())
        {
            PdfEditablePage page =
                session.Document.Pages[0];
            PdfEditableAnnotation annotation =
                page.Annotations[0];

            _ = session.Document.SetAnnotationContents(
                page,
                annotation,
                "detached edit");
            _ = session.Document.RemoveAnnotation(
                page,
                annotation);
            changeSet = transaction.Commit();
        }

        PdfIncrementalWritePlan plan =
            PdfAnnotationTransactionWritingService.CreateIncrementalPlan(
                session,
                changeSet,
                CreateIncrementalWritePlan(document),
                CreateSource());

        Assert.Equal(
            [pageId],
            plan.ChangedObjectIdentifiers);
        Assert.Same(
            original,
            document.Objects.Get<PdfDictionary>(
                annotationId));
    }

    [Fact]
    public void PlanningFailureLeavesMixedNewAnnotationUnbound()
    {
        (PdfDocument document, _, _) =
            CreateBoundDocument(PdfVersion.Pdf14);
        using PdfEditingSession session =
            PdfEditingService.OpenSession(document);

        PdfEditableAnnotation added =
            session.Document.CreateAnnotation(
                PdfAnnotationSubtype.Text,
                new PdfAnnotationRectangle(1, 1, 20, 20));

        PdfEditChangeSet changeSet;
        using (var transaction = session.BeginTransaction())
        {
            PdfEditablePage page =
                session.Document.Pages[0];

            _ = session.Document.AddAnnotation(
                page,
                added);
            _ = session.Document.SetAnnotationContents(
                page,
                page.Annotations[0],
                "existing changed");
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
            new PdfObjectIdentifier(
                writePlan.NextObjectNumber);

        Assert.Throws<NotSupportedException>(
            () => PdfAnnotationTransactionWritingService.CreateIncrementalPlan(
                session,
                changeSet,
                writePlan,
                CreateSource()));

        Assert.False(
            document.Objects.Contains(expectedId));
    }

    private static (
        PdfDocument Document,
        PdfObjectIdentifier PageId,
        PdfObjectIdentifier AnnotationId)
        CreateBoundDocument(
            PdfVersion? version = null)
    {
        PdfDocument document =
            PdfDocument.CreateEmpty(
                version ?? PdfVersion.Pdf17);

        var rootId = new PdfObjectIdentifier(1);
        var treeId = new PdfObjectIdentifier(2);
        var pageId = new PdfObjectIdentifier(3);
        var annotationId = new PdfObjectIdentifier(4);

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

        var annotation =
            new PdfAnnotation(
                PdfAnnotationSubtype.Text,
                new PdfAnnotationRectangle(0, 0, 100, 100));
        annotation.BindObjectIdentifier(
            annotationId);
        page.InsertAnnotation(
            0,
            annotation);

        PdfDictionary pageDictionary =
            page.CreateDictionarySnapshot();
        pageDictionary.Set(
            AnnotsName,
            new PdfArray(
            [
                new PdfIndirectReference(
                    annotationId)
            ]));

        document.Objects.Add(
            pageId,
            pageDictionary);
        document.Objects.Add(
            annotationId,
            CreateAnnotationDictionary());
        document.Trailer.SetRootObject(
            rootId);

        return (
            document,
            pageId,
            annotationId);
    }

    private static PdfDictionary CreateAnnotationDictionary()
    {
        return new PdfDictionary
        {
            {
                PdfName.FromLatin1("Type"),
                PdfName.FromLatin1("Annot")
            },
            {
                PdfName.FromLatin1("Subtype"),
                PdfName.FromLatin1("Text")
            },
            {
                PdfName.FromLatin1("Rect"),
                new PdfArray(
                [
                    new PdfInteger(0),
                    new PdfInteger(0),
                    new PdfInteger(100),
                    new PdfInteger(100)
                ])
            }
        };
    }

    private static PdfWritePlan CreateIncrementalWritePlan(
        PdfDocument document)
    {
        return PdfWritePlanner.Create(
            document,
            new PdfWriteOptions
            {
                Mode = PdfWriteMode.IncrementalUpdate,
                CrossReferenceMode =
                    PdfCrossReferenceWriteMode.TraditionalTable
            });
    }

    private static PdfMemorySourceRevision CreateSource()
    {
        return new PdfMemorySourceRevision(
            Encoding.ASCII.GetBytes(
                "%PDF-1.7\nxref\n%%EOF\n"),
            9,
            PdfCrossReferenceWriteMode.TraditionalTable);
    }
}
