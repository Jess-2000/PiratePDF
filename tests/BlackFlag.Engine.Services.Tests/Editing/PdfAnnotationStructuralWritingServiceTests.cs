using System.Text;
using BlackFlag.Engine.Document.Annotations;
using BlackFlag.Engine.Document.Model;
using BlackFlag.Engine.Document.Objects;
using BlackFlag.Engine.Document.Objects.Primitives;
using BlackFlag.Engine.Document.Pages;
using BlackFlag.Engine.Editing.Annotations;
using BlackFlag.Engine.Editing.Documents;
using BlackFlag.Engine.Editing.Transactions;
using BlackFlag.Engine.Services.Editing;
using BlackFlag.Engine.Writing;

namespace BlackFlag.Engine.Services.Tests.Editing;

public sealed class PdfAnnotationStructuralWritingServiceTests
{
    private static readonly PdfName AnnotsName =
        PdfName.FromLatin1("Annots");

    [Fact]
    public void IncrementalAddAllocatesAnnotationAndTargetsOwningPage()
    {
        (PdfDocument document, PdfObjectIdentifier pageId, _) =
            CreateBoundDocument();
        using PdfEditingSession session =
            PdfEditingService.OpenSession(document);

        PdfEditableAnnotation added =
            session.Document.CreateAnnotation(
                PdfAnnotationSubtype.Text,
                new PdfAnnotationRectangle(10, 20, 40, 60));

        PdfEditChangeSet changeSet;
        using (var transaction = session.BeginTransaction())
        {
            _ = session.Document.AddAnnotation(
                session.Document.Pages[0],
                added);
            changeSet = transaction.Commit();
        }

        PdfWritePlan writePlan =
            CreateIncrementalWritePlan(document);
        int expectedObjectNumber =
            writePlan.NextObjectNumber;

        PdfIncrementalWritePlan plan =
            PdfAnnotationStructuralWritingService.CreateIncrementalPlan(
                session,
                changeSet,
                writePlan,
                CreateSource());

        var addedId =
            new PdfObjectIdentifier(expectedObjectNumber);

        Assert.Equal(
            expectedObjectNumber,
            addedId.ObjectNumber);
        Assert.Contains(
            addedId,
            plan.ChangedObjectIdentifiers);
        Assert.Contains(
            pageId,
            plan.ChangedObjectIdentifiers);
    }

    [Fact]
    public void IncrementalRemoveTargetsPageWithoutDeletingDetachedObject()
    {
        (PdfDocument document, PdfObjectIdentifier pageId, PdfObjectIdentifier annotationId) =
            CreateBoundDocument();
        using PdfEditingSession session =
            PdfEditingService.OpenSession(document);

        PdfEditChangeSet changeSet;
        using (var transaction = session.BeginTransaction())
        {
            _ = session.Document.RemoveAnnotation(
                session.Document.Pages[0],
                session.Document.Pages[0].Annotations[0]);
            changeSet = transaction.Commit();
        }

        PdfIncrementalWritePlan plan =
            PdfAnnotationStructuralWritingService.CreateIncrementalPlan(
                session,
                changeSet,
                CreateIncrementalWritePlan(document),
                CreateSource());

        Assert.Equal(
            [pageId],
            plan.ChangedObjectIdentifiers);
        Assert.True(
            document.Objects.Contains(annotationId));

        PdfDictionary page =
            document.Objects.Get<PdfDictionary>(pageId);
        Assert.False(
            page.ContainsKey(AnnotsName));
    }

    [Fact]
    public void TwoPageAdditionsShareOneAllocatorSequence()
    {
        (PdfDocument document, _, _) =
            CreateBoundDocument();
        PdfPage secondPage =
            AddBoundPage(
                document,
                new PdfObjectIdentifier(5));

        using PdfEditingSession session =
            PdfEditingService.OpenSession(document);

        PdfEditableAnnotation first =
            session.Document.CreateAnnotation(
                PdfAnnotationSubtype.Text,
                new PdfAnnotationRectangle(1, 1, 20, 20));
        PdfEditableAnnotation second =
            session.Document.CreateAnnotation(
                PdfAnnotationSubtype.Link,
                new PdfAnnotationRectangle(2, 2, 30, 30));

        PdfEditChangeSet changeSet;
        using (var transaction = session.BeginTransaction())
        {
            _ = session.Document.AddAnnotation(
                session.Document.Pages[0],
                first);
            _ = session.Document.AddAnnotation(
                session.Document.Pages[1],
                second);
            changeSet = transaction.Commit();
        }

        PdfWritePlan writePlan =
            PdfWritePlanner.Create(document);
        int firstExpected =
            writePlan.NextObjectNumber;

        _ = PdfAnnotationStructuralWritingService.CreateFullRewritePlan(
            session,
            changeSet,
            writePlan);

        Assert.True(
            document.Objects.Contains(
                new PdfObjectIdentifier(firstExpected)));
        Assert.True(
            document.Objects.Contains(
                new PdfObjectIdentifier(firstExpected + 1)));
        Assert.Equal(
            secondPage.ObjectIdentifier,
            new PdfObjectIdentifier(5));
    }

    [Fact]
    public void AddThenRemoveDoesNotAllocateCancelledAnnotation()
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
            _ = session.Document.AddAnnotation(
                session.Document.Pages[0],
                added);
            _ = session.Document.RemoveAnnotation(
                session.Document.Pages[0],
                added);
            changeSet = transaction.Commit();
        }

        PdfWritePlan writePlan =
            CreateIncrementalWritePlan(document);
        var wouldBeIdentifier =
            new PdfObjectIdentifier(
                writePlan.NextObjectNumber);

        PdfIncrementalWritePlan plan =
            PdfAnnotationStructuralWritingService.CreateIncrementalPlan(
                session,
                changeSet,
                writePlan,
                CreateSource());

        Assert.False(
            document.Objects.Contains(
                wouldBeIdentifier));
        Assert.Equal(
            [pageId],
            plan.ChangedObjectIdentifiers);
    }

    [Fact]
    public void PlanningFailureLeavesNewAnnotationUnbound()
    {
        (PdfDocument document, _, _) =
            CreateBoundDocument(PdfVersion.Pdf14);
        using PdfEditingSession session =
            PdfEditingService.OpenSession(document);

        PdfEditableAnnotation added =
            session.Document.CreateAnnotation(
                PdfAnnotationSubtype.Text,
                new PdfAnnotationRectangle(1, 1, 10, 10));

        PdfEditChangeSet changeSet;
        using (var transaction = session.BeginTransaction())
        {
            _ = session.Document.AddAnnotation(
                session.Document.Pages[0],
                added);
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
        var expectedIdentifier =
            new PdfObjectIdentifier(
                writePlan.NextObjectNumber);

        Assert.Throws<NotSupportedException>(
            () => PdfAnnotationStructuralWritingService.CreateIncrementalPlan(
                session,
                changeSet,
                writePlan,
                CreateSource()));

        Assert.False(
            document.Objects.Contains(
                expectedIdentifier));
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
        page.BindObjectIdentifier(pageId);
        document.Pages.Add(page);

        var annotation =
            new PdfAnnotation(
                PdfAnnotationSubtype.Text,
                new PdfAnnotationRectangle(0, 0, 100, 100));
        annotation.BindObjectIdentifier(annotationId);
        page.InsertAnnotation(0, annotation);

        PdfDictionary pageDictionary =
            page.CreateDictionarySnapshot();
        pageDictionary.Set(
            AnnotsName,
            new PdfArray(
            [
                new PdfIndirectReference(annotationId)
            ]));
        document.Objects.Add(
            pageId,
            pageDictionary);
        document.Objects.Add(
            annotationId,
            CreateAnnotationDictionary());

        document.Trailer.SetRootObject(rootId);

        return (
            document,
            pageId,
            annotationId);
    }

    private static PdfPage AddBoundPage(
        PdfDocument document,
        PdfObjectIdentifier identifier)
    {
        var page =
            new PdfPage(612, 792);
        page.BindObjectIdentifier(identifier);
        document.Pages.Add(page);
        document.Objects.Add(
            identifier,
            page.CreateDictionarySnapshot());

        PdfDictionary tree =
            document.Objects.Get<PdfDictionary>(
                document.Pages.ObjectIdentifier!.Value);
        tree.Set(
            PdfName.FromLatin1("Kids"),
            new PdfArray(
                document.Pages.Select(
                    item =>
                        (PdfObject)new PdfIndirectReference(
                            item.ObjectIdentifier!.Value))));
        tree.Set(
            PdfName.FromLatin1("Count"),
            new PdfInteger(
                document.Pages.Count));

        return page;
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
