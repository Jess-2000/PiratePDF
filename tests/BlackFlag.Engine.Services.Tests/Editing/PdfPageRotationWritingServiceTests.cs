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

public sealed class PdfPageRotationWritingServiceTests
{
    private static readonly PdfName RotateName =
        PdfName.FromLatin1("Rotate");

    [Fact]
    public void IncrementalRotationTargetsOnlyOwningPage()
    {
        (PdfDocument document, PdfObjectIdentifier pageId) =
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

        PdfIncrementalWritePlan plan =
            PdfPageRotationWritingService.CreateIncrementalPlan(
                session,
                changeSet,
                CreateIncrementalWritePlan(document),
                CreateSource());

        Assert.Equal(
            [pageId],
            plan.ChangedObjectIdentifiers);

        Assert.Equal(
            90L,
            document.Objects
                .Get<PdfDictionary>(pageId)
                .Get<PdfInteger>(RotateName)
                .Value);
    }

    [Fact]
    public void RepeatedRotationChangesAreDeduplicated()
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
            changeSet = transaction.Commit();
        }

        PdfWritePlan plan =
            PdfPageRotationWritingService.CreateFullRewritePlan(
                session,
                changeSet,
                PdfWritePlanner.Create(document));

        Assert.Equal(
            180L,
            document.Objects
                .Get<PdfDictionary>(pageId)
                .Get<PdfInteger>(RotateName)
                .Value);
    }

    [Fact]
    public void GeometryOperationIsRejected()
    {
        (PdfDocument document, _) =
            CreateBoundDocument();
        using PdfEditingSession session =
            PdfEditingService.OpenSession(document);

        PdfEditChangeSet changeSet;
        using (var transaction = session.BeginTransaction())
        {
            _ = session.Document.CropPage(
                session.Document.Pages[0],
                new PdfPageBox(10, 10, 500, 700));
            changeSet = transaction.Commit();
        }

        Assert.Throws<NotSupportedException>(
            () => PdfPageRotationWritingService.CreateFullRewritePlan(
                session,
                changeSet,
                PdfWritePlanner.Create(document)));
    }

    [Fact]
    public void StructuralPageOperationIsRejected()
    {
        (PdfDocument document, _) =
            CreateBoundDocument();
        using PdfEditingSession session =
            PdfEditingService.OpenSession(document);

        PdfEditablePage added =
            session.Document.CreatePage(300, 400);

        PdfEditChangeSet changeSet;
        using (var transaction = session.BeginTransaction())
        {
            _ = session.Document.InsertPage(
                1,
                added);
            changeSet = transaction.Commit();
        }

        Assert.Throws<NotSupportedException>(
            () => PdfPageRotationWritingService.CreateFullRewritePlan(
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
