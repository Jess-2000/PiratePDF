using System.Text;
using BlackFlag.Engine.Document.Annotations;
using BlackFlag.Engine.Document.Model;
using BlackFlag.Engine.Document.Objects;
using BlackFlag.Engine.Document.Objects.Primitives;
using BlackFlag.Engine.Document.Pages;

namespace BlackFlag.Engine.Writing.Tests;

public sealed class PdfNewAnnotationWritePlannerTests
{
    private static readonly PdfName AnnotsName = PdfName.FromLatin1("Annots");
    private static readonly PdfName ParentName = PdfName.FromLatin1("Parent");

    [Fact]
    public void FullRewriteAllocatesBindsAndSerializesNewAnnotation()
    {
        (PdfDocument document, PdfPage page) =
            CreateBoundDocument(PdfVersion.Pdf17);
        var annotation =
            new PdfAnnotation(
                PdfAnnotationSubtype.Text,
                new PdfAnnotationRectangle(10, 20, 110, 220))
            {
                Contents = "Created"
            };
        page.InsertAnnotation(0, annotation);

        PdfWritePlan basePlan = PdfWritePlanner.Create(document);
        int expectedObjectNumber = basePlan.NextObjectNumber;

        PdfWritePlan plan =
            PdfNewAnnotationFullRewritePlanner.Create(
                basePlan,
                page.ObjectIdentifier.Value);

        PdfObjectIdentifier identifier =
            annotation.ObjectIdentifier.Value;

        Assert.Equal(expectedObjectNumber, identifier.ObjectNumber);
        Assert.Contains(identifier, plan.ExistingObjectIdentifiers);

        PdfDictionary dictionary =
            document.Objects.Get<PdfDictionary>(identifier);
        Assert.Equal(
            "Created",
            dictionary
                .Get<PdfString>(PdfName.FromLatin1("Contents"))
                .GetLatin1Text());

        string output =
            Encoding.Latin1.GetString(
                PdfFullRewriteWriter.Write(plan));

        Assert.Contains(
            $"{identifier.ObjectNumber} 0 obj",
            output,
            StringComparison.Ordinal);
    }

    [Fact]
    public void FullRewritePreservesExistingAnnotationAndAddsNewReference()
    {
        (PdfDocument document, PdfPage page) =
            CreateBoundDocument(PdfVersion.Pdf17);
        PdfAnnotation existing =
            AddBoundAnnotation(
                document,
                page,
                new PdfObjectIdentifier(4));

        var added =
            new PdfAnnotation(
                PdfAnnotationSubtype.Link,
                new PdfAnnotationRectangle(5, 5, 25, 25));
        page.InsertAnnotation(1, added);

        PdfWritePlan plan =
            PdfNewAnnotationFullRewritePlanner.Create(
                PdfWritePlanner.Create(document),
                page.ObjectIdentifier.Value);

        PdfDictionary projectedPage =
            document.Objects.Get<PdfDictionary>(
                page.ObjectIdentifier.Value);
        PdfArray annots =
            projectedPage.Get<PdfArray>(AnnotsName);

        Assert.Equal(
            existing.ObjectIdentifier,
            annots.Get<PdfIndirectReference>(0).Identifier);
        Assert.Equal(
            added.ObjectIdentifier,
            annots.Get<PdfIndirectReference>(1).Identifier);
        Assert.Contains(
            added.ObjectIdentifier.Value,
            plan.ExistingObjectIdentifiers);
    }

    [Fact]
    public void IncrementalTargetsNewAnnotationAndOwningPage()
    {
        (PdfDocument document, PdfPage page) =
            CreateBoundDocument(PdfVersion.Pdf17);
        PdfAnnotation existing =
            AddBoundAnnotation(
                document,
                page,
                new PdfObjectIdentifier(4));
        var annotation =
            new PdfAnnotation(
                PdfAnnotationSubtype.Highlight,
                new PdfAnnotationRectangle(1, 2, 30, 40));
        page.InsertAnnotation(1, annotation);

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
            PdfNewAnnotationIncrementalWritePlanner.Create(
                basePlan,
                CreateSource(),
                page.ObjectIdentifier.Value);

        Assert.Contains(
            annotation.ObjectIdentifier.Value,
            plan.ChangedObjectIdentifiers);
        Assert.Contains(
            page.ObjectIdentifier.Value,
            plan.ChangedObjectIdentifiers);
        Assert.DoesNotContain(
            existing.ObjectIdentifier!.Value,
            plan.ChangedObjectIdentifiers);
    }

    [Fact]
    public void IncrementalFailureLeavesAnnotationUnboundAndUnstored()
    {
        (PdfDocument document, PdfPage page) =
            CreateBoundDocument(PdfVersion.Pdf14);
        var annotation =
            new PdfAnnotation(
                PdfAnnotationSubtype.Text,
                new PdfAnnotationRectangle(0, 0, 10, 10));
        page.InsertAnnotation(0, annotation);

        PdfWritePlan basePlan =
            PdfWritePlanner.Create(
                document,
                new PdfWriteOptions
                {
                    Mode = PdfWriteMode.IncrementalUpdate,
                    CrossReferenceMode =
                        PdfCrossReferenceWriteMode.CrossReferenceStream
                });
        var expectedIdentifier =
            new PdfObjectIdentifier(basePlan.NextObjectNumber);

        Assert.Throws<NotSupportedException>(
            () => PdfNewAnnotationIncrementalWritePlanner.Create(
                basePlan,
                CreateSource(),
                page.ObjectIdentifier.Value));

        Assert.Null(annotation.ObjectIdentifier);
        Assert.False(document.Objects.Contains(expectedIdentifier));
    }

    [Fact]
    public void PlanningRejectsPageWithoutNewAnnotations()
    {
        (PdfDocument document, PdfPage page) =
            CreateBoundDocument(PdfVersion.Pdf17);

        Assert.Throws<InvalidOperationException>(
            () => PdfNewAnnotationFullRewritePlanner.Create(
                PdfWritePlanner.Create(document),
                page.ObjectIdentifier.Value));
    }

    private static (PdfDocument Document, PdfPage Page)
        CreateBoundDocument(PdfVersion version)
    {
        PdfDocument document = PdfDocument.CreateEmpty(version);
        var rootIdentifier = new PdfObjectIdentifier(1);
        var treeIdentifier = new PdfObjectIdentifier(2);
        var pageIdentifier = new PdfObjectIdentifier(3);

        document.BindCoreStructure(rootIdentifier, treeIdentifier);
        document.Objects.Add(rootIdentifier, new PdfDictionary());
        document.Objects.Add(
            treeIdentifier,
            document.Pages.CreateDictionarySnapshot());

        var page = new PdfPage(612, 792);
        page.BindObjectIdentifier(pageIdentifier);
        document.Pages.Add(page);
        document.Objects.Add(
            pageIdentifier,
            page.CreateDictionarySnapshot());
        document.Trailer.SetRootObject(rootIdentifier);

        return (document, page);
    }

    private static PdfAnnotation AddBoundAnnotation(
        PdfDocument document,
        PdfPage page,
        PdfObjectIdentifier identifier)
    {
        var annotation =
            new PdfAnnotation(
                PdfAnnotationSubtype.Text,
                new PdfAnnotationRectangle(0, 0, 100, 100));
        annotation.BindObjectIdentifier(identifier);
        page.InsertAnnotation(page.Annotations.Count, annotation);

        document.Objects.Add(
            identifier,
            new PdfDictionary
            {
                {
                    PdfName.FromLatin1("Type"),
                    PdfName.FromLatin1("Annot")
                },
                {
                    PdfName.FromLatin1("Subtype"),
                    PdfName.FromLatin1("Text")
                }
            });

        return annotation;
    }

    private static PdfMemorySourceRevision CreateSource() =>
        new(
            Encoding.ASCII.GetBytes("%PDF-1.7\nxref\n%%EOF\n"),
            9,
            PdfCrossReferenceWriteMode.TraditionalTable);
}
