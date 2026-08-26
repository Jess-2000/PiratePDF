using BlackFlag.Engine.Document.Annotations;
using BlackFlag.Engine.Document.Model;
using BlackFlag.Engine.Document.Objects;
using BlackFlag.Engine.Document.Objects.Primitives;
using BlackFlag.Engine.Document.Pages;

namespace BlackFlag.Engine.Writing.Tests;

public sealed class PdfAnnotationWriteProjectorTests
{
    private static readonly PdfName AnnotsName = PdfName.FromLatin1("Annots");
    private static readonly PdfName RectName = PdfName.FromLatin1("Rect");
    private static readonly PdfName ContentsName = PdfName.FromLatin1("Contents");
    private static readonly PdfName FlagsName = PdfName.FromLatin1("F");
    private static readonly PdfName CustomName = PdfName.FromLatin1("FusionProPrivate");

    [Fact]
    public void CreateProjectsCommonStateAndPreservesUntargetedEntry()
    {
        (PdfDocument document, PdfPage page, PdfAnnotation annotation) =
            CreateBoundDocument();

        annotation.Rectangle =
            new PdfAnnotationRectangle(10, 20, 110, 220);
        annotation.Contents = "Updated";
        annotation.Flags =
            PdfAnnotationOptions.Print;

        PdfObjectIdentifier annotationIdentifier =
            annotation.ObjectIdentifier!.Value;
        PdfDictionary source =
            document.Objects.Get<PdfDictionary>(
                annotationIdentifier);
        var custom = PdfString.FromLatin1("preserve");
        source.Add(CustomName, custom);

        PdfObjectWriteProjectionSet set =
            PdfAnnotationWriteProjector.Create(
                document,
                page.ObjectIdentifier!.Value);

        PdfDictionary projected =
            Assert.IsType<PdfDictionary>(
                set.Projections.Single(
                    item => item.Identifier == annotationIdentifier)
                    .Value);
        PdfArray rectangle =
            projected.Get<PdfArray>(RectName);

        Assert.Equal(10, rectangle.Get<PdfInteger>(0).Value);
        Assert.Equal(220, rectangle.Get<PdfInteger>(3).Value);
        Assert.Equal(
            "Updated",
            projected.Get<PdfString>(ContentsName).GetLatin1Text());
        Assert.Equal(
            (int)PdfAnnotationOptions.Print,
            projected.Get<PdfInteger>(FlagsName).Value);
        Assert.Same(custom, projected.Get(CustomName));
        Assert.Same(custom, source.Get(CustomName));
    }

    [Fact]
    public void CreateProjectsPageAnnotationReferencesInAuthoritativeOrder()
    {
        (PdfDocument document, PdfPage page, PdfAnnotation first) =
            CreateBoundDocument();
        var second =
            new PdfAnnotation(
                PdfAnnotationSubtype.Link,
                new PdfAnnotationRectangle(5, 5, 25, 25));
        var secondIdentifier = new PdfObjectIdentifier(5);
        second.BindObjectIdentifier(secondIdentifier);
        page.InsertAnnotation(1, second);
        document.Objects.Add(
            secondIdentifier,
            CreateSourceAnnotation("Link"));

        PdfObjectWriteProjectionSet set =
            PdfAnnotationWriteProjector.Create(
                document,
                page.ObjectIdentifier!.Value);
        PdfDictionary projectedPage =
            Assert.IsType<PdfDictionary>(
                set.Projections.Single(
                    item => item.Identifier ==
                        page.ObjectIdentifier!.Value)
                    .Value);
        PdfArray annots =
            projectedPage.Get<PdfArray>(AnnotsName);

        Assert.Equal(
            first.ObjectIdentifier,
            annots.Get<PdfIndirectReference>(0).Identifier);
        Assert.Equal(
            secondIdentifier,
            annots.Get<PdfIndirectReference>(1).Identifier);
    }

    [Fact]
    public void CreateRemovesPageAnnotsWhenAuthoritativeCollectionIsEmpty()
    {
        (PdfDocument document, PdfPage page, PdfAnnotation annotation) =
            CreateBoundDocument();
        _ = page.RemoveAnnotation(annotation);

        PdfDictionary sourcePage =
            document.Objects.Get<PdfDictionary>(
                page.ObjectIdentifier!.Value);
        Assert.True(sourcePage.ContainsKey(AnnotsName));

        PdfObjectWriteProjectionSet set =
            PdfAnnotationWriteProjector.Create(
                document,
                page.ObjectIdentifier.Value);
        PdfDictionary projectedPage =
            Assert.IsType<PdfDictionary>(
                Assert.Single(set.Projections).Value);

        Assert.False(projectedPage.ContainsKey(AnnotsName));
        Assert.True(sourcePage.ContainsKey(AnnotsName));
    }

    [Fact]
    public void CreateRejectsUnboundAnnotation()
    {
        (PdfDocument document, PdfPage page, _) =
            CreateBoundDocument();
        var unbound =
            new PdfAnnotation(
                PdfAnnotationSubtype.Text,
                new PdfAnnotationRectangle(1, 1, 10, 10));
        page.InsertAnnotation(1, unbound);

        Assert.Throws<InvalidOperationException>(
            () => PdfAnnotationWriteProjector.Create(
                document,
                page.ObjectIdentifier!.Value));
    }

    [Fact]
    public void CreateRejectsMissingAnnotationBackingObject()
    {
        (PdfDocument document, PdfPage page, PdfAnnotation annotation) =
            CreateBoundDocument();
        PdfObjectIdentifier identifier =
            annotation.ObjectIdentifier!.Value;
        _ = document.Objects.Remove(identifier);

        Assert.Throws<InvalidOperationException>(
            () => PdfAnnotationWriteProjector.Create(
                document,
                page.ObjectIdentifier!.Value));
    }

    private static (
        PdfDocument Document,
        PdfPage Page,
        PdfAnnotation Annotation)
        CreateBoundDocument()
    {
        PdfDocument document = PdfDocument.CreateEmpty();
        var rootIdentifier = new PdfObjectIdentifier(1);
        var treeIdentifier = new PdfObjectIdentifier(2);
        var pageIdentifier = new PdfObjectIdentifier(3);
        var annotationIdentifier = new PdfObjectIdentifier(4);

        document.BindCoreStructure(rootIdentifier, treeIdentifier);
        document.Objects.Add(rootIdentifier, new PdfDictionary());
        document.Objects.Add(
            treeIdentifier,
            document.Pages.CreateDictionarySnapshot());

        var page = new PdfPage(612, 792);
        page.BindObjectIdentifier(pageIdentifier);
        document.Pages.Add(page);

        var annotation =
            new PdfAnnotation(
                PdfAnnotationSubtype.Text,
                new PdfAnnotationRectangle(0, 0, 100, 100));
        annotation.BindObjectIdentifier(annotationIdentifier);
        page.InsertAnnotation(0, annotation);

        PdfDictionary pageDictionary =
            page.CreateDictionarySnapshot();
        pageDictionary.Set(
            AnnotsName,
            new PdfArray(
            [
                new PdfIndirectReference(annotationIdentifier)
            ]));
        document.Objects.Add(pageIdentifier, pageDictionary);
        document.Objects.Add(
            annotationIdentifier,
            CreateSourceAnnotation("Text"));

        return (document, page, annotation);
    }

    private static PdfDictionary CreateSourceAnnotation(
        string subtype)
    {
        return new PdfDictionary
        {
            { PdfName.FromLatin1("Type"), PdfName.FromLatin1("Annot") },
            { PdfName.FromLatin1("Subtype"), PdfName.FromLatin1(subtype) },
            {
                RectName,
                new PdfArray(
                [
                    new PdfInteger(0),
                    new PdfInteger(0),
                    new PdfInteger(100),
                    new PdfInteger(100)
                ])
            }
        };
    }
}
