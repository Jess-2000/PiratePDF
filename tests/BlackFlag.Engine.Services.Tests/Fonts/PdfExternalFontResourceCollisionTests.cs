using BlackFlag.Engine.Document.Objects;
using BlackFlag.Engine.Document.Objects.Primitives;
using BlackFlag.Engine.Services.Fonts;
using BlackFlag.Engine.Services.Tests.Text;
using BlackFlag.Engine.Writing;

namespace BlackFlag.Engine.Services.Tests.Fonts;

public sealed class PdfExternalFontResourceCollisionTests
{
    public static TheoryData<string, string, bool> Cases
    {
        get
        {
            var cases = new TheoryData<string, string, bool>();
            foreach (string projector in new[] { "full", "subset", "type0" })
            {
                foreach (string ownership in new[] { "direct", "indirect", "inherited", "indirect-category" })
                {
                    cases.Add(projector, ownership, false);
                    cases.Add(projector, ownership, true);
                }
            }

            return cases;
        }
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public void ReturnedNameResolvesToProjectedFontWithoutChangingExistingResources(
        string projector, string ownership, bool collision)
    {
        using var context = new PdfFontCollisionTestDocument(ownership, collision);
        var projection = Create(context, projector);
        string expected = collision ? "FExt2" : "FExt1";

        Assert.Equal(expected, projection.Name);
        context.AssertOriginalGraph();
        projection.Objects.ApplyTo(context.Document.Objects);
        AssertMapping(context, expected, projection.FontId);
        if (collision)
        {
            AssertMapping(context, "FExt1", PdfFontCollisionTestDocument.OldFontId);
        }

        context.AssertUnrelatedObjectsUnchanged();
        Assert.Equal(0, projection.FontId.GenerationNumber);
    }

    [Theory]
    [InlineData("full", "direct")]
    [InlineData("full", "indirect")]
    [InlineData("full", "inherited")]
    [InlineData("subset", "direct")]
    [InlineData("subset", "indirect")]
    [InlineData("subset", "inherited")]
    [InlineData("type0", "direct")]
    [InlineData("type0", "indirect")]
    [InlineData("type0", "inherited")]
    public void SequentialAllocationsAreDeterministicAndRetainEveryMapping(string projector, string ownership)
    {
        using var context = new PdfFontCollisionTestDocument(ownership);
        var first = Create(context, projector);
        first.Objects.ApplyTo(context.Document.Objects);
        var second = Create(context, projector);
        second.Objects.ApplyTo(context.Document.Objects);

        Assert.Equal("FExt2", first.Name);
        Assert.Equal("FExt3", second.Name);
        Assert.NotEqual(first.FontId, second.FontId);
        AssertMapping(context, "FExt1", PdfFontCollisionTestDocument.OldFontId);
        AssertMapping(context, first.Name, first.FontId);
        AssertMapping(context, second.Name, second.FontId);
        context.AssertUnrelatedObjectsUnchanged();
    }

    internal static void AssertMapping(PdfFontCollisionTestDocument context, string name, PdfObjectIdentifier id)
    {
        PdfDictionary page = context.Document.Objects.Get<PdfDictionary>(PdfFilteredTextTestDocument.PageId);
        PdfDictionary resources = page.Get<PdfDictionary>(PdfFontCollisionTestDocument.Name("Resources"));
        PdfDictionary fonts = resources.Get<PdfDictionary>(PdfFontCollisionTestDocument.Name("Font"));
        Assert.Equal(id, fonts.Get<PdfIndirectReference>(PdfFontCollisionTestDocument.Name(name)).Identifier);
        Assert.Equal(71, resources.Get<PdfInteger>(PdfFontCollisionTestDocument.Name("PrivateResourceEntry")).Value);
        Assert.Equal(new PdfObjectIdentifier(5),
            fonts.Get<PdfIndirectReference>(PdfFontCollisionTestDocument.Name("KeepFontAlias")).Identifier);
        Assert.True(context.Document.Objects.Contains(id));
    }

    private static (string Name, PdfObjectIdentifier FontId, PdfObjectWriteProjectionSet Objects) Create(
        PdfFontCollisionTestDocument context, string projector)
    {
        var plan = PdfWritePlanner.Create(context.Document);
        if (projector == "type0")
        {
            var type0 = PdfExternalType0TrueTypeResourceProjector.Create(plan, PdfFilteredTextTestDocument.PageId,
                "FExt1", context.Face, context.Program, "A");
            return (type0.ResourceName, type0.Type0FontIdentifier, type0.Projections);
        }

        var simple = projector == "full"
            ? PdfExternalTrueTypeResourceProjector.Create(plan, PdfFilteredTextTestDocument.PageId,
                "FExt1", context.Face, context.Program)
            : PdfExternalTrueTypeSubsetResourceProjector.Create(plan, PdfFilteredTextTestDocument.PageId,
                "FExt1", context.Face, context.Program, "A");
        return (simple.ResourceName, simple.FontIdentifier, simple.Projections);
    }
}
