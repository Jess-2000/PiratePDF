using BlackFlag.Engine.Document.Model;
using BlackFlag.Engine.Document.Objects;
using BlackFlag.Engine.Document.Objects.Primitives;
using BlackFlag.Engine.Document.Pages;

namespace BlackFlag.Engine.Document.Tests.Model;

public sealed class PdfDocumentModelTests
{
    [Fact]
    public void CreateEmpty_ReturnsPdf17Document()
    {
        var document = PdfDocument.CreateEmpty();

        Assert.Equal(
            PdfVersion.Pdf17,
            document.Version);

        Assert.Equal(
            0,
            document.PageCount);

        Assert.Empty(document.Pages);
        Assert.Equal(0, document.Objects.Count);
        Assert.Same(
            document.Catalog.Pages,
            document.Pages);
    }

    [Fact]
    public void SetVersion_ChangesDocumentVersion()
    {
        var document = PdfDocument.CreateEmpty();

        document.SetVersion(
            PdfVersion.Pdf20);

        Assert.Equal(
            PdfVersion.Pdf20,
            document.Version);
    }

    [Fact]
    public void PageTree_AddInsertMoveAndRemove_PreservesOrder()
    {
        var pages = new PdfPageTree();

        var first = new PdfPage(612, 792);
        var second = new PdfPage(612, 792);
        var third = new PdfPage(612, 792);

        pages.Add(first);
        pages.Add(third);
        pages.Insert(1, second);

        Assert.Equal(
            [first, second, third],
            pages.ToArray());

        pages.Move(
            sourceIndex: 2,
            destinationIndex: 0);

        Assert.Equal(
            [third, first, second],
            pages.ToArray());

        var removed = pages.RemoveAt(1);

        Assert.Same(first, removed);

        Assert.Equal(
            [third, second],
            pages.ToArray());
    }

    [Fact]
    public void PageTree_AddingSamePageTwice_Throws()
    {
        var pages = new PdfPageTree();
        var page = new PdfPage(612, 792);

        pages.Add(page);

        Assert.Throws<InvalidOperationException>(
            () => pages.Add(page));
    }

    [Fact]
    public void PdfPage_WithInvalidDimension_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new PdfPage(0, 792));

        Assert.Throws<ArgumentOutOfRangeException>(
            () => new PdfPage(
                double.PositiveInfinity,
                792));
    }

    [Fact]
    public void PdfPage_RotationCanBeChanged()
    {
        var page = new PdfPage(
            612,
            792);

        page.SetRotation(
            PdfPageRotation.Clockwise90);

        Assert.Equal(
            PdfPageRotation.Clockwise90,
            page.Rotation);

        Assert.True(page.IsLandscape);
    }

    [Fact]
    public void ObjectStore_AddGetSetAndRemove_ManagesObjects()
    {
        var store = new PdfObjectStore();
        var identifier = new PdfObjectIdentifier(1);

        store.Add(
            identifier,
            new PdfInteger(10));

        Assert.Equal(
            10,
            store.Get<PdfInteger>(identifier).Value);

        store.Set(
            identifier,
            new PdfInteger(20));

        Assert.Equal(
            20,
            store.Get<PdfInteger>(identifier).Value);

        Assert.True(
            store.Remove(identifier));

        Assert.False(
            store.Contains(identifier));
    }

    [Fact]
    public void ObjectStore_AddDuplicateIdentifier_Throws()
    {
        var store = new PdfObjectStore();
        var identifier = new PdfObjectIdentifier(1);

        store.Add(
            identifier,
            PdfNull.Instance);

        Assert.Throws<InvalidOperationException>(
            () => store.Add(
                identifier,
                new PdfBoolean(true)));
    }

    [Fact]
    public void Trailer_WithEncryptionObject_IsEncrypted()
    {
        var trailer = new PdfTrailer();

        Assert.False(trailer.IsEncrypted);

        trailer.SetEncryptionObject(
            new PdfObjectIdentifier(7));

        Assert.True(trailer.IsEncrypted);
    }

    [Fact]
    public void Catalog_SetLanguage_NormalizesBlankValue()
    {
        var catalog = new PdfCatalog(
            new PdfPageTree());

        catalog.SetLanguage(" en-US ");

        Assert.Equal(
            "en-US",
            catalog.Language);

        catalog.SetLanguage(" ");

        Assert.Null(catalog.Language);
    }
}
