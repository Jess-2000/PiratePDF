using System.Text;
using BlackFlag.Engine.Document.Annotations;
using BlackFlag.Engine.Document.Model;
using BlackFlag.Engine.Document.Objects;
using BlackFlag.Engine.Document.Objects.Primitives;
using BlackFlag.Engine.Document.Pages;

namespace BlackFlag.Engine.Writing.Tests;

public sealed class PdfTargetedAnnotationWritePlannerTests
{
    private static readonly PdfName AnnotsName = PdfName.FromLatin1("Annots");

    [Fact]
    public void IncrementalTargetsSelectedAnnotationAndOwningPageOnly()
    {
        (PdfDocument document, PdfPage page, PdfAnnotation first, PdfAnnotation second) =
            CreateDocument();

        first.Contents = "unchanged";
        second.Contents = "changed";

        PdfWritePlan writePlan =
            PdfWritePlanner.Create(
                document,
                new PdfWriteOptions
                {
                    Mode = PdfWriteMode.IncrementalUpdate,
                    CrossReferenceMode =
                        PdfCrossReferenceWriteMode.TraditionalTable
                });

        PdfIncrementalWritePlan plan =
            PdfAnnotationIncrementalWritePlanner.Create(
                writePlan,
                CreateSource(),
                page.ObjectIdentifier!.Value,
                [second.ObjectIdentifier!.Value]);

        Assert.Contains(
            page.ObjectIdentifier.Value,
            plan.ChangedObjectIdentifiers);
        Assert.Contains(
            second.ObjectIdentifier.Value,
            plan.ChangedObjectIdentifiers);
        Assert.DoesNotContain(
            first.ObjectIdentifier!.Value,
            plan.ChangedObjectIdentifiers);
    }

    [Fact]
    public void FullRewriteSelectedProjectionLeavesUntargetedAnnotationObjectReferenceStable()
    {
        (PdfDocument document, PdfPage page, PdfAnnotation first, PdfAnnotation second) =
            CreateDocument();
        PdfDictionary firstSource =
            document.Objects.Get<PdfDictionary>(
                first.ObjectIdentifier!.Value);

        second.Contents = "updated";

        PdfWritePlan plan =
            PdfAnnotationFullRewritePlanner.Create(
                PdfWritePlanner.Create(document),
                page.ObjectIdentifier!.Value,
                [second.ObjectIdentifier!.Value]);

        Assert.Same(
            firstSource,
            document.Objects.Get<PdfDictionary>(
                first.ObjectIdentifier.Value));
        Assert.Contains(
            second.ObjectIdentifier.Value,
            plan.ExistingObjectIdentifiers);
    }

    [Fact]
    public void EmptyTargetSetProjectsPageReferencesOnlyForRemoval()
    {
        (PdfDocument document, PdfPage page, PdfAnnotation first, PdfAnnotation second) =
            CreateDocument();

        _ = page.RemoveAnnotation(first);

        PdfObjectWriteProjectionSet set =
            PdfAnnotationWriteProjector.Create(
                document,
                page.ObjectIdentifier!.Value,
                []);

        PdfObjectWriteProjection only =
            Assert.Single(set.Projections);
        Assert.Equal(
            page.ObjectIdentifier.Value,
            only.Identifier);

        PdfDictionary projectedPage =
            Assert.IsType<PdfDictionary>(only.Value);
        PdfArray annots =
            projectedPage.Get<PdfArray>(AnnotsName);

        Assert.Single(annots);
        Assert.Equal(
            second.ObjectIdentifier,
            annots.Get<PdfIndirectReference>(0).Identifier);
    }

    [Fact]
    public void ExplicitTargetMustBelongToPage()
    {
        (PdfDocument document, PdfPage page, _, _) =
            CreateDocument();
        var otherIdentifier = new PdfObjectIdentifier(9);
        document.Objects.Add(
            otherIdentifier,
            new PdfDictionary());

        Assert.Throws<InvalidOperationException>(
            () => PdfAnnotationWriteProjector.Create(
                document,
                page.ObjectIdentifier!.Value,
                [otherIdentifier]));
    }

    private static (
        PdfDocument Document,
        PdfPage Page,
        PdfAnnotation First,
        PdfAnnotation Second)
        CreateDocument()
    {
        PdfDocument document = PdfDocument.CreateEmpty();
        var rootIdentifier = new PdfObjectIdentifier(1);
        var treeIdentifier = new PdfObjectIdentifier(2);
        var pageIdentifier = new PdfObjectIdentifier(3);
        var firstIdentifier = new PdfObjectIdentifier(4);
        var secondIdentifier = new PdfObjectIdentifier(5);

        document.BindCoreStructure(rootIdentifier, treeIdentifier);
        document.Objects.Add(rootIdentifier, new PdfDictionary());
        document.Objects.Add(
            treeIdentifier,
            document.Pages.CreateDictionarySnapshot());

        var page = new PdfPage(612, 792);
        page.BindObjectIdentifier(pageIdentifier);
        document.Pages.Add(page);

        var first =
            new PdfAnnotation(
                PdfAnnotationSubtype.Text,
                new PdfAnnotationRectangle(0, 0, 100, 100));
        first.BindObjectIdentifier(firstIdentifier);
        page.InsertAnnotation(0, first);

        var second =
            new PdfAnnotation(
                PdfAnnotationSubtype.Link,
                new PdfAnnotationRectangle(5, 5, 50, 50));
        second.BindObjectIdentifier(secondIdentifier);
        page.InsertAnnotation(1, second);

        PdfDictionary pageDictionary =
            page.CreateDictionarySnapshot();
        pageDictionary.Set(
            AnnotsName,
            new PdfArray(
            [
                new PdfIndirectReference(firstIdentifier),
                new PdfIndirectReference(secondIdentifier)
            ]));
        document.Objects.Add(pageIdentifier, pageDictionary);
        document.Objects.Add(
            firstIdentifier,
            CreateSourceAnnotation("Text"));
        document.Objects.Add(
            secondIdentifier,
            CreateSourceAnnotation("Link"));

        document.Trailer.SetRootObject(rootIdentifier);
        return (document, page, first, second);
    }

    private static PdfDictionary CreateSourceAnnotation(
        string subtype)
    {
        return new PdfDictionary
        {
            {
                PdfName.FromLatin1("Type"),
                PdfName.FromLatin1("Annot")
            },
            {
                PdfName.FromLatin1("Subtype"),
                PdfName.FromLatin1(subtype)
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
        };
    }

    private static PdfMemorySourceRevision CreateSource() =>
        new(
            Encoding.ASCII.GetBytes("%PDF-1.7\nxref\n%%EOF\n"),
            9,
            PdfCrossReferenceWriteMode.TraditionalTable);
}
