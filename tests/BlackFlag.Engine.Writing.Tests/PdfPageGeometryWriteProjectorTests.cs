using BlackFlag.Engine.Document.Model;
using BlackFlag.Engine.Document.Objects;
using BlackFlag.Engine.Document.Objects.Primitives;
using BlackFlag.Engine.Document.Pages;

namespace BlackFlag.Engine.Writing.Tests;

public sealed class PdfPageGeometryWriteProjectorTests
{
    private static readonly PdfName MediaBoxName = PdfName.FromLatin1("MediaBox");
    private static readonly PdfName CropBoxName = PdfName.FromLatin1("CropBox");
    private static readonly PdfName RotateName = PdfName.FromLatin1("Rotate");
    private static readonly PdfName CustomName = PdfName.FromLatin1("FusionProPrivate");

    [Fact]
    public void CreateProjectsExplicitGeometryAndPreservesUntargetedEntry()
    {
        (PdfDocument document, PdfPage page, PdfObjectIdentifier pageId) =
            CreateBoundPage();
        PdfDictionary original = document.Objects.Get<PdfDictionary>(pageId);
        var customValue = PdfString.FromLatin1("preserve-me");
        original.Add(CustomName, customValue);

        page.SetBox(
            PdfPageBoxKind.CropBox,
            new PdfEffectiveValue<PdfPageBox>(
                new PdfPageBox(12, 24, 580, 760),
                PdfPageValueOrigin.Explicit));
        page.SetRotation(PdfPageRotation.Clockwise90);

        PdfDictionary projected = Assert.IsType<PdfDictionary>(
            Assert.Single(
                PdfPageGeometryWriteProjector.Create(document, [pageId])
                    .Projections)
                .Value);

        Assert.Equal(
            [12L, 24L, 580L, 760L],
            ReadIntegerBox(projected, CropBoxName));
        Assert.Equal(
            90,
            projected.Get<PdfInteger>(RotateName).Value);
        Assert.Same(customValue, projected.Get(CustomName));
        Assert.False(original.ContainsKey(CropBoxName));
    }

    [Fact]
    public void CreateRemovesDefaultedCropAndRotationInsteadOfMaterializingThem()
    {
        (PdfDocument document, _, PdfObjectIdentifier pageId) =
            CreateBoundPage();
        PdfDictionary original = document.Objects.Get<PdfDictionary>(pageId);
        original.Add(
            CropBoxName,
            new PdfArray(
            [
                new PdfInteger(0),
                new PdfInteger(0),
                new PdfInteger(300),
                new PdfInteger(400)
            ]));
        original.Add(RotateName, new PdfInteger(180));

        PdfDictionary projected = Assert.IsType<PdfDictionary>(
            Assert.Single(
                PdfPageGeometryWriteProjector.Create(document, [pageId])
                    .Projections)
                .Value);

        Assert.False(projected.ContainsKey(CropBoxName));
        Assert.False(projected.ContainsKey(RotateName));
        Assert.True(projected.ContainsKey(MediaBoxName));
    }

    [Fact]
    public void CreateRejectsIdentifierNotBoundToAuthoritativePage()
    {
        (PdfDocument document, _, _) = CreateBoundPage();
        var otherId = new PdfObjectIdentifier(9);
        document.Objects.Add(otherId, new PdfDictionary());

        Assert.Throws<InvalidOperationException>(
            () => PdfPageGeometryWriteProjector.Create(
                document,
                [otherId]));
    }

    [Fact]
    public void CreateRejectsNonDictionaryPageObject()
    {
        (PdfDocument document, _, PdfObjectIdentifier pageId) =
            CreateBoundPage();
        document.Objects.Set(pageId, PdfString.FromLatin1("invalid"));

        Assert.Throws<InvalidOperationException>(
            () => PdfPageGeometryWriteProjector.Create(
                document,
                [pageId]));
    }

    private static (PdfDocument Document, PdfPage Page, PdfObjectIdentifier PageId)
        CreateBoundPage()
    {
        PdfDocument document = PdfDocument.CreateEmpty();
        var rootId = new PdfObjectIdentifier(1);
        var pagesId = new PdfObjectIdentifier(2);
        var pageId = new PdfObjectIdentifier(3);

        document.BindCoreStructure(rootId, pagesId);
        document.Objects.Add(rootId, new PdfDictionary());
        document.Objects.Add(pagesId, new PdfDictionary());

        var page = new PdfPage(612, 792);
        page.BindObjectIdentifier(pageId);
        document.Pages.Add(page);
        document.Objects.Add(
            pageId,
            page.CreateDictionarySnapshot());

        return (document, page, pageId);
    }

    private static long[] ReadIntegerBox(
        PdfDictionary dictionary,
        PdfName name)
    {
        PdfArray box = dictionary.Get<PdfArray>(name);
        return
        [
            box.Get<PdfInteger>(0).Value,
            box.Get<PdfInteger>(1).Value,
            box.Get<PdfInteger>(2).Value,
            box.Get<PdfInteger>(3).Value
        ];
    }
}
