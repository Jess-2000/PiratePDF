using BlackFlag.Engine.Document.Model;
using BlackFlag.Engine.Document.Objects;
using BlackFlag.Engine.Document.Objects.Primitives;
using BlackFlag.Engine.Document.Pages;

namespace BlackFlag.Engine.Document.Tests.Model;

public sealed class PdfBackingDictionaryTests
{
    [Fact]
    public void CatalogSnapshot_ContainsTypePagesAndLanguage()
    {
        var document = PdfDocument.CreateEmpty();

        document.BindCoreStructure(
            new PdfObjectIdentifier(1),
            new PdfObjectIdentifier(2));

        document.Catalog.SetLanguage(
            "en-US");

        var dictionary =
            document.Catalog.CreateDictionarySnapshot();

        Assert.Equal(
            PdfNames.Catalog,
            dictionary.Get<PdfName>(
                PdfNames.Type));

        Assert.Equal(
            new PdfIndirectReference(2),
            dictionary.Get<PdfIndirectReference>(
                PdfNames.Pages));

        Assert.Equal(
            "en-US",
            dictionary
                .Get<PdfString>(PdfNames.Language)
                .GetLatin1Text());
    }

    [Fact]
    public void PageTreeSnapshot_ContainsBoundPageReferences()
    {
        var document = PdfDocument.CreateEmpty();

        document.BindCoreStructure(
            new PdfObjectIdentifier(1),
            new PdfObjectIdentifier(2));

        var first = new PdfPage(612, 792);
        var second = new PdfPage(595, 842);

        first.BindObjectIdentifier(
            new PdfObjectIdentifier(3));

        second.BindObjectIdentifier(
            new PdfObjectIdentifier(4));

        document.Pages.Add(first);
        document.Pages.Add(second);

        var dictionary =
            document.Pages.CreateDictionarySnapshot();

        Assert.Equal(
            2,
            dictionary
                .Get<PdfInteger>(PdfNames.Count)
                .Value);

        var kids =
            dictionary.Get<PdfArray>(
                PdfNames.Kids);

        Assert.Equal(2, kids.Count);

        Assert.Equal(
            new PdfIndirectReference(3),
            kids.Get<PdfIndirectReference>(0));

        Assert.Equal(
            new PdfIndirectReference(4),
            kids.Get<PdfIndirectReference>(1));
    }

    [Fact]
    public void PageSnapshot_ContainsMediaBoxParentAndRotation()
    {
        var document = PdfDocument.CreateEmpty();

        document.BindCoreStructure(
            new PdfObjectIdentifier(1),
            new PdfObjectIdentifier(2));

        var page = new PdfPage(
            612,
            792,
            PdfPageRotation.Clockwise90);

        page.BindObjectIdentifier(
            new PdfObjectIdentifier(3));

        document.Pages.Add(page);

        var dictionary =
            page.CreateDictionarySnapshot();

        Assert.Equal(
            PdfNames.Page,
            dictionary.Get<PdfName>(
                PdfNames.Type));

        Assert.Equal(
            new PdfIndirectReference(2),
            dictionary.Get<PdfIndirectReference>(
                PdfNames.Parent));

        Assert.Equal(
            90,
            dictionary
                .Get<PdfInteger>(PdfNames.Rotate)
                .Value);

        var mediaBox =
            dictionary.Get<PdfArray>(
                PdfNames.MediaBox);

        Assert.Equal(4, mediaBox.Count);

        Assert.Equal(
            612,
            mediaBox.Get<PdfInteger>(2).Value);

        Assert.Equal(
            792,
            mediaBox.Get<PdfInteger>(3).Value);
    }

    [Fact]
    public void PageTreeSnapshot_WithUnboundPage_Throws()
    {
        var document = PdfDocument.CreateEmpty();

        document.BindCoreStructure(
            new PdfObjectIdentifier(1),
            new PdfObjectIdentifier(2));

        document.Pages.Add(
            new PdfPage(612, 792));

        Assert.Throws<InvalidOperationException>(
            () => document.Pages
                .CreateDictionarySnapshot());
    }

    [Fact]
    public void TrailerSnapshot_ContainsStructuralEntries()
    {
        var trailer = new PdfTrailer();

        trailer.SetRootObject(
            new PdfObjectIdentifier(1));

        trailer.SetInformationObject(
            new PdfObjectIdentifier(5));

        trailer.SetPreviousCrossReferenceOffset(
            1024);

        var dictionary =
            trailer.CreateDictionarySnapshot(
                objectStoreSize: 8);

        Assert.Equal(
            8,
            dictionary
                .Get<PdfInteger>(PdfNames.Size)
                .Value);

        Assert.Equal(
            new PdfIndirectReference(1),
            dictionary.Get<PdfIndirectReference>(
                PdfNames.Root));

        Assert.Equal(
            new PdfIndirectReference(5),
            dictionary.Get<PdfIndirectReference>(
                PdfNames.Info));

        Assert.Equal(
            1024,
            dictionary
                .Get<PdfInteger>(PdfNames.Previous)
                .Value);
    }

    [Fact]
    public void PageCannotBelongToTwoPageTrees()
    {
        var firstTree = new PdfPageTree();
        var secondTree = new PdfPageTree();
        var page = new PdfPage(612, 792);

        firstTree.Add(page);

        Assert.Throws<InvalidOperationException>(
            () => secondTree.Add(page));
    }

    [Fact]
    public void RemovingPage_ClearsParentOwnership()
    {
        var firstTree = new PdfPageTree();
        var secondTree = new PdfPageTree();
        var page = new PdfPage(612, 792);

        firstTree.Add(page);

        Assert.True(
            firstTree.Remove(page));

        secondTree.Add(page);

        Assert.Same(
            page,
            secondTree[0]);
    }
}