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

public sealed class PdfNestedPageTransactionWritingServiceTests
{
    private static readonly PdfName TypeName =
        PdfName.FromLatin1("Type");
    private static readonly PdfName PagesName =
        PdfName.FromLatin1("Pages");
    private static readonly PdfName PageName =
        PdfName.FromLatin1("Page");
    private static readonly PdfName KidsName =
        PdfName.FromLatin1("Kids");
    private static readonly PdfName CountName =
        PdfName.FromLatin1("Count");
    private static readonly PdfName ParentName =
        PdfName.FromLatin1("Parent");
    private static readonly PdfName RotateName =
        PdfName.FromLatin1("Rotate");
    private static readonly PdfName MediaBoxName =
        PdfName.FromLatin1("MediaBox");

    [Fact]
    public void CrossBranchMoveAndRotationMergeIntoOnePageProjection()
    {
        PdfDocument document =
            CreateTwoBranchDocument();
        using PdfEditingSession session =
            PdfEditingService.OpenSession(document);

        PdfEditChangeSet changeSet;
        using (var transaction = session.BeginTransaction())
        {
            PdfEditablePage moved =
                session.Document.Pages[0];

            _ = session.Document.MovePage(
                moved,
                1);
            _ = session.Document.SetPageRotation(
                moved,
                PdfPageRotation.Clockwise90);

            changeSet = transaction.Commit();
        }

        PdfIncrementalWritePlan plan =
            PdfNestedPageTransactionWritingService.CreateIncrementalPlan(
                session,
                changeSet,
                CreateIncrementalWritePlan(document),
                CreateSource());

        PdfDictionary movedDictionary =
            document.Objects.Get<PdfDictionary>(
                new PdfObjectIdentifier(5));

        Assert.Equal(
            new PdfObjectIdentifier(4),
            movedDictionary
                .Get<PdfIndirectReference>(ParentName)
                .Identifier);
        Assert.Equal(
            90L,
            movedDictionary
                .Get<PdfInteger>(RotateName)
                .Value);
        Assert.Single(plan.ChangedObjectIdentifiers, identifier =>
                    identifier ==
                    new PdfObjectIdentifier(5));
    }

    [Fact]
    public void InsertResizeRotateWritesFinalNewPageState()
    {
        PdfDocument document =
            CreateSingleBranchDocument();
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
                PdfPageRotation.Clockwise180);

            changeSet = transaction.Commit();
        }

        PdfWritePlan writePlan =
            CreateIncrementalWritePlan(document);

        _ = PdfNestedPageTransactionWritingService.CreateIncrementalPlan(
            session,
            changeSet,
            writePlan,
            CreateSource());

        PdfObjectIdentifier addedId =
            Assert.IsType<PdfObjectIdentifier>(
                added.ObjectIdentifier);
        PdfDictionary dictionary =
            document.Objects.Get<PdfDictionary>(
                addedId);

        Assert.Equal(
            new PdfObjectIdentifier(3),
            dictionary
                .Get<PdfIndirectReference>(ParentName)
                .Identifier);
        Assert.Equal(
            180L,
            dictionary
                .Get<PdfInteger>(RotateName)
                .Value);

        PdfArray mediaBox =
            dictionary.Get<PdfArray>(
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
    public void StateOnlyNestedTransactionDoesNotRewritePageTree()
    {
        PdfDocument document =
            CreateTwoBranchDocument();
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
                PdfPageRotation.Clockwise270);

            changeSet = transaction.Commit();
        }

        PdfIncrementalWritePlan plan =
            PdfNestedPageTransactionWritingService.CreateIncrementalPlan(
                session,
                changeSet,
                CreateIncrementalWritePlan(document),
                CreateSource());

        Assert.Equal(
            [new PdfObjectIdentifier(5)],
            plan.ChangedObjectIdentifiers);
        Assert.DoesNotContain(
            new PdfObjectIdentifier(2),
            plan.ChangedObjectIdentifiers);
        Assert.DoesNotContain(
            new PdfObjectIdentifier(3),
            plan.ChangedObjectIdentifiers);
    }

    [Fact]
    public void FullRewriteMixedTransactionMatchesFinalAuthoritativeState()
    {
        PdfDocument document =
            CreateTwoBranchDocument();
        using PdfEditingSession session =
            PdfEditingService.OpenSession(document);

        PdfEditChangeSet changeSet;
        using (var transaction = session.BeginTransaction())
        {
            PdfEditablePage moved =
                session.Document.Pages[0];

            _ = session.Document.MovePage(
                moved,
                1);
            _ = session.Document.ResizePage(
                moved,
                450,
                650);
            _ = session.Document.SetPageRotation(
                moved,
                PdfPageRotation.Clockwise180);

            changeSet = transaction.Commit();
        }

        PdfWritePlan plan =
            PdfNestedPageTransactionWritingService.CreateFullRewritePlan(
                session,
                changeSet,
                PdfWritePlanner.Create(document));

        Assert.Contains(
            new PdfObjectIdentifier(5),
            plan.ExistingObjectIdentifiers);

        PdfDictionary movedDictionary =
            document.Objects.Get<PdfDictionary>(
                new PdfObjectIdentifier(5));

        Assert.Equal(
            new PdfObjectIdentifier(4),
            movedDictionary
                .Get<PdfIndirectReference>(ParentName)
                .Identifier);
        Assert.Equal(
            180L,
            movedDictionary
                .Get<PdfInteger>(RotateName)
                .Value);

        PdfArray mediaBox =
            movedDictionary.Get<PdfArray>(
                MediaBoxName);

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
    public void PlanningFailureRestoresTreeAndLeavesNewPageUnbound()
    {
        PdfDocument document =
            CreateSingleBranchDocument(
                PdfVersion.Pdf14);
        PdfDictionary originalRoot =
            document.Objects.Get<PdfDictionary>(
                new PdfObjectIdentifier(2));
        PdfDictionary originalBranch =
            document.Objects.Get<PdfDictionary>(
                new PdfObjectIdentifier(3));

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

        PdfObjectIdentifier wouldBeId =
            new(
                writePlan.NextObjectNumber);

        Assert.Throws<NotSupportedException>(
            () => PdfNestedPageTransactionWritingService.CreateIncrementalPlan(
                session,
                changeSet,
                writePlan,
                CreateSource()));

        Assert.Null(
            added.ObjectIdentifier);
        Assert.False(
            document.Objects.Contains(
                wouldBeId));
        Assert.Same(
            originalRoot,
            document.Objects.Get<PdfDictionary>(
                new PdfObjectIdentifier(2)));
        Assert.Same(
            originalBranch,
            document.Objects.Get<PdfDictionary>(
                new PdfObjectIdentifier(3)));
    }

    [Fact]
    public void MultiOperationStructuralSequenceRemainsWithinTwoNodeBoundary()
    {
        PdfDocument document =
            CreateTwoBranchDocument();
        using PdfEditingSession session =
            PdfEditingService.OpenSession(document);

        PdfEditablePage added =
            session.Document.CreatePage(
                320,
                480);

        PdfEditChangeSet changeSet;
        using (var transaction = session.BeginTransaction())
        {
            _ = session.Document.InsertPage(
                1,
                added);
            _ = session.Document.MovePage(
                session.Document.Pages[0],
                2);
            _ = session.Document.SetPageRotation(
                added,
                PdfPageRotation.Clockwise90);

            changeSet = transaction.Commit();
        }

        PdfIncrementalWritePlan plan =
            PdfNestedPageTransactionWritingService.CreateIncrementalPlan(
                session,
                changeSet,
                CreateIncrementalWritePlan(document),
                CreateSource());

        PdfObjectIdentifier addedId =
            Assert.IsType<PdfObjectIdentifier>(
                added.ObjectIdentifier);

        Assert.Contains(
            new PdfObjectIdentifier(3),
            plan.ChangedObjectIdentifiers);
        Assert.Contains(
            new PdfObjectIdentifier(4),
            plan.ChangedObjectIdentifiers);
        Assert.Contains(
            addedId,
            plan.ChangedObjectIdentifiers);
    }

    private static PdfDocument CreateSingleBranchDocument(
        PdfVersion? version = null)
    {
        PdfDocument document =
            PdfDocument.CreateEmpty(
                version ?? PdfVersion.Pdf17);

        var catalogId =
            new PdfObjectIdentifier(1);
        var rootId =
            new PdfObjectIdentifier(2);
        var branchId =
            new PdfObjectIdentifier(3);
        var firstId =
            new PdfObjectIdentifier(4);
        var secondId =
            new PdfObjectIdentifier(5);

        document.BindCoreStructure(
            catalogId,
            rootId);
        document.Objects.Add(
            catalogId,
            new PdfDictionary());
        document.Objects.Add(
            rootId,
            CreatePagesDictionary(
                null,
                [branchId],
                2));
        document.Objects.Add(
            branchId,
            CreatePagesDictionary(
                rootId,
                [firstId, secondId],
                2));

        AddBoundPage(
            document,
            firstId,
            branchId);
        AddBoundPage(
            document,
            secondId,
            branchId);
        document.Trailer.SetRootObject(
            catalogId);

        return document;
    }

    private static PdfDocument CreateTwoBranchDocument()
    {
        PdfDocument document =
            PdfDocument.CreateEmpty();

        var catalogId =
            new PdfObjectIdentifier(1);
        var rootId =
            new PdfObjectIdentifier(2);
        var firstBranchId =
            new PdfObjectIdentifier(3);
        var secondBranchId =
            new PdfObjectIdentifier(4);
        var firstId =
            new PdfObjectIdentifier(5);
        var secondId =
            new PdfObjectIdentifier(6);

        document.BindCoreStructure(
            catalogId,
            rootId);
        document.Objects.Add(
            catalogId,
            new PdfDictionary());
        document.Objects.Add(
            rootId,
            CreatePagesDictionary(
                null,
                [firstBranchId, secondBranchId],
                2));
        document.Objects.Add(
            firstBranchId,
            CreatePagesDictionary(
                rootId,
                [firstId],
                1));
        document.Objects.Add(
            secondBranchId,
            CreatePagesDictionary(
                rootId,
                [secondId],
                1));

        AddBoundPage(
            document,
            firstId,
            firstBranchId);
        AddBoundPage(
            document,
            secondId,
            secondBranchId);
        document.Trailer.SetRootObject(
            catalogId);

        return document;
    }

    private static void AddBoundPage(
        PdfDocument document,
        PdfObjectIdentifier pageId,
        PdfObjectIdentifier parentId)
    {
        var page =
            new PdfPage(612, 792);
        page.BindObjectIdentifier(
            pageId);
        document.Pages.Add(
            page);
        document.Objects.Add(
            pageId,
            CreatePageDictionary(
                parentId));
    }

    private static PdfDictionary CreatePagesDictionary(
        PdfObjectIdentifier? parent,
        IReadOnlyList<PdfObjectIdentifier> kids,
        int count)
    {
        var dictionary =
            new PdfDictionary
            {
                {
                    TypeName,
                    PagesName
                },
                {
                    KidsName,
                    new PdfArray(
                        kids.Select(
                            identifier =>
                                (PdfObject)new PdfIndirectReference(
                                    identifier)))
                },
                {
                    CountName,
                    new PdfInteger(count)
                }
            };

        if (parent.HasValue)
        {
            dictionary.Set(
                ParentName,
                new PdfIndirectReference(
                    parent.Value));
        }

        return dictionary;
    }

    private static PdfDictionary CreatePageDictionary(
        PdfObjectIdentifier parent)
    {
        return new PdfDictionary
        {
            {
                TypeName,
                PageName
            },
            {
                ParentName,
                new PdfIndirectReference(
                    parent)
            },
            {
                MediaBoxName,
                new PdfArray(
                [
                    new PdfInteger(0),
                    new PdfInteger(0),
                    new PdfInteger(612),
                    new PdfInteger(792)
                ])
            }
        };
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
