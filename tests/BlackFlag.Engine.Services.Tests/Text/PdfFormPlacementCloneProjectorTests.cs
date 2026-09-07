using System.Text;
using BlackFlag.Engine.Document.Model;
using BlackFlag.Engine.Document.Objects;
using BlackFlag.Engine.Document.Objects.Primitives;
using BlackFlag.Engine.Editing.Text;
using BlackFlag.Engine.Rendering.DisplayList;
using BlackFlag.Engine.Rendering.Graphics;
using BlackFlag.Engine.Rendering.Resources;
using BlackFlag.Engine.Services.Text;
using BlackFlag.Engine.Writing;
using GraphicsObjects = BlackFlag.Engine.Objects;

namespace BlackFlag.Engine.Services.Tests.Text;

public sealed class PdfFormPlacementCloneProjectorTests
{
    [Fact]
    public void FilteredPlacementClonePreservesBothFilterDictionariesAndOriginalForm()
    {
        PdfDocument document = CreateDocument(out var pageId, out var contentId, out var formId);
        PdfStream pageContent = document.Objects.Get<PdfStream>(contentId);
        PdfStream form = document.Objects.Get<PdfStream>(formId);
        MakeFlate(pageContent);
        MakeFlate(form);
        byte[] originalForm = form.ToByteArray();
        byte[] pageDictionary = PdfFilteredTextTestDocument.Serialize(pageContent.Dictionary);
        byte[] formDictionary = PdfFilteredTextTestDocument.Serialize(form.Dictionary);
        var placement = CreatePlacementDescriptor(pageId, contentId, formId, "X1", 0, 6);

        PdfFormPlacementCloneProjection projection = PdfFormPlacementCloneProjector.Create(
            PdfWritePlanner.Create(document), placement, "BT (Edited) Tj ET"u8);
        var projectedPage = Assert.IsType<PdfStream>(Assert.Single(
            projection.Projections.Projections, item => item.Identifier == contentId).Value);
        var projectedForm = Assert.IsType<PdfStream>(Assert.Single(
            projection.Projections.Projections, item => item.Identifier == projection.ClonedFormIdentifier).Value);

        Assert.Equal("/X2 Do\n/X1 Do", Encoding.ASCII.GetString(
            PdfContentStreamMutationCodec.Decode(projectedPage)));
        Assert.Equal("BT (Edited) Tj ET"u8.ToArray(), PdfContentStreamMutationCodec.Decode(projectedForm));
        Assert.Equal(originalForm, form.ToByteArray());
        Assert.Equal(pageDictionary, PdfFilteredTextTestDocument.Serialize(projectedPage.Dictionary));
        Assert.Equal(formDictionary, PdfFilteredTextTestDocument.Serialize(projectedForm.Dictionary));
    }

    [Fact]
    public void PlacementDecodeBudgetFailureLeavesObjectGraphUnchanged()
    {
        PdfDocument document = CreateDocument(out var pageId, out var contentId, out var formId);
        PdfStream pageContent = document.Objects.Get<PdfStream>(contentId);
        MakeFlate(pageContent);
        pageContent.ReplaceData(PdfFilteredTextTestDocument.Compress(new byte[1024 * 1024]));
        byte[] originalPage = pageContent.ToByteArray();
        byte[] originalForm = document.Objects.Get<PdfStream>(formId).ToByteArray();
        byte[] originalPageDictionary = PdfFilteredTextTestDocument.Serialize(
            document.Objects.Get<PdfDictionary>(pageId));
        int objectCount = document.Objects.Count;
        var placement = CreatePlacementDescriptor(pageId, contentId, formId, "X1", 0, 6);

        var exception = Assert.Throws<BlackFlag.Engine.Filters.PdfFilterDecodeException>(
            () => PdfFormPlacementCloneProjector.Create(
                PdfWritePlanner.Create(document), placement, "BT (Edited) Tj ET"u8));

        Assert.Equal(BlackFlag.Engine.Filters.PdfFilterDecodeFailure.ExpansionRatioExceeded, exception.Failure);
        Assert.Equal(objectCount, document.Objects.Count);
        Assert.Equal(originalPage, pageContent.ToByteArray());
        Assert.Equal(originalForm, document.Objects.Get<PdfStream>(formId).ToByteArray());
        Assert.Equal(originalPageDictionary, PdfFilteredTextTestDocument.Serialize(
            document.Objects.Get<PdfDictionary>(pageId)));
    }

    private static void MakeFlate(PdfStream stream)
    {
        byte[] decoded = stream.ToByteArray();
        stream.Dictionary.Set(PdfName.FromLatin1("Filter"), PdfName.FromLatin1("Fl"));
        stream.Dictionary.Set(PdfName.FromLatin1("DecodeParms"), new PdfDictionary
        {
            { PdfName.FromLatin1("Predictor"), new PdfInteger(1) }
        });
        stream.ReplaceData(PdfFilteredTextTestDocument.Compress(decoded));
    }

    [Fact]
    public void PlacementCloneCreatesNewFormAndRedirectsOnlySelectedDo()
    {
        PdfDocument document =
            CreateDocument(
                out PdfObjectIdentifier pageId,
                out PdfObjectIdentifier pageContentId,
                out PdfObjectIdentifier formId);

        var placement =
            CreatePlacementDescriptor(
                pageId,
                pageContentId,
                formId,
                "X1",
                startOffset: 0,
                endOffset: 6);

        Assert.Equal(pageId, placement.PageIdentifier);
        Assert.Equal(pageContentId, placement.ContentStreamIdentifier);
        Assert.Equal(formId, placement.FormIdentifier);
        Assert.Equal("X1", placement.ResourceName);
        Assert.Equal(0, placement.StartOffset);
        Assert.Equal(6, placement.EndOffset);

        PdfFormPlacementCloneProjection projection =
            PdfFormPlacementCloneProjector.Create(
                PdfWritePlanner.Create(document),
                placement,
                Encoding.ASCII.GetBytes("BT /F1 12 Tf (Edited) Tj ET"));

        Assert.NotEqual(
            formId,
            projection.ClonedFormIdentifier);

        Assert.Equal(
            "X2",
            projection.ResourceName);

        PdfObjectWriteProjection contentProjection =
            Assert.Single(
                projection.Projections.Projections,
                item =>
                    item.Identifier ==
                    pageContentId);

        PdfStream projectedContent =
            Assert.IsType<PdfStream>(
                contentProjection.Value);

        string content =
            Encoding.ASCII.GetString(
                projectedContent.ToByteArray());

        Assert.StartsWith(
            "/X2 Do",
            content,
            StringComparison.Ordinal);

        Assert.Contains(
            "/X1 Do",
            content,
            StringComparison.Ordinal);
    }

    [Fact]
    public void PlacementCloneLeavesOriginalFormUnchanged()
    {
        PdfDocument document =
            CreateDocument(
                out PdfObjectIdentifier pageId,
                out PdfObjectIdentifier pageContentId,
                out PdfObjectIdentifier formId);

        byte[] original =
            document.Objects
                .Get<PdfStream>(formId)
                .ToByteArray();

        var placement =
            CreatePlacementDescriptor(
                pageId,
                pageContentId,
                formId,
                "X1",
                0,
                6);

        _ = PdfFormPlacementCloneProjector.Create(
            PdfWritePlanner.Create(document),
            placement,
            Encoding.ASCII.GetBytes("BT (Changed) Tj ET"));

        Assert.Equal(
            original,
            document.Objects
                .Get<PdfStream>(formId)
                .ToByteArray());
    }

    private static PdfDocument CreateDocument(
        out PdfObjectIdentifier pageId,
        out PdfObjectIdentifier contentId,
        out PdfObjectIdentifier formId)
    {
        PdfDocument document =
            PdfDocument.CreateEmpty(
                PdfVersion.Pdf20);

        var catalogId = new PdfObjectIdentifier(1);
        var pagesId = new PdfObjectIdentifier(2);
        pageId = new PdfObjectIdentifier(3);
        contentId = new PdfObjectIdentifier(4);
        formId = new PdfObjectIdentifier(5);

        document.BindCoreStructure(
            catalogId,
            pagesId);

        document.Objects.Add(
            catalogId,
            new PdfDictionary
            {
                { PdfName.FromLatin1("Type"), PdfName.FromLatin1("Catalog") },
                { PdfName.FromLatin1("Pages"), new PdfIndirectReference(pagesId) }
            });

        document.Objects.Add(
            pagesId,
            new PdfDictionary
            {
                { PdfName.FromLatin1("Type"), PdfName.FromLatin1("Pages") },
                {
                    PdfName.FromLatin1("Kids"),
                    new PdfArray([new PdfIndirectReference(pageId)])
                },
                { PdfName.FromLatin1("Count"), new PdfInteger(1) }
            });

        document.Objects.Add(
            contentId,
            new PdfStream(
                new PdfDictionary(),
                Encoding.ASCII.GetBytes("/X1 Do\n/X1 Do")));

        document.Objects.Add(
            formId,
            new PdfStream(
                new PdfDictionary
                {
                    { PdfName.FromLatin1("Type"), PdfName.FromLatin1("XObject") },
                    { PdfName.FromLatin1("Subtype"), PdfName.FromLatin1("Form") },
                    {
                        PdfName.FromLatin1("BBox"),
                        new PdfArray(
                        [
                            new PdfInteger(0),
                            new PdfInteger(0),
                            new PdfInteger(100),
                            new PdfInteger(100)
                        ])
                    }
                },
                Encoding.ASCII.GetBytes("BT /F1 12 Tf (Original) Tj ET")));

        document.Objects.Add(
            pageId,
            new PdfDictionary
            {
                { PdfName.FromLatin1("Type"), PdfName.FromLatin1("Page") },
                { PdfName.FromLatin1("Parent"), new PdfIndirectReference(pagesId) },
                { PdfName.FromLatin1("Contents"), new PdfIndirectReference(contentId) },
                {
                    PdfName.FromLatin1("Resources"),
                    new PdfDictionary
                    {
                        {
                            PdfName.FromLatin1("XObject"),
                            new PdfDictionary
                            {
                                { PdfName.FromLatin1("X1"), new PdfIndirectReference(formId) }
                            }
                        }
                    }
                }
            });

        return document;
    }

    private static PdfFormPlacementDescriptor CreatePlacementDescriptor(
        PdfObjectIdentifier pageId,
        PdfObjectIdentifier contentId,
        PdfObjectIdentifier formId,
        string resourceName,
        long startOffset,
        long endOffset)
    {
        Assert.True(PdfGraphicsOperator.TryGet("Do", out var doOperator));

        var sourceSpan = new PdfContentSourceSpan(0, startOffset, endOffset);
        var operation = new PdfGraphicsOperation(
            doOperator,
            [new GraphicsObjects.PdfName(resourceName)],
            sourceSpan);
        var form = new PdfResolvedFormXObject(
            PdfResourceIdentity.ForIndirect(
                formId.ObjectNumber,
                formId.GenerationNumber),
            new GraphicsObjects.PdfStream(
                new GraphicsObjects.PdfDictionary([]),
                ReadOnlyMemory<byte>.Empty),
            new GraphicsObjects.PdfArray(
            [
                new GraphicsObjects.PdfInteger(0),
                new GraphicsObjects.PdfInteger(0),
                new GraphicsObjects.PdfInteger(100),
                new GraphicsObjects.PdfInteger(100)
            ]),
            matrix: null,
            new Lazy<PdfResourceScope>(() => null),
            group: null);
        var displayForm = new PdfDisplayForm(
            form,
            PdfMatrix.Identity,
            new PdfDisplayList([]),
            new PdfGraphicsStateSnapshot(),
            sourceSpan,
            markedContent: null);
        var pageSource = new PdfExistingTextDiscoverySource(
            pageId,
            PdfExistingTextSourceKind.PageContent,
            [operation],
            new PdfDisplayList([displayForm]),
            [contentId]);

        return PdfFormPlacementDescriptor.Create(pageId, pageSource, displayForm);
    }
}
