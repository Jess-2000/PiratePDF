using BlackFlag.Engine.Document.Model;
using BlackFlag.Engine.Document.Objects;
using BlackFlag.Engine.Document.Objects.Primitives;
using BlackFlag.Engine.Document.Pages;

namespace BlackFlag.Engine.Writing.Tests;

public sealed class PdfNestedPageTreeStructuralProjectorTests
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
    public void InsertIntoBranchProjectsKidsCountsAndNewPageParent()
    {
        PdfDocument document =
            CreateNestedDocument();
        var newPage =
            new PdfPage(400, 500);
        var newIdentifier =
            new PdfObjectIdentifier(6);

        PdfObjectWriteProjectionSet set =
            PdfNestedPageTreeStructuralProjector.Create(
                document,
                new PdfObjectIdentifier(3),
                [
                    new PdfObjectIdentifier(4),
                    newIdentifier,
                    new PdfObjectIdentifier(5)
                ],
                new Dictionary<PdfPage, PdfObjectIdentifier>
                {
                    [newPage] = newIdentifier
                });

        PdfDictionary branch =
            Assert.IsType<PdfDictionary>(
                set.Projections.Single(
                    item =>
                        item.Identifier ==
                        new PdfObjectIdentifier(3))
                    .Value);
        PdfDictionary root =
            Assert.IsType<PdfDictionary>(
                set.Projections.Single(
                    item =>
                        item.Identifier ==
                        new PdfObjectIdentifier(2))
                    .Value);
        PdfDictionary added =
            Assert.IsType<PdfDictionary>(
                set.Projections.Single(
                    item =>
                        item.Identifier ==
                        newIdentifier)
                    .Value);

        PdfArray kids =
            branch.Get<PdfArray>(KidsName);

        Assert.Equal(
            newIdentifier,
            kids.Get<PdfIndirectReference>(1).Identifier);
        Assert.Equal(
            3L,
            branch.Get<PdfInteger>(CountName).Value);
        Assert.Equal(
            3L,
            root.Get<PdfInteger>(CountName).Value);
        Assert.Equal(
            new PdfObjectIdentifier(3),
            added.Get<PdfIndirectReference>(ParentName).Identifier);
    }

    [Fact]
    public void RemoveFromBranchProjectsCountsWithoutDeletingPage()
    {
        PdfDocument document =
            CreateNestedDocument();

        PdfObjectWriteProjectionSet set =
            PdfNestedPageTreeStructuralProjector.Create(
                document,
                new PdfObjectIdentifier(3),
                [
                    new PdfObjectIdentifier(5)
                ],
                new Dictionary<PdfPage, PdfObjectIdentifier>());

        PdfDictionary branch =
            Assert.IsType<PdfDictionary>(
                set.Projections.Single(
                    item =>
                        item.Identifier ==
                        new PdfObjectIdentifier(3))
                    .Value);
        PdfDictionary root =
            Assert.IsType<PdfDictionary>(
                set.Projections.Single(
                    item =>
                        item.Identifier ==
                        new PdfObjectIdentifier(2))
                    .Value);

        Assert.Equal(
            1L,
            branch.Get<PdfInteger>(CountName).Value);
        Assert.Equal(
            1L,
            root.Get<PdfInteger>(CountName).Value);
        Assert.True(
            document.Objects.Contains(
                new PdfObjectIdentifier(4)));
    }

    [Fact]
    public void ExistingLeafInProjectedKidsGetsCorrectParent()
    {
        PdfDocument document =
            CreateNestedDocument();

        PdfObjectWriteProjectionSet set =
            PdfNestedPageTreeStructuralProjector.Create(
                document,
                new PdfObjectIdentifier(3),
                [
                    new PdfObjectIdentifier(5),
                    new PdfObjectIdentifier(4)
                ],
                new Dictionary<PdfPage, PdfObjectIdentifier>());

        PdfDictionary first =
            Assert.IsType<PdfDictionary>(
                set.Projections.Single(
                    item =>
                        item.Identifier ==
                        new PdfObjectIdentifier(4))
                    .Value);

        Assert.Equal(
            new PdfObjectIdentifier(3),
            first.Get<PdfIndirectReference>(ParentName).Identifier);
    }

    [Fact]
    public void DuplicateChildIsRejected()
    {
        PdfDocument document =
            CreateNestedDocument();

        Assert.Throws<InvalidOperationException>(
            () => PdfNestedPageTreeStructuralProjector.Create(
                document,
                new PdfObjectIdentifier(3),
                [
                    new PdfObjectIdentifier(4),
                    new PdfObjectIdentifier(4)
                ],
                new Dictionary<PdfPage, PdfObjectIdentifier>()));
    }

    [Fact]
    public void ExistingLeafFromDifferentNodeIsRejected()
    {
        PdfDocument document =
            CreateTwoBranchDocument();

        Assert.Throws<InvalidOperationException>(
            () => PdfNestedPageTreeStructuralProjector.Create(
                document,
                new PdfObjectIdentifier(3),
                [
                    new PdfObjectIdentifier(5),
                    new PdfObjectIdentifier(6)
                ],
                new Dictionary<PdfPage, PdfObjectIdentifier>()));
    }

    private static PdfDocument CreateNestedDocument()
    {
        PdfDocument document =
            PdfDocument.CreateEmpty();

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
