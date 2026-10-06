using System.Text;
using BlackFlag.Engine.Document.Annotations;
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

public sealed class PdfAnnotationCommonStateWritingServiceTests
{
    [Fact]
    public void IncrementalPlanDeduplicatesRepeatedChangesToSameAnnotation()
    {
        (PdfDocument document, PdfObjectIdentifier pageId, PdfObjectIdentifier annotationId) =
            CreateBoundDocument();
        using PdfEditingSession session =
            PdfEditingService.OpenSession(document);

        PdfEditChangeSet changeSet;
        using (var transaction = session.BeginTransaction())
        {
            var page = session.Document.Pages[0];
            var annotation = page.Annotations[0];

            _ = session.Document.SetAnnotationContents(
                page,
                annotation,
                "updated");
            _ = session.Document.SetAnnotationFlags(
                page,
                annotation,
                PdfAnnotationOptions.Print);
            changeSet = transaction.Commit();
        }

        PdfWritePlan writePlan =
            PdfWritePlanner.Create(
                document,
                new PdfWriteOptions
                {
                    Mode = PdfWriteMode.IncrementalUpdate,
                    CrossReferenceMode =
                        PdfCrossReferenceWriteMode.TraditionalTable
                });

        PdfIncrementalWritePlan plan =
            PdfAnnotationCommonStateWritingService.CreateIncrementalPlan(
                session,
                changeSet,
                writePlan,
                CreateSource());

        Assert.Equal(
            [annotationId],
            plan.ChangedObjectIdentifiers);
        Assert.DoesNotContain(
            pageId,
            plan.ChangedObjectIdentifiers);
    }

    [Fact]
    public void FullRewritePlanUsesCommittedCommonState()
    {
        (PdfDocument document, _, PdfObjectIdentifier annotationId) =
            CreateBoundDocument();
        using PdfEditingSession session =
            PdfEditingService.OpenSession(document);

        PdfEditChangeSet changeSet;
        using (var transaction = session.BeginTransaction())
        {
            var page = session.Document.Pages[0];
            var annotation = page.Annotations[0];

            _ = session.Document.SetAnnotationContents(
                page,
                annotation,
                "full rewrite");
            changeSet = transaction.Commit();
        }

        PdfWritePlan plan =
            PdfAnnotationCommonStateWritingService.CreateFullRewritePlan(
                session,
                changeSet,
                PdfWritePlanner.Create(document));

        Assert.Equal(
            "full rewrite",
            document.Objects
                .Get<PdfDictionary>(annotationId)
                .Get<PdfString>(PdfName.FromLatin1("Contents"))
                .GetLatin1Text());
        Assert.Contains(
            annotationId,
            plan.ExistingObjectIdentifiers);
    }

    [Fact]
    public void StructuralAnnotationMutationIsRejected()
    {
        (PdfDocument document, _, _) =
            CreateBoundDocument();
        using PdfEditingSession session =
            PdfEditingService.OpenSession(document);

        PdfEditChangeSet changeSet;
        using (var transaction = session.BeginTransaction())
        {
            var page = session.Document.Pages[0];
            var added = session.Document.CreateAnnotation(
                PdfAnnotationSubtype.Text,
                new PdfAnnotationRectangle(10, 10, 20, 20));

            _ = session.Document.AddAnnotation(
                page,
                added);
            changeSet = transaction.Commit();
        }

        Assert.Throws<NotSupportedException>(
            () => PdfAnnotationCommonStateWritingService.CreateFullRewritePlan(
                session,
                changeSet,
                PdfWritePlanner.Create(document)));
    }

    [Fact]
    public void ChangeSetFromAnotherSessionIsRejected()
    {
        (PdfDocument firstDocument, _, _) =
            CreateBoundDocument();
        (PdfDocument secondDocument, _, _) =
            CreateBoundDocument();

        PdfEditChangeSet changeSet;
        using (PdfEditingSession first =
            PdfEditingService.OpenSession(firstDocument))
        {
            using var transaction = first.BeginTransaction();
            _ = first.Document.SetAnnotationContents(
                first.Document.Pages[0],
                first.Document.Pages[0].Annotations[0],
                "first");
            changeSet = transaction.Commit();
        }

        using PdfEditingSession second =
            PdfEditingService.OpenSession(secondDocument);

        Assert.Throws<PdfEditingOwnershipException>(
            () => PdfAnnotationCommonStateWritingService.CreateFullRewritePlan(
                second,
                changeSet,
                PdfWritePlanner.Create(secondDocument)));
    }

    private static (
        PdfDocument Document,
        PdfObjectIdentifier PageId,
        PdfObjectIdentifier AnnotationId)
        CreateBoundDocument()
    {
        PdfDocument document = PdfDocument.CreateEmpty();
        var rootId = new PdfObjectIdentifier(1);
        var treeId = new PdfObjectIdentifier(2);
        var pageId = new PdfObjectIdentifier(3);
        var annotationId = new PdfObjectIdentifier(4);

        document.BindCoreStructure(rootId, treeId);
        document.Objects.Add(rootId, new PdfDictionary());
        document.Objects.Add(
            treeId,
            document.Pages.CreateDictionarySnapshot());

        var page = new PdfPage(612, 792);
        page.BindObjectIdentifier(pageId);
        document.Pages.Add(page);

        var annotation = new PdfAnnotation(
            PdfAnnotationSubtype.Text,
            new PdfAnnotationRectangle(0, 0, 100, 100));
        annotation.BindObjectIdentifier(annotationId);
        page.InsertAnnotation(0, annotation);

        PdfDictionary pageDictionary =
            page.CreateDictionarySnapshot();
        pageDictionary.Set(
            PdfName.FromLatin1("Annots"),
            new PdfArray(
            [
                new PdfIndirectReference(annotationId)
            ]));

        document.Objects.Add(pageId, pageDictionary);
        document.Objects.Add(
            annotationId,
            new PdfDictionary
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
            });
        document.Trailer.SetRootObject(rootId);

        return (document, pageId, annotationId);
    }

    private static PdfMemorySourceRevision CreateSource() =>
        new(
            Encoding.ASCII.GetBytes("%PDF-1.7\nxref\n%%EOF\n"),
            9,
            PdfCrossReferenceWriteMode.TraditionalTable);
}
