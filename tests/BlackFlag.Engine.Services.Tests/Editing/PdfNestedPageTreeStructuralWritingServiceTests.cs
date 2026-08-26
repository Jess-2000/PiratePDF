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

public sealed class PdfNestedPageTreeStructuralWritingServiceTests
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

    [Fact]
    public void CrossBranchMoveIsSelectedAutomatically()
    {
        PdfDocument document =
            CreateTwoBranchDocument();
        using PdfEditingSession session =
            PdfEditingService.OpenSession(document);

        PdfEditChangeSet changeSet;
        using (var transaction = session.BeginTransaction())
        {
            _ = session.Document.MovePage(
                session.Document.Pages[0],
                1);
            changeSet = transaction.Commit();
        }

        PdfIncrementalWritePlan plan =
            PdfNestedPageTreeStructuralWritingService.CreateIncrementalPlan(
                session,
                changeSet,
                CreateIncrementalWritePlan(document),
                CreateSource());

        Assert.Contains(
            new PdfObjectIdentifier(3),
            plan.ChangedObjectIdentifiers);
        Assert.Contains(
            new PdfObjectIdentifier(4),
            plan.ChangedObjectIdentifiers);
        Assert.Contains(
            new PdfObjectIdentifier(5),
            plan.ChangedObjectIdentifiers);
        Assert.DoesNotContain(
            new PdfObjectIdentifier(2),
            plan.ChangedObjectIdentifiers);

        PdfDictionary moved =
            document.Objects.Get<PdfDictionary>(
                new PdfObjectIdentifier(5));

        Assert.Equal(
            new PdfObjectIdentifier(4),
            moved.Get<PdfIndirectReference>(ParentName).Identifier);
    }

    [Fact]
    public void SameBranchInsertUsesSingleNodeProjection()
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
            changeSet = transaction.Commit();
        }

        PdfWritePlan writePlan =
            CreateIncrementalWritePlan(document);
        int expectedNumber =
            writePlan.NextObjectNumber;

        PdfIncrementalWritePlan plan =
            PdfNestedPageTreeStructuralWritingService.CreateIncrementalPlan(
                session,
                changeSet,
                writePlan,
                CreateSource());

        PdfObjectIdentifier addedId =
            Assert.IsType<PdfObjectIdentifier>(
                added.ObjectIdentifier);

        Assert.Equal(
            expectedNumber,
            addedId.ObjectNumber);
        Assert.Contains(
            new PdfObjectIdentifier(3),
            plan.ChangedObjectIdentifiers);
        Assert.Contains(
            new PdfObjectIdentifier(2),
            plan.ChangedObjectIdentifiers);
        Assert.Contains(
            addedId,
            plan.ChangedObjectIdentifiers);
    }

    [Fact]
    public void ReplaceKeepsOriginalNestedParent()
    {
        PdfDocument document =
            CreateTwoBranchDocument();
        using PdfEditingSession session =
            PdfEditingService.OpenSession(document);

        PdfEditablePage replacement =
            session.Document.CreatePage(
                500,
                700);

        PdfEditChangeSet changeSet;
        using (var transaction = session.BeginTransaction())
        {
            _ = session.Document.ReplacePage(
                session.Document.Pages[0],
                replacement);
            changeSet = transaction.Commit();
        }

        _ = PdfNestedPageTreeStructuralWritingService.CreateFullRewritePlan(
            session,
            changeSet,
            PdfWritePlanner.Create(document));

        PdfObjectIdentifier replacementId =
            Assert.IsType<PdfObjectIdentifier>(
                replacement.ObjectIdentifier);

        PdfDictionary projected =
            document.Objects.Get<PdfDictionary>(
                replacementId);

        Assert.Equal(
            new PdfObjectIdentifier(3),
            projected.Get<PdfIndirectReference>(ParentName).Identifier);
    }

    [Fact]
    public void ThreeAffectedLeafNodesAreRejected()
    {
        PdfDocument document =
            CreateThreeBranchDocument();
        using PdfEditingSession session =
            PdfEditingService.OpenSession(document);

        PdfEditChangeSet changeSet;
        using (var transaction = session.BeginTransaction())
        {
            _ = session.Document.MovePage(
                session.Document.Pages[0],
                2);
            _ = session.Document.MovePage(
                session.Document.Pages[0],
                1);
            changeSet = transaction.Commit();
        }

        Assert.Throws<NotSupportedException>(
            () => PdfNestedPageTreeStructuralWritingService.CreateFullRewritePlan(
                session,
                changeSet,
                PdfWritePlanner.Create(document)));
    }

    private static PdfDocument CreateSingleBranchDocument()
    {
        PdfDocument document =
            PdfDocument.CreateEmpty();

        var catalogId = new PdfObjectIdentifier(1);
        var rootId = new PdfObjectIdentifier(2);
        var branchId = new PdfObjectIdentifier(3);
        var firstId = new PdfObjectIdentifier(4);
        var secondId = new PdfObjectIdentifier(5);

        document.BindCoreStructure(catalogId, rootId);
        document.Objects.Add(catalogId, new PdfDictionary());
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

        AddBoundPage(document, firstId, branchId);
        AddBoundPage(document, secondId, branchId);
        document.Trailer.SetRootObject(catalogId);
        return document;
    }

    private static PdfDocument CreateTwoBranchDocument()
    {
        PdfDocument document =
            PdfDocument.CreateEmpty();

        var catalogId = new PdfObjectIdentifier(1);
        var rootId = new PdfObjectIdentifier(2);
        var firstBranchId = new PdfObjectIdentifier(3);
        var secondBranchId = new PdfObjectIdentifier(4);
        var firstId = new PdfObjectIdentifier(5);
        var secondId = new PdfObjectIdentifier(6);

        document.BindCoreStructure(catalogId, rootId);
        document.Objects.Add(catalogId, new PdfDictionary());
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

        AddBoundPage(document, firstId, firstBranchId);
        AddBoundPage(document, secondId, secondBranchId);
        document.Trailer.SetRootObject(catalogId);
        return document;
    }

    private static PdfDocument CreateThreeBranchDocument()
    {
        PdfDocument document =
            PdfDocument.CreateEmpty();

        var catalogId = new PdfObjectIdentifier(1);
        var rootId = new PdfObjectIdentifier(2);
        var firstBranchId = new PdfObjectIdentifier(3);
        var secondBranchId = new PdfObjectIdentifier(4);
        var thirdBranchId = new PdfObjectIdentifier(5);
        var firstId = new PdfObjectIdentifier(6);
        var secondId = new PdfObjectIdentifier(7);
        var thirdId = new PdfObjectIdentifier(8);

        document.BindCoreStructure(catalogId, rootId);
        document.Objects.Add(catalogId, new PdfDictionary());
        document.Objects.Add(
            rootId,
            CreatePagesDictionary(
                null,
                [firstBranchId, secondBranchId, thirdBranchId],
                3));
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
        document.Objects.Add(
            thirdBranchId,
            CreatePagesDictionary(
                rootId,
                [thirdId],
                1));

        AddBoundPage(document, firstId, firstBranchId);
        AddBoundPage(document, secondId, secondBranchId);
        AddBoundPage(document, thirdId, thirdBranchId);
        document.Trailer.SetRootObject(catalogId);
        return document;
    }

    private static void AddBoundPage(
        PdfDocument document,
        PdfObjectIdentifier pageId,
        PdfObjectIdentifier parentId)
    {
        var page =
            new PdfPage(612, 792);
        page.BindObjectIdentifier(pageId);
        document.Pages.Add(page);
        document.Objects.Add(
            pageId,
            CreatePageDictionary(parentId));
    }

    private static PdfDictionary CreatePagesDictionary(
        PdfObjectIdentifier? parent,
        IReadOnlyList<PdfObjectIdentifier> kids,
        int count)
    {
        var dictionary =
            new PdfDictionary
            {
                { TypeName, PagesName },
                {
                    KidsName,
                    new PdfArray(
                        kids.Select(
                            identifier =>
                                (PdfObject)new PdfIndirectReference(
                                    identifier)))
                },
                { CountName, new PdfInteger(count) }
            };

        if (parent.HasValue)
        {
            dictionary.Set(
                ParentName,
                new PdfIndirectReference(parent.Value));
        }

        return dictionary;
    }

    private static PdfDictionary CreatePageDictionary(
        PdfObjectIdentifier parent)
    {
        return new PdfDictionary
        {
            { TypeName, PageName },
            {
                ParentName,
                new PdfIndirectReference(parent)
            },
            {
                PdfName.FromLatin1("MediaBox"),
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
