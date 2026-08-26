using BlackFlag.Engine.Document.Model;
using BlackFlag.Engine.Document.Objects;
using BlackFlag.Engine.Document.Objects.Primitives;
using BlackFlag.Engine.Document.Pages;

namespace BlackFlag.Engine.Writing.Tests;

public sealed class PdfNestedPageTreeWriteProjectorTests
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
    private static readonly PdfName CustomName =
        PdfName.FromLatin1("PrivateNodeState");

    [Fact]
    public void SnapshotPreservesNestedTopologyAndAuthoritativeLeafOrder()
    {
        PdfDocument document =
            CreateNestedDocument();

        PdfPageTreeHierarchySnapshot snapshot =
            PdfPageTreeHierarchySnapshot.Create(
                document);

        Assert.Equal(
            new PdfObjectIdentifier(2),
            snapshot.RootIdentifier);
        Assert.Equal(
            2,
            snapshot.Nodes.Count);
        Assert.Equal(
            2,
            snapshot.Leaves.Count);

        PdfPageTreeHierarchySnapshot.Node branch =
            snapshot.Nodes.Single(
                node =>
                    node.Identifier ==
                    new PdfObjectIdentifier(3));

        Assert.Equal(
            2,
            branch.DescendantPageCount);
        Assert.Equal(
            new PdfObjectIdentifier(2),
            branch.ParentIdentifier);
        Assert.Equal(
            [
                new PdfObjectIdentifier(4),
                new PdfObjectIdentifier(5)
            ],
            branch.ChildIdentifiers);
    }

    [Fact]
    public void ProjectorRepairsCountsAndParentReferencesWithoutFlatteningKids()
    {
        PdfDocument document =
            CreateNestedDocument();

        PdfDictionary root =
            document.Objects.Get<PdfDictionary>(
                new PdfObjectIdentifier(2));
        PdfDictionary branch =
            document.Objects.Get<PdfDictionary>(
                new PdfObjectIdentifier(3));
        PdfDictionary first =
            document.Objects.Get<PdfDictionary>(
                new PdfObjectIdentifier(4));

        root.Set(
            CountName,
            new PdfInteger(99));
        branch.Set(
            CountName,
            new PdfInteger(0));
        first.Set(
            ParentName,
            new PdfIndirectReference(
                new PdfObjectIdentifier(2)));

        PdfArray rootKids =
            root.Get<PdfArray>(
                KidsName);
        PdfArray branchKids =
            branch.Get<PdfArray>(
                KidsName);

        PdfObjectWriteProjectionSet set =
            PdfNestedPageTreeWriteProjector.Create(
                document);

        PdfDictionary projectedRoot =
            Assert.IsType<PdfDictionary>(
                set.Projections.Single(
                    item =>
                        item.Identifier ==
                        new PdfObjectIdentifier(2))
                    .Value);
        PdfDictionary projectedBranch =
            Assert.IsType<PdfDictionary>(
                set.Projections.Single(
                    item =>
                        item.Identifier ==
                        new PdfObjectIdentifier(3))
                    .Value);
        PdfDictionary projectedFirst =
            Assert.IsType<PdfDictionary>(
                set.Projections.Single(
                    item =>
                        item.Identifier ==
                        new PdfObjectIdentifier(4))
                    .Value);

        Assert.Equal(
            2L,
            projectedRoot
                .Get<PdfInteger>(CountName)
                .Value);
        Assert.Equal(
            2L,
            projectedBranch
                .Get<PdfInteger>(CountName)
                .Value);
        Assert.Equal(
            new PdfObjectIdentifier(3),
            projectedFirst
                .Get<PdfIndirectReference>(ParentName)
                .Identifier);

        Assert.Same(
            rootKids,
            projectedRoot.Get<PdfArray>(KidsName));
        Assert.Same(
            branchKids,
            projectedBranch.Get<PdfArray>(KidsName));
    }

    [Fact]
    public void ProjectorPreservesUntargetedNodeEntries()
    {
        PdfDocument document =
            CreateNestedDocument();
        PdfDictionary branch =
            document.Objects.Get<PdfDictionary>(
                new PdfObjectIdentifier(3));
        var custom =
            PdfString.FromLatin1("preserve");
        branch.Set(
            CustomName,
            custom);

        PdfObjectWriteProjectionSet set =
            PdfNestedPageTreeWriteProjector.Create(
                document);
        PdfDictionary projected =
            Assert.IsType<PdfDictionary>(
                set.Projections.Single(
                    item =>
                        item.Identifier ==
                        new PdfObjectIdentifier(3))
                    .Value);

        Assert.Same(
            custom,
            projected.Get(CustomName));
        Assert.Same(
            custom,
            branch.Get(CustomName));
    }

    [Fact]
    public void SnapshotRejectsLeafOrderDifferentFromAuthoritativePages()
    {
        PdfDocument document =
            CreateNestedDocument();
        PdfDictionary branch =
            document.Objects.Get<PdfDictionary>(
                new PdfObjectIdentifier(3));

        branch.Set(
            KidsName,
            new PdfArray(
            [
                new PdfIndirectReference(
                    new PdfObjectIdentifier(5)),
                new PdfIndirectReference(
                    new PdfObjectIdentifier(4))
            ]));

        Assert.Throws<InvalidOperationException>(
            () => PdfPageTreeHierarchySnapshot.Create(
                document));
    }

    [Fact]
    public void SnapshotRejectsRepeatedPageTreeObject()
    {
        PdfDocument document =
            CreateNestedDocument();
        PdfDictionary root =
            document.Objects.Get<PdfDictionary>(
                new PdfObjectIdentifier(2));

        root.Set(
            KidsName,
            new PdfArray(
            [
                new PdfIndirectReference(
                    new PdfObjectIdentifier(3)),
                new PdfIndirectReference(
                    new PdfObjectIdentifier(3))
            ]));

        Assert.Throws<InvalidOperationException>(
            () => PdfPageTreeHierarchySnapshot.Create(
                document));
    }

    private static PdfDocument CreateNestedDocument()
    {
        PdfDocument document =
            PdfDocument.CreateEmpty();

        var rootCatalogId =
            new PdfObjectIdentifier(1);
        var rootPagesId =
            new PdfObjectIdentifier(2);
        var branchId =
            new PdfObjectIdentifier(3);
        var firstPageId =
            new PdfObjectIdentifier(4);
        var secondPageId =
            new PdfObjectIdentifier(5);

        document.BindCoreStructure(
            rootCatalogId,
            rootPagesId);
        document.Objects.Add(
            rootCatalogId,
            new PdfDictionary());

        document.Objects.Add(
            rootPagesId,
            CreatePagesDictionary(
                parent: null,
                [
                    branchId
                ],
                2));

        document.Objects.Add(
            branchId,
            CreatePagesDictionary(
                rootPagesId,
                [
                    firstPageId,
                    secondPageId
                ],
                2));

        var first =
            new PdfPage(612, 792);
        first.BindObjectIdentifier(
            firstPageId);
        document.Pages.Add(
            first);
        document.Objects.Add(
            firstPageId,
            CreatePageDictionary(
                branchId));

        var second =
            new PdfPage(612, 792);
        second.BindObjectIdentifier(
            secondPageId);
        document.Pages.Add(
            second);
        document.Objects.Add(
            secondPageId,
            CreatePageDictionary(
                branchId));

        document.Trailer.SetRootObject(
            rootCatalogId);

        return document;
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
