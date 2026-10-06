using System.Text;
using System.IO;
using BlackFlag.Engine.Document.Model;
using BlackFlag.Engine.Document.Objects;
using BlackFlag.Engine.Document.Objects.Primitives;
using BlackFlag.Engine.Services.Text;
using BlackFlag.Engine.Writing;

namespace BlackFlag.Engine.Services.Tests.Text;

public sealed partial class PdfFormPlacementCloneProjectorTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    public void FreshAndStaleBindingsRespectDirectIndirectAndInheritedResources(int layout)
    {
        var document = CreateDocument(out var pageId, out var contentId, out var formId);
        var (owner, resources, category) = ConfigureResources(document, pageId, layout);
        category.Add(PdfName.FromLatin1("X2"), new PdfIndirectReference(formId));
        var placement = CreatePlacementDescriptor(document, pageId, contentId, formId, "X1", 0, 6);
        byte[] original = GraphBytes(document);
        var projection = PdfFormPlacementCloneProjector.Create(PdfWritePlanner.Create(document), placement, "Edited"u8);
        Assert.Equal("X3", projection.ResourceName);
        Assert.Equal(original, GraphBytes(document));
        var projectedPage = Assert.IsType<PdfDictionary>(Assert.Single(
            projection.Projections.Projections, item => item.Identifier == pageId).Value);
        var projectedResources = projectedPage.Get<PdfDictionary>(PdfName.FromLatin1("Resources"));
        var projectedCategory = projectedResources.Get<PdfDictionary>(PdfName.FromLatin1("XObject"));
        Assert.Equal(formId, projectedCategory.Get<PdfIndirectReference>(PdfName.FromLatin1("X1")).Identifier);
        Assert.Equal(formId, projectedCategory.Get<PdfIndirectReference>(PdfName.FromLatin1("X2")).Identifier);
        Assert.Equal(projection.ClonedFormIdentifier,
            projectedCategory.Get<PdfIndirectReference>(PdfName.FromLatin1("X3")).Identifier);

        // Changing only the generation is still changing the binding.
        var otherFormId = new PdfObjectIdentifier(formId.ObjectNumber, 9);
        document.Objects.Add(otherFormId, new PdfStream(document.Objects.Get<PdfStream>(formId).Dictionary, "Other"u8));
        category.Set(PdfName.FromLatin1("X1"), new PdfIndirectReference(otherFormId));
        AssertRejectedUnchanged(document, placement);
        var fresh = CreatePlacementDescriptor(document, pageId, contentId, otherFormId, "X1", 0, 6);
        _ = PdfFormPlacementCloneProjector.Create(PdfWritePlanner.Create(document), fresh, "Fresh"u8);

        // An equivalent container replacement is deliberately not a proof of identity.
        owner.Set(PdfName.FromLatin1("Resources"), resources.CreateCopy());
        AssertRejectedUnchanged(document, fresh);
    }

    [Theory]
    [InlineData("page")]
    [InlineData("content")]
    [InlineData("form")]
    [InlineData("category")]
    [InlineData("contents-binding")]
    [InlineData("direct-form")]
    public void ReplacedOwnerOrObjectCannotReuseCapturedProvenance(string change)
    {
        var document = CreateDocument(out var pageId, out var contentId, out var formId);
        var (_, resources, category) = ConfigureResources(document, pageId, 0);
        var placement = CreatePlacementDescriptor(document, pageId, contentId, formId, "X1", 0, 6);
        switch (change)
        {
            case "page":
                document.Objects.Set(pageId, document.Objects.Get<PdfDictionary>(pageId).CreateCopy());
                break;
            case "content":
            case "form":
                var identifier = change == "content" ? contentId : formId;
                var stream = document.Objects.Get<PdfStream>(identifier);
                document.Objects.Set(identifier, new PdfStream(stream.Dictionary, stream.ToByteArray()));
                break;
            case "category":
                resources.Set(PdfName.FromLatin1("XObject"), category.CreateCopy());
                break;
            case "contents-binding":
                document.Objects.Get<PdfDictionary>(pageId).Set(PdfName.FromLatin1("Contents"), new PdfIndirectReference(formId));
                break;
            case "direct-form":
                category.Set(PdfName.FromLatin1("X1"), document.Objects.Get<PdfStream>(formId));
                break;
        }
        AssertRejectedUnchanged(document, placement);
    }

    [Fact]
    public void DescriptorCannotCrossDocumentsWithIdenticalObjectNumbersAndBytes()
    {
        var first = CreateDocument(out var pageId, out var contentId, out var formId);
        var placement = CreatePlacementDescriptor(first, pageId, contentId, formId, "X1", 0, 6);
        var second = CreateDocument(out _, out _, out _);
        AssertRejectedUnchanged(second, placement);
    }

    [Fact]
    public void SameObjectUnderAnotherAliasDoesNotAuthorizeChangedOperand()
    {
        var document = CreateDocument(out var pageId, out var contentId, out var formId);
        var (_, _, category) = ConfigureResources(document, pageId, 0);
        category.Add(PdfName.FromLatin1("X2"), new PdfIndirectReference(formId));
        var placement = CreatePlacementDescriptor(document, pageId, contentId, formId, "X1", 0, 6);
        document.Objects.Get<PdfStream>(contentId).ReplaceData("/X2 Do\n/X1 Do"u8);
        AssertRejectedUnchanged(document, placement);
        var fresh = CreatePlacementDescriptor(document, pageId, contentId, formId, "X2", 0, 6);
        Assert.Equal("X3", PdfFormPlacementCloneProjector.Create(PdfWritePlanner.Create(document), fresh, "New"u8).ResourceName);
    }

    [Fact]
    public void CurrentFormDataCanChangeWithoutChangingPlacementIdentity()
    {
        var document = CreateDocument(out var pageId, out var contentId, out var formId);
        var placement = CreatePlacementDescriptor(document, pageId, contentId, formId, "X1", 0, 6);
        var form = document.Objects.Get<PdfStream>(formId);
        form.ReplaceData("Current original"u8);
        form.Dictionary.Add(PdfName.FromLatin1("PrivateEntry"), new PdfInteger(71));
        var projection = PdfFormPlacementCloneProjector.Create(PdfWritePlanner.Create(document), placement, "Edited current"u8);
        var clone = Assert.IsType<PdfStream>(Assert.Single(projection.Projections.Projections,
            item => item.Identifier == projection.ClonedFormIdentifier).Value);
        Assert.Equal("Current original"u8.ToArray(), form.ToByteArray());
        Assert.Equal("Edited current"u8.ToArray(), clone.ToByteArray());
        Assert.Equal(PdfFilteredTextTestDocument.Serialize(form.Dictionary), PdfFilteredTextTestDocument.Serialize(clone.Dictionary));
    }

    private static (PdfDictionary Owner, PdfDictionary Resources, PdfDictionary Category) ConfigureResources(
        PdfDocument document, PdfObjectIdentifier pageId, int layout)
    {
        var page = document.Objects.Get<PdfDictionary>(pageId);
        var resources = page.Get<PdfDictionary>(PdfName.FromLatin1("Resources"));
        var category = resources.Get<PdfDictionary>(PdfName.FromLatin1("XObject"));
        if (layout is 2 or 3 or 5)
        {
            var categoryId = new PdfObjectIdentifier(11, 4);
            document.Objects.Add(categoryId, category);
            resources.Set(PdfName.FromLatin1("XObject"), new PdfIndirectReference(categoryId));
        }
        PdfObject resourceValue = resources;
        if (layout is 1 or 3 or 5)
        {
            var resourcesId = new PdfObjectIdentifier(12, 6);
            document.Objects.Add(resourcesId, resources);
            resourceValue = new PdfIndirectReference(resourcesId);
        }
        var owner = layout >= 4 ? document.Objects.Get<PdfDictionary>(new PdfObjectIdentifier(2)) : page;
        if (layout >= 4)
        {
            page.Remove(PdfName.FromLatin1("Resources"));
        }
        owner.Set(PdfName.FromLatin1("Resources"), resourceValue);
        return (owner, resources, category);
    }

    private static byte[] GraphBytes(PdfDocument document)
    {
        using var destination = new MemoryStream();
        foreach (var identifier in document.Objects.Identifiers)
        {
            destination.Write(Encoding.ASCII.GetBytes(identifier.ToString()));
            PdfObjectSerializer.Write(destination, document.Objects.Get(identifier));
        }
        return destination.ToArray();
    }

    private static void AssertRejectedUnchanged(PdfDocument document, PdfFormPlacementDescriptor placement)
    {
        byte[] bytes = GraphBytes(document);
        var references = document.Objects.CreateSnapshot();
        var plan = PdfWritePlanner.Create(document);
        var next = plan.CreateObjectAllocator().ReserveNext();
        Assert.Throws<InvalidOperationException>(() => PdfFormPlacementCloneProjector.Create(plan, placement, "Edited"u8));
        Assert.Equal(bytes, GraphBytes(document));
        Assert.Equal(references.Count, document.Objects.Count);
        foreach (var pair in references)
        {
            Assert.Same(pair.Value, document.Objects.Get(pair.Key));
        }
        Assert.Equal(next, plan.CreateObjectAllocator().ReserveNext());
    }
}
