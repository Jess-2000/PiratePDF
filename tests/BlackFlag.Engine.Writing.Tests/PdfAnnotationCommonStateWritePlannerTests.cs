using System.Text;
using BlackFlag.Engine.Document.Annotations;
using BlackFlag.Engine.Document.Model;
using BlackFlag.Engine.Document.Objects;
using BlackFlag.Engine.Document.Objects.Primitives;
using BlackFlag.Engine.Document.Pages;

namespace BlackFlag.Engine.Writing.Tests;

public sealed class PdfAnnotationCommonStateWritePlannerTests
{
    private static readonly PdfName AnnotsName = PdfName.FromLatin1("Annots");
    private static readonly PdfName ContentsName = PdfName.FromLatin1("Contents");

    [Fact]
    public void IncrementalCommonStateTargetsAnnotationWithoutOwningPage()
    {
        (PdfDocument document, PdfPage page, PdfAnnotation annotation) =
            CreateDocument();

        annotation.Contents = "updated";

        PdfWritePlan basePlan =
            PdfWritePlanner.Create(
                document,
                new PdfWriteOptions
                {
                    Mode = PdfWriteMode.IncrementalUpdate,
                    CrossReferenceMode =
                        PdfCrossReferenceWriteMode.TraditionalTable
                });

        PdfIncrementalWritePlan plan =
            PdfAnnotationCommonStateIncrementalWritePlanner.Create(
                basePlan,
                CreateSource(),
                page.ObjectIdentifier!.Value,
                [annotation.ObjectIdentifier!.Value]);

        Assert.Contains(
            annotation.ObjectIdentifier.Value,
            plan.ChangedObjectIdentifiers);
        Assert.DoesNotContain(
            page.ObjectIdentifier.Value,
            plan.ChangedObjectIdentifiers);
    }

    [Fact]
    public void CommonStateProjectionDoesNotRewritePageAnnots()
    {
        (PdfDocument document, PdfPage page, PdfAnnotation annotation) =
            CreateDocument();
        PdfDictionary pageSource =
            document.Objects.Get<PdfDictionary>(
                page.ObjectIdentifier!.Value);
        PdfArray annotsSource =
            pageSource.Get<PdfArray>(AnnotsName);

        annotation.Contents = "updated";

        PdfObjectWriteProjectionSet set =
            PdfAnnotationWriteProjector.CreateAnnotationObjects(
                document,
                page.ObjectIdentifier.Value,
                [annotation.ObjectIdentifier!.Value]);

        PdfObjectWriteProjection only =
            Assert.Single(set.Projections);
        Assert.Equal(
            annotation.ObjectIdentifier.Value,
            only.Identifier);
        Assert.Same(
            pageSource,
            document.Objects.Get<PdfDictionary>(
                page.ObjectIdentifier.Value));
        Assert.Same(
            annotsSource,
            pageSource.Get<PdfArray>(AnnotsName));
    }

    [Fact]
    public void FullRewriteCommonStateUpdatesAnnotationObject()
    {
        (PdfDocument document, PdfPage page, PdfAnnotation annotation) =
            CreateDocument();
        annotation.Contents = "full rewrite";

        PdfWritePlan plan =
            PdfAnnotationCommonStateFullRewritePlanner.Create(
                PdfWritePlanner.Create(document),
                page.ObjectIdentifier!.Value,
                [annotation.ObjectIdentifier!.Value]);

        PdfDictionary projected =
            document.Objects.Get<PdfDictionary>(
                annotation.ObjectIdentifier.Value);

        Assert.Equal(
            "full rewrite",
            projected.Get<PdfString>(ContentsName).GetLatin1Text());
        Assert.Contains(
            annotation.ObjectIdentifier.Value,
            plan.ExistingObjectIdentifiers);
    }

    [Fact]
    public void AnnotationOnlyProjectionRejectsTargetFromAnotherPage()
    {
        (PdfDocument document, PdfPage page, _) =
            CreateDocument();
        var unrelated = new PdfObjectIdentifier(9);
        document.Objects.Add(unrelated, new PdfDictionary());

        Assert.Throws<InvalidOperationException>(
            () => PdfAnnotationWriteProjector.CreateAnnotationObjects(
                document,
                page.ObjectIdentifier!.Value,
                [unrelated]));
    }

    [Fact]
    public void IncrementalCommonStateRejectsEmptyTargetSet()
    {
        (PdfDocument document, PdfPage page, _) =
            CreateDocument();

        PdfWritePlan basePlan =
            PdfWritePlanner.Create(
                document,
                new PdfWriteOptions
                {
                    Mode = PdfWriteMode.IncrementalUpdate,
                    CrossReferenceMode =
                        PdfCrossReferenceWriteMode.TraditionalTable
                });

        Assert.Throws<InvalidOperationException>(
            () => PdfAnnotationCommonStateIncrementalWritePlanner.Create(
                basePlan,
                CreateSource(),
                page.ObjectIdentifier!.Value,
                []));
    }

    private static (
        PdfDocument Document,
        PdfPage Page,
        PdfAnnotation Annotation)
        CreateDocument()
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
            new PdfDictionary
            {
                {
                    PdfName.FromLatin1("Type"),
                    PdfName.FromLatin1("Annot")
                },
                {
                    PdfName.FromLatin1("Subtype"),
                    PdfName.FromLatin1("Text")
                },
                {
                    PdfName.FromLatin1("Rect"),
                    new PdfArray(
                    [
                        new PdfInteger(0),
                        new PdfInteger(0),
                        new PdfInteger(100),
                        new PdfInteger(100)
                    ])
                }
            });
        document.Trailer.SetRootObject(rootIdentifier);

        return (document, page, annotation);
    }

    private static PdfMemorySourceRevision CreateSource() =>
        new(
            Encoding.ASCII.GetBytes("%PDF-1.7\nxref\n%%EOF\n"),
            9,
            PdfCrossReferenceWriteMode.TraditionalTable);
}
