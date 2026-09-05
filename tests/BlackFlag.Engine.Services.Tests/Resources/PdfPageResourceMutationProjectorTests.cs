using BlackFlag.Engine.Document.Model;
using BlackFlag.Engine.Document.Objects;
using BlackFlag.Engine.Document.Objects.Primitives;
using BlackFlag.Engine.Services.Resources;
using BlackFlag.Engine.Writing;

namespace BlackFlag.Engine.Services.Tests.Resources;

public sealed class PdfPageResourceMutationProjectorTests
{
    private static readonly PdfName ResourcesName =
        PdfName.FromLatin1("Resources");

    private static readonly PdfName FontName =
        PdfName.FromLatin1("Font");

    [Fact]
    public void InheritedResourcesUsePageCopyOnWriteAndAvoidNameCollision()
    {
        TestDocument test =
            CreateDocument(
                new PdfDictionary
                {
                    {
                        FontName,
                        new PdfDictionary
                        {
                            { PdfName.FromLatin1("F1"), new PdfIndirectReference(20) },
                            { PdfName.FromLatin1("F2"), new PdfIndirectReference(21) }
                        }
                    }
                });

        PdfPageResourceMutationProjection projection =
            PdfPageResourceMutationProjector.Create(
                PdfWritePlanner.Create(test.Document),
                test.PageIdentifier,
                FontName,
                "F1",
                new PdfIndirectReference(30));

        Assert.Equal(
            PdfResourceMutationOwnership.InheritedResourcesCopyOnWrite,
            projection.Ownership);
        Assert.Equal("F3", projection.ResourceName);

        PdfDictionary projectedPage =
            Assert.IsType<PdfDictionary>(
                projection.PageProjection.Value);

        PdfDictionary resources =
            Assert.IsType<PdfDictionary>(
                projectedPage.Get(ResourcesName));

        PdfDictionary fonts =
            Assert.IsType<PdfDictionary>(
                resources.Get(FontName));

        Assert.True(fonts.ContainsKey(PdfName.FromLatin1("F1")));
        Assert.True(fonts.ContainsKey(PdfName.FromLatin1("F2")));
        Assert.True(fonts.ContainsKey(PdfName.FromLatin1("F3")));

        PdfDictionary ancestor =
            test.Document.Objects.Get<PdfDictionary>(
                test.ParentIdentifier);

        PdfDictionary originalResources =
            Assert.IsType<PdfDictionary>(
                ancestor.Get(ResourcesName));

        PdfDictionary originalFonts =
            Assert.IsType<PdfDictionary>(
                originalResources.Get(FontName));

        Assert.False(
            originalFonts.ContainsKey(
                PdfName.FromLatin1("F3")));
    }

    [Fact]
    public void IndirectPageResourcesAreCopiedOntoPage()
    {
        TestDocument test = CreateDocument();

        var resourcesId = new PdfObjectIdentifier(40);

        test.Document.Objects.Add(
            resourcesId,
            new PdfDictionary
            {
                {
                    FontName,
                    new PdfDictionary
                    {
                        { PdfName.FromLatin1("F1"), new PdfIndirectReference(20) }
                    }
                }
            });

        PdfDictionary page =
            test.Document.Objects
                .Get<PdfDictionary>(test.PageIdentifier)
                .CreateCopy();

        page.Set(
            ResourcesName,
            new PdfIndirectReference(resourcesId));

        test.Document.Objects.Set(
            test.PageIdentifier,
            page);

        PdfPageResourceMutationProjection projection =
            PdfPageResourceMutationProjector.Create(
                PdfWritePlanner.Create(test.Document),
                test.PageIdentifier,
                FontName,
                "F2",
                new PdfIndirectReference(30));

        Assert.Equal(
            PdfResourceMutationOwnership.IndirectPageResourcesCopyOnWrite,
            projection.Ownership);

        PdfDictionary projectedPage =
            Assert.IsType<PdfDictionary>(
                projection.PageProjection.Value);

        Assert.IsType<PdfDictionary>(
            projectedPage.Get(ResourcesName));

        PdfDictionary originalFonts =
            Assert.IsType<PdfDictionary>(
                test.Document.Objects
                    .Get<PdfDictionary>(resourcesId)
                    .Get(FontName));

        Assert.False(
            originalFonts.ContainsKey(
                PdfName.FromLatin1("F2")));
    }

    [Fact]
    public void IndirectCategoryIsCopiedBeforeMutation()
    {
        TestDocument test = CreateDocument();

        var fontsId = new PdfObjectIdentifier(41);

        test.Document.Objects.Add(
            fontsId,
            new PdfDictionary
            {
                { PdfName.FromLatin1("F1"), new PdfIndirectReference(20) }
            });

        PdfDictionary page =
            test.Document.Objects
                .Get<PdfDictionary>(test.PageIdentifier)
                .CreateCopy();

        page.Set(
            ResourcesName,
            new PdfDictionary
            {
                { FontName, new PdfIndirectReference(fontsId) }
            });

        test.Document.Objects.Set(
            test.PageIdentifier,
            page);

        PdfPageResourceMutationProjection projection =
            PdfPageResourceMutationProjector.Create(
                PdfWritePlanner.Create(test.Document),
                test.PageIdentifier,
                FontName,
                "F2",
                new PdfIndirectReference(30));

        Assert.True(projection.CategoryCopiedFromIndirectObject);

        Assert.False(
            test.Document.Objects
                .Get<PdfDictionary>(fontsId)
                .ContainsKey(
                    PdfName.FromLatin1("F2")));
    }

    [Fact]
    public void MissingResourcesCreatePageLocalResources()
    {
        TestDocument test = CreateDocument();

        PdfPageResourceMutationProjection projection =
            PdfPageResourceMutationProjector.Create(
                PdfWritePlanner.Create(test.Document),
                test.PageIdentifier,
                FontName,
                "FUnicode1",
                new PdfIndirectReference(30));

        Assert.Equal(
            PdfResourceMutationOwnership.NewPageResources,
            projection.Ownership);
        Assert.Equal("FUnicode1", projection.ResourceName);
    }

    [Fact]
    public void InheritanceCycleIsRejected()
    {
        TestDocument test = CreateDocument();

        PdfDictionary parent =
            test.Document.Objects
                .Get<PdfDictionary>(test.ParentIdentifier)
                .CreateCopy();

        parent.Set(
            PdfName.FromLatin1("Parent"),
            new PdfIndirectReference(
                test.ParentIdentifier));

        test.Document.Objects.Set(
            test.ParentIdentifier,
            parent);

        Assert.Throws<InvalidOperationException>(
            () =>
                PdfPageResourceMutationProjector.Create(
                    PdfWritePlanner.Create(test.Document),
                    test.PageIdentifier,
                    FontName,
                    "F1",
                    new PdfIndirectReference(30)));
    }

    private static TestDocument CreateDocument(
        PdfDictionary inheritedResources = null)
    {
        PdfDocument document =
            PdfDocument.CreateEmpty(PdfVersion.Pdf20);

        var catalogId = new PdfObjectIdentifier(1);
        var rootPagesId = new PdfObjectIdentifier(2);
        var parentId = new PdfObjectIdentifier(3);
        var pageId = new PdfObjectIdentifier(4);

        document.BindCoreStructure(catalogId, rootPagesId);

        document.Objects.Add(
            catalogId,
            new PdfDictionary
            {
                { PdfName.FromLatin1("Type"), PdfName.FromLatin1("Catalog") },
                { PdfName.FromLatin1("Pages"), new PdfIndirectReference(rootPagesId) }
            });

        document.Objects.Add(
            rootPagesId,
            new PdfDictionary
            {
                { PdfName.FromLatin1("Type"), PdfName.FromLatin1("Pages") },
                {
                    PdfName.FromLatin1("Kids"),
                    new PdfArray([new PdfIndirectReference(parentId)])
                },
                { PdfName.FromLatin1("Count"), new PdfInteger(1) }
            });

        var parent =
            new PdfDictionary
            {
                { PdfName.FromLatin1("Type"), PdfName.FromLatin1("Pages") },
                { PdfName.FromLatin1("Parent"), new PdfIndirectReference(rootPagesId) },
                {
                    PdfName.FromLatin1("Kids"),
                    new PdfArray([new PdfIndirectReference(pageId)])
                },
                { PdfName.FromLatin1("Count"), new PdfInteger(1) }
            };

        if (inheritedResources is not null)
        {
            parent.Add(ResourcesName, inheritedResources);
        }

        document.Objects.Add(parentId, parent);

        document.Objects.Add(
            pageId,
            new PdfDictionary
            {
                { PdfName.FromLatin1("Type"), PdfName.FromLatin1("Page") },
                { PdfName.FromLatin1("Parent"), new PdfIndirectReference(parentId) },
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
            });

        return new TestDocument(
            document,
            pageId,
            parentId);
    }

    private sealed record TestDocument(
        PdfDocument Document,
        PdfObjectIdentifier PageIdentifier,
        PdfObjectIdentifier ParentIdentifier);
}
