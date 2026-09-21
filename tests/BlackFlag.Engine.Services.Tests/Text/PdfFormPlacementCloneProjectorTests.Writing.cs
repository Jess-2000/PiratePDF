using System.IO;
using BlackFlag.Engine.Document.Objects;
using BlackFlag.Engine.Document.Objects.Primitives;
using BlackFlag.Engine.Services.Text;
using BlackFlag.Engine.Writing;

namespace BlackFlag.Engine.Services.Tests.Text;

public sealed partial class PdfFormPlacementCloneProjectorTests
{
    [Theory]
    [InlineData(false, false, false)]
    [InlineData(false, false, true)]
    [InlineData(false, true, false)]
    [InlineData(false, true, true)]
    [InlineData(true, false, false)]
    [InlineData(true, false, true)]
    [InlineData(true, true, false)]
    [InlineData(true, true, true)]
    public void SuccessfulOrRejectedPlacementWritesTheAuthoritativeGraph(bool streamXref, bool filtered, bool stale)
    {
        var saved = CreateSavedPlacement(streamXref, filtered, stale);
        Assert.NotEmpty(saved.Pdf);
    }

    // Shared with supplemental acceptance, which uses the already built parser.
    public static (byte[] Pdf, int CloneNumber, byte[] Content, byte[] OriginalForm, byte[] CloneForm)
        CreateSavedPlacement(bool streamXref, bool filtered, bool stale)
    {
        var document = CreateDocument(out var pageId, out var contentId, out var formId);
        var page = document.Objects.Get<PdfDictionary>(pageId);
        page.Add(PdfName.FromLatin1("MediaBox"), new PdfArray([
            new PdfInteger(0), new PdfInteger(0), new PdfInteger(100), new PdfInteger(100)]));
        var content = document.Objects.Get<PdfStream>(contentId);
        var form = document.Objects.Get<PdfStream>(formId);
        content.ReplaceData("0 0 m 1 1 l S\n/X1 Do\n/X1 Do\nBT (tail) Tj ET"u8);
        if (filtered)
        {
            MakeFlate(content);
            MakeFlate(form);
        }
        byte[] originalForm = form.ToByteArray();
        var placement = CreatePlacementDescriptor(document, pageId, contentId, formId, "X1", 14, 20);
        var options = new PdfWriteOptions { CrossReferenceMode = streamXref
            ? PdfCrossReferenceWriteMode.CrossReferenceStream : PdfCrossReferenceWriteMode.TraditionalTable };
        byte[] cloneForm = [];
        int cloneNumber = 0;
        if (stale)
        {
            content.ReplaceData(PdfContentStreamMutationCodec.Encode(content,
                "0 0 m 1 1 l S\n0 0 m \n/X1 Do\nBT (tail) Tj ET"u8));
            AssertRejectedUnchanged(document, placement);
        }
        else
        {
            var basePlan = PdfWritePlanner.Create(document, options);
            var projection = PdfFormPlacementCloneProjector.Create(basePlan, placement, "BT (edited clone) Tj ET"u8);
            Assert.Equal("X2", projection.ResourceName);
            byte[] originalDictionary = PdfFilteredTextTestDocument.Serialize(content.Dictionary);
            var plan = PdfProjectedFullRewritePlanner.Create(basePlan, projection.Projections);
            content = document.Objects.Get<PdfStream>(contentId);
            cloneNumber = projection.ClonedFormIdentifier.ObjectNumber;
            var clone = document.Objects.Get<PdfStream>(projection.ClonedFormIdentifier);
            cloneForm = clone.ToByteArray();
            Assert.Equal("BT (edited clone) Tj ET"u8.ToArray(), PdfContentStreamMutationCodec.Decode(clone));
            Assert.Equal(originalDictionary, PdfFilteredTextTestDocument.Serialize(content.Dictionary));
            Assert.Equal(originalForm, document.Objects.Get<PdfStream>(formId).ToByteArray());
            Assert.Equal("0 0 m 1 1 l S\n/X2 Do\n/X1 Do\nBT (tail) Tj ET"u8.ToArray(),
                PdfContentStreamMutationCodec.Decode(content));
            Assert.Equal(6, plan.ExistingObjectIdentifiers.Count);
        }
        using var output = new MemoryStream();
        PdfFullRewriteWriter.Write(output, PdfWritePlanner.Create(document, options));
        return (output.ToArray(), cloneNumber, content.ToByteArray(), originalForm, cloneForm);
    }
}
