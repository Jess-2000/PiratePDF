using System.Text;
using BlackFlag.Engine.Document.Objects;
using BlackFlag.Engine.Document.Objects.Primitives;
using BlackFlag.Engine.Editing.Text;
using BlackFlag.Engine.Services.Text;
using BlackFlag.Engine.Writing;

namespace BlackFlag.Engine.Services.Tests.Text;

public sealed partial class PdfFormPlacementCloneProjectorTests
{
    [Theory]
    [InlineData("/X1 Do", "X1")]
    [InlineData("/X1    Do", "X1")]
    [InlineData("/X1% comment\r\nDo", "X1")]
    [InlineData("/X#31\tDo", "X1")]
    [InlineData("/X#20A Do", "X A")]
    [InlineData("/X#2fA Do", "X/A")]
    public void FreshRepresentationPreservesExactPrefixSuffixAndOtherPlacement(string operation, string name)
    {
        const string Prefix = "0 0 m 1 1 l S\nq\n";
        const string Suffix = "\nQ\n/X1 Do\nBT (unaffected) Tj ET";
        var document = CreateDocument(out var pageId, out var contentId, out var formId);
        var (_, _, category) = ConfigureResources(document, pageId, 0);
        category.Set(PdfName.FromLatin1(name), new PdfIndirectReference(formId));
        document.Objects.Get<PdfStream>(contentId).ReplaceData(Encoding.ASCII.GetBytes(Prefix + operation + Suffix));
        var placement = CreatePlacementDescriptor(document, pageId, contentId, formId, name, Prefix.Length, Prefix.Length + operation.Length);
        var projection = PdfFormPlacementCloneProjector.Create(PdfWritePlanner.Create(document), placement, "Edited"u8, "Clone/A");
        var content = Assert.IsType<PdfStream>(Assert.Single(projection.Projections.Projections, item => item.Identifier == contentId).Value);
        Assert.Equal(Prefix + "/Clone#2FA Do" + Suffix, Encoding.ASCII.GetString(content.ToByteArray()));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(7)]
    public void AdjacentOperationsRedirectOnlySelectedOccurrence(int start)
    {
        var document = CreateDocument(out var pageId, out var contentId, out var formId);
        document.Objects.Get<PdfStream>(contentId).ReplaceData("/X1 Do /X1 Do"u8);
        var placement = CreatePlacementDescriptor(document, pageId, contentId, formId, "X1", start, start + 6);
        var projection = PdfFormPlacementCloneProjector.Create(PdfWritePlanner.Create(document), placement, "Edited"u8);
        var content = Assert.IsType<PdfStream>(Assert.Single(projection.Projections.Projections, item => item.Identifier == contentId).Value);
        Assert.Equal(start == 0 ? "/X2 Do /X1 Do" : "/X1 Do /X2 Do", Encoding.ASCII.GetString(content.ToByteArray()));
    }

    [Fact]
    public void ShiftedPlacementRequiresFreshDiscoveryAtItsNewOffset()
    {
        var document = CreateDocument(out var pageId, out var contentId, out var formId);
        var old = CreatePlacementDescriptor(document, pageId, contentId, formId, "X1", 0, 6);
        document.Objects.Get<PdfStream>(contentId).ReplaceData("0 0 m\n/X1 Do\n/X1 Do"u8);
        AssertRejectedUnchanged(document, old);
        var fresh = CreatePlacementDescriptor(document, pageId, contentId, formId, "X1", 6, 12);
        var projection = PdfFormPlacementCloneProjector.Create(PdfWritePlanner.Create(document), fresh, "Edited"u8);
        var content = Assert.IsType<PdfStream>(Assert.Single(projection.Projections.Projections, item => item.Identifier == contentId).Value);
        Assert.Equal("0 0 m\n/X2 Do\n/X1 Do", Encoding.ASCII.GetString(content.ToByteArray()));
    }

    [Theory]
    [InlineData(0L, 0L)]
    [InlineData(0L, 7L)]
    [InlineData(6L, 12L)]
    [InlineData(13L, 14L)]
    [InlineData(2147483647L, 2147483653L)]
    [InlineData(0L, 9223372036854775807L)]
    public void HostileOrNonCanonicalSpansRejectBeforeProjection(long start, long end)
    {
        var document = CreateDocument(out var pageId, out var contentId, out var formId);
        byte[] before = GraphBytes(document);
        Assert.Throws<InvalidOperationException>(() => CreatePlacementDescriptor(document, pageId, contentId, formId, "X1", start, end));
        Assert.Equal(before, GraphBytes(document));
    }

    [Theory]
    [InlineData(-1L, 6L)]
    [InlineData(6L, 5L)]
    public void StructurallyInvalidSpanRetainsArgumentFailure(long start, long end)
    {
        var document = CreateDocument(out var pageId, out var contentId, out var formId);
        Assert.Throws<ArgumentOutOfRangeException>(() => CreatePlacementDescriptor(document, pageId, contentId, formId, "X1", start, end));
    }

    [Theory]
    [InlineData("/X1 XX", 0, 6)]
    [InlineData("/X1 Doo", 0, 6)]
    [InlineData("(/X1 Do)", 1, 7)]
    [InlineData("%/X1 Do\n", 1, 7)]
    [InlineData("[/X1 Do]", 1, 7)]
    [InlineData("<< /X1 Do >>", 3, 9)]
    [InlineData("1 /X1 Do", 2, 8)]
    [InlineData("/X#GG Do", 0, 8)]
    [InlineData("/X1%Do", 0, 6)]
    public void CallerConstructedCanonicalOperationsCannotAuthorizeMalformedOrEmbeddedTarget(string bytes, int start, int end)
    {
        var document = CreateDocument(out var pageId, out var contentId, out var formId);
        document.Objects.Get<PdfStream>(contentId).ReplaceData(Encoding.ASCII.GetBytes(bytes));
        Assert.Throws<InvalidOperationException>(() => CreatePlacementDescriptor(document, pageId, contentId, formId, "X1", start, end));
    }

    [Fact]
    public void LegacyDescriptorWithoutDocumentCannotAuthorizeProjection()
    {
        var document = CreateDocument(out var pageId, out var contentId, out var formId);
        var placement = CreatePlacementDescriptor(document, pageId, contentId, formId, "X1", 0, 6, legacy: true);
        AssertRejectedUnchanged(document, placement);
    }

    [Fact]
    public void FormOwnedDiscoveryRemainsOutsidePlacementCloneSupport()
    {
        var document = CreateDocument(out _, out var contentId, out var formId);
        Assert.Throws<ArgumentException>(() => CreatePlacementDescriptor(document, formId, contentId, formId,
            "X1", 0, 6, sourceKind: PdfExistingTextSourceKind.FormXObject));
    }

    [Fact]
    public void InlineImagePrefixFailsClosedWithoutGuessingBinaryImageBoundaries()
    {
        var document = CreateDocument(out var pageId, out var contentId, out var formId);
        const string Content = "BI /W 1 /H 1 /BPC 8 /CS /G ID x EI\n/X1 Do";
        document.Objects.Get<PdfStream>(contentId).ReplaceData(Encoding.ASCII.GetBytes(Content));
        Assert.Throws<NotSupportedException>(() => CreatePlacementDescriptor(document, pageId, contentId, formId,
            "X1", Content.Length - 6, Content.Length));
    }

    [Fact]
    public void StreamArrayIndexCannotSilentlyMoveToAnotherPhysicalStream()
    {
        var document = CreateDocument(out var pageId, out var contentId, out var formId);
        var secondId = new PdfObjectIdentifier(7, 2);
        document.Objects.Add(secondId, new PdfStream(new PdfDictionary(), "/X1 Do"u8));
        var contents = new PdfArray([new PdfIndirectReference(contentId), new PdfIndirectReference(secondId)]);
        document.Objects.Get<PdfDictionary>(pageId).Set(PdfName.FromLatin1("Contents"), contents);
        var placement = CreatePlacementDescriptor(document, pageId, secondId, formId, "X1", 0, 6,
            streamIndex: 1, streamIdentifiers: [contentId, secondId]);
        var projection = PdfFormPlacementCloneProjector.Create(PdfWritePlanner.Create(document), placement, "Edited"u8);
        Assert.DoesNotContain(projection.Projections.Projections, item => item.Identifier == contentId);
        contents.Set(1, new PdfIndirectReference(contentId));
        AssertRejectedUnchanged(document, placement);
    }
}
