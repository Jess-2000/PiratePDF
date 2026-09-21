using System.Text;
using BlackFlag.Engine.Document.Objects;
using BlackFlag.Engine.Document.Objects.Primitives;
using BlackFlag.Engine.Services.Text;
using BlackFlag.Engine.Writing;

namespace BlackFlag.Engine.Services.Tests.Text;

public sealed partial class PdfFormPlacementCloneProjectorTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void MalformedOrTruncatedCurrentFlateRetainsCodecFailureAndPublishesNothing(bool truncated)
    {
        var document = CreateDocument(out var pageId, out var contentId, out var formId);
        var stream = document.Objects.Get<PdfStream>(contentId);
        MakeFlate(stream);
        var placement = CreatePlacementDescriptor(document, pageId, contentId, formId, "X1", 0, 6);
        byte[] encoded = stream.ToByteArray();
        stream.ReplaceData(truncated ? encoded[..^2] : new byte[] { 1, 2, 3, 4 });
        byte[] before = GraphBytes(document);
        Assert.Throws<System.IO.InvalidDataException>(() => PdfFormPlacementCloneProjector.Create(
            PdfWritePlanner.Create(document), placement, "Edited"u8));
        Assert.Equal(before, GraphBytes(document));
    }

    [Theory]
    [InlineData("0 0 m\n/X1 Do")]
    [InlineData("0 0 m ")]
    [InlineData("q\n/X1 Do\n/X1 Do")]
    [InlineData("/X2 Do\n/X1 Do")]
    public void StaleSourceMustRejectWithoutRelocating(string changedContent)
    {
        var document = CreateDocument(out var pageId, out var contentId, out var formId);
        var placement = CreatePlacementDescriptor(document, pageId, contentId, formId, "X1", 0, 6);
        var stream = document.Objects.Get<PdfStream>(contentId);
        stream.ReplaceData(Encoding.ASCII.GetBytes(changedContent));
        var plan = PdfWritePlanner.Create(document);
        int count = document.Objects.Count;
        var next = plan.CreateObjectAllocator().ReserveNext();

        var failure = Record.Exception(() =>
        {
            var projection = PdfFormPlacementCloneProjector.Create(plan, placement, "Edited"u8);
            var projectedStream = Assert.IsType<PdfStream>(Assert.Single(
                projection.Projections.Projections, item => item.Identifier == contentId).Value);
            Assert.Fail($"Stale placement succeeded: {Encoding.ASCII.GetString(projectedStream.ToByteArray())}; " +
                $"clone {projection.ClonedFormIdentifier}, resource {projection.ResourceName}, " +
                $"projections {projection.Projections.Projections.Count}, objects {document.Objects.Count}.");
        });
        Assert.True(failure is InvalidOperationException, failure?.Message);

        Assert.Equal(Encoding.ASCII.GetBytes(changedContent), stream.ToByteArray());
        Assert.Equal(count, document.Objects.Count);
        Assert.Equal(next, plan.CreateObjectAllocator().ReserveNext());
    }

    [Fact]
    public void UnchangedSpanInsideChangedLexicalContextMustReject()
    {
        var document = CreateDocument(out var pageId, out var contentId, out var formId);
        var stream = document.Objects.Get<PdfStream>(contentId);
        stream.ReplaceData("q /X1 Do Q"u8);
        var placement = CreatePlacementDescriptor(document, pageId, contentId, formId, "X1", 2, 8);
        stream.ReplaceData("( /X1 Do )"u8);
        Assert.Throws<InvalidOperationException>(() => PdfFormPlacementCloneProjector.Create(
            PdfWritePlanner.Create(document), placement, "Edited"u8));
    }

    [Fact]
    public void ChangedBindingWithIdenticalBytesMustReject()
    {
        var document = CreateDocument(out var pageId, out var contentId, out var formId);
        var placement = CreatePlacementDescriptor(document, pageId, contentId, formId, "X1", 0, 6);
        var otherFormId = new PdfObjectIdentifier(6, 7);
        document.Objects.Add(otherFormId, new PdfStream(
            document.Objects.Get<PdfStream>(formId).Dictionary, "Other"u8));
        var page = document.Objects.Get<PdfDictionary>(pageId);
        var resources = Assert.IsType<PdfDictionary>(page[PdfName.FromLatin1("Resources")]);
        var xobjects = Assert.IsType<PdfDictionary>(resources[PdfName.FromLatin1("XObject")]);
        xobjects.Set(PdfName.FromLatin1("X1"), new PdfIndirectReference(otherFormId));

        Assert.Throws<InvalidOperationException>(() => PdfFormPlacementCloneProjector.Create(
            PdfWritePlanner.Create(document), placement, "Edited"u8));
    }

    [Fact]
    public void WrongPageCannotBorrowAnotherOwnersPlacement()
    {
        var document = CreateDocument(out var pageId, out var contentId, out var formId);
        var otherPageId = new PdfObjectIdentifier(6);
        document.Objects.Add(otherPageId, document.Objects.Get<PdfDictionary>(pageId).CreateCopy());
        Assert.Throws<InvalidOperationException>(() =>
        {
            var placement = CreatePlacementDescriptor(document, otherPageId, contentId, formId, "X1", 0, 6,
                sourceOwner: pageId);
            _ = PdfFormPlacementCloneProjector.Create(PdfWritePlanner.Create(document), placement, "Edited"u8);
        });
    }
}
