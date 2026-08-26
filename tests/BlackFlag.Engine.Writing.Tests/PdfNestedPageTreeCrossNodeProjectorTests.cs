using BlackFlag.Engine.Document.Model;
using BlackFlag.Engine.Document.Objects;
using BlackFlag.Engine.Document.Objects.Primitives;
using BlackFlag.Engine.Document.Pages;

namespace BlackFlag.Engine.Writing.Tests;

public sealed class PdfNestedPageTreeCrossNodeProjectorTests
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
    public void CrossNodeMoveReparentsPageAndKeepsSharedRootCountStable()
    {
        PdfDocument document =
            CreateTwoBranchDocument();

        PdfObjectWriteProjectionSet set =
            PdfNestedPageTreeCrossNodeProjector.Create(
                document,
                new PdfObjectIdentifier(3),
                [],
                new PdfObjectIdentifier(4),
                [
                    new PdfObjectIdentifier(6),
                    new PdfObjectIdentifier(5)
                ],
                new Dictionary<PdfPage, PdfObjectIdentifier>());

        PdfDictionary source =
            GetProjection(
                set,
                new PdfObjectIdentifier(3));
        PdfDictionary destination =
            GetProjection(
                set,
                new PdfObjectIdentifier(4));
        PdfDictionary moved =
            GetProjection(
                set,
                new PdfObjectIdentifier(5));

        Assert.Empty(
            source.Get<PdfArray>(KidsName));
        Assert.Equal(
            0L,
            source.Get<PdfInteger>(CountName).Value);
        Assert.Equal(
            2L,
            destination.Get<PdfInteger>(CountName).Value);
        Assert.Equal(
            new PdfObjectIdentifier(4),
            moved.Get<PdfIndirectReference>(ParentName).Identifier);

        Assert.DoesNotContain(
            set.Projections,
            item =>
                item.Identifier ==
                new PdfObjectIdentifier(2));
    }

    [Fact]
    public void CrossNodeReplacementWithPendingPagePreservesTotalCount()
    {
        PdfDocument document =
            CreateTwoBranchDocument();
        var replacement =
            new PdfPage(500, 700);
        var replacementId =
            new PdfObjectIdentifier(7);

        PdfObjectWriteProjectionSet set =
            PdfNestedPageTreeCrossNodeProjector.Create(
                document,
                new PdfObjectIdentifier(3),
                [],
                new PdfObjectIdentifier(4),
                [
                    replacementId
                ],
                new Dictionary<PdfPage, PdfObjectIdentifier>
                {
                    [replacement] = replacementId
                });

        PdfDictionary destination =
            GetProjection(
                set,
                new PdfObjectIdentifier(4));
        PdfDictionary projectedReplacement =
            GetProjection(
                set,
                replacementId);

        Assert.Equal(
            1L,
            destination.Get<PdfInteger>(CountName).Value);
        Assert.Equal(
            replacementId,
            destination
                .Get<PdfArray>(KidsName)
                .Get<PdfIndirectReference>(0)
                .Identifier);
        Assert.Equal(
            new PdfObjectIdentifier(4),
            projectedReplacement
                .Get<PdfIndirectReference>(ParentName)
                .Identifier);

        Assert.True(
            document.Objects.Contains(
                new PdfObjectIdentifier(5)));
        Assert.True(
            document.Objects.Contains(
                new PdfObjectIdentifier(6)));
    }

    [Fact]
    public void UnequalNetDeltaUpdatesSharedRootOnce()
    {
        PdfDocument document =
            CreateTwoBranchDocument();
        var added =
            new PdfPage(300, 400);
        var addedId =
            new PdfObjectIdentifier(7);

        PdfObjectWriteProjectionSet set =
            PdfNestedPageTreeCrossNodeProjector.Create(
                document,
                new PdfObjectIdentifier(3),
                [],
                new PdfObjectIdentifier(4),
                [
                    new PdfObjectIdentifier(6),
                    new PdfObjectIdentifier(5),
                    addedId
                ],
                new Dictionary<PdfPage, PdfObjectIdentifier>
                {
                    [added] = addedId
                });

        PdfDictionary root =
            GetProjection(
                set,
                new PdfObjectIdentifier(2));

        Assert.Equal(
            3L,
            root.Get<PdfInteger>(CountName).Value);
        Assert.Single(set.Projections, item =>
                    item.Identifier ==
                    new PdfObjectIdentifier(2));
    }

    [Fact]
    public void MovingPagesNodeAcrossBranchesIsRejected()
    {
        PdfDocument document =
            CreateNestedBranchDocument();

        Assert.Throws<InvalidOperationException>(
            () => PdfNestedPageTreeCrossNodeProjector.Create(
                document,
                new PdfObjectIdentifier(3),
                [],
                new PdfObjectIdentifier(4),
                [
                    new PdfObjectIdentifier(7),
                    new PdfObjectIdentifier(5)
                ],
                new Dictionary<PdfPage, PdfObjectIdentifier>()));
    }

    [Fact]
    public void SameNodeProjectionIsRejected()
    {
        PdfDocument document =
            CreateTwoBranchDocument();

        Assert.Throws<ArgumentException>(
            () => PdfNestedPageTreeCrossNodeProjector.Create(
                document,
                new PdfObjectIdentifier(3),
                [
                    new PdfObjectIdentifier(5)
                ],
                new PdfObjectIdentifier(3),
                [
                    new PdfObjectIdentifier(5)
                ],
                new Dictionary<PdfPage, PdfObjectIdentifier>()));
    }

    private static PdfDictionary GetProjection(
        PdfObjectWriteProjectionSet set,
        PdfObjectIdentifier identifier)
    {
        return Assert.IsType<PdfDictionary>(
            set.Projections.Single(
                item =>
                    item.Identifier == identifier)
            .Value);
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
        var firstPageId =
            new PdfObjectIdentifier(5);
        var secondPageId =
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
                [firstPageId],
                1));
        document.Objects.Add(
            secondBranchId,
            CreatePagesDictionary(
                rootId,
                [secondPageId],
                1));

        AddBoundPage(
            document,
            firstPageId,
            firstBranchId);
        AddBoundPage(
            document,
            secondPageId,
            secondBranchId);

        document.Trailer.SetRootObject(
            catalogId);

        return document;
    }

    private static PdfDocument CreateNestedBranchDocument()
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
        var childBranchId =
            new PdfObjectIdentifier(5);
        var firstPageId =
            new PdfObjectIdentifier(6);
        var secondPageId =
            new PdfObjectIdentifier(7);

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
                [childBranchId],
                1));
        document.Objects.Add(
            childBranchId,
            CreatePagesDictionary(
                firstBranchId,
                [firstPageId],
                1));
        document.Objects.Add(
            secondBranchId,
            CreatePagesDictionary(
                rootId,
                [secondPageId],
                1));

        AddBoundPage(
            document,
            firstPageId,
            childBranchId);
        AddBoundPage(
            document,
            secondPageId,
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
}
