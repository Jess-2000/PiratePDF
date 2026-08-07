using BlackFlag.Engine.Objects;
using BlackFlag.Engine.Parsing.CrossReferences;
using BlackFlag.Engine.Parsing.Loading;
using BlackFlag.Engine.Parsing.Resources;
using BlackFlag.Engine.Rendering.Resources;

namespace BlackFlag.Engine.Parsing.Tests.Resources;

public sealed class PdfResourceResolverTests
{
    [Fact]
    public void ResolvesFontMetadataAndSubsetName()
    {
        PdfDictionary descriptor = Dictionary(
            ("FontFamily", new PdfLiteralString("Acme Sans")),
            ("FontWeight", new PdfInteger(700)),
            ("ItalicAngle", new PdfInteger(-12)));
        PdfDictionary font = Dictionary(
            ("Subtype", new PdfName("TrueType")),
            ("BaseFont", new PdfName("ABCDEF+AcmeSans-BoldItalic")),
            ("FontDescriptor", descriptor));
        PdfDictionary resources = ResourceDictionary(
            "Font",
            "F1",
            font);

        PdfResourceScope scope = CreateResolver().CreateScope(resources);
        PdfResolvedFont result = scope.ResolveFont(new PdfName("F1"));

        Assert.Equal("TrueType", result.Subtype);
        Assert.Equal("Acme Sans", result.FamilyName);
        Assert.Equal("AcmeSans-BoldItalic", result.FaceName);
        Assert.Equal(700, result.Weight);
        Assert.Equal(PdfFontSlant.Italic, result.Slant);
        Assert.Equal(PdfFontTechnology.TrueType, result.Technology);
        Assert.True(result.IsSubset);
    }

    [Fact]
    public void CachesSharedIndirectResourceByObjectIdentity()
    {
        PdfDictionary font = Dictionary(
            ("Subtype", new PdfName("Type1")),
            ("BaseFont", new PdfName("Helvetica")));
        var reference = new PdfIndirectReference(7, 0);
        var objectResolver = new DictionaryObjectResolver(
            (new PdfObjectId(7, 0), font));
        var resolver = new PdfResourceResolver(objectResolver);
        PdfDictionary firstResources = ResourceDictionary("Font", "F1", reference);
        PdfDictionary secondResources = ResourceDictionary("Font", "Body", reference);

        PdfResolvedFont first = resolver.CreateScope(firstResources).ResolveFont(new PdfName("F1"));
        PdfResolvedFont second = resolver.CreateScope(secondResources).ResolveFont(new PdfName("Body"));

        Assert.Same(first, second);
        Assert.True(first.Identity.IsIndirect);
        Assert.Equal(7, first.Identity.ObjectNumber);
        Assert.Equal(1, objectResolver.ResolveCount);
    }

    [Fact]
    public void FallsBackToParentScope()
    {
        PdfDictionary parentResources = ResourceDictionary(
            "Font",
            "F1",
            Dictionary(
                ("Subtype", new PdfName("Type1")),
                ("BaseFont", new PdfName("Helvetica"))));
        PdfDictionary childResources = PdfDictionary.Empty;
        var resolver = CreateResolver();
        PdfResourceScope parent = resolver.CreateScope(parentResources);
        PdfResourceScope child = resolver.CreateScope(childResources, parent);

        PdfResolvedFont result = child.ResolveFont(new PdfName("F1"));

        Assert.Equal("Helvetica", result.FaceName);
        Assert.Equal(2, child.Depth);
    }

    [Fact]
    public void LocalResourceOverridesParentResourceWithSameName()
    {
        var resolver = CreateResolver();
        PdfResourceScope parent = resolver.CreateScope(
            ResourceDictionary(
                "Font",
                "F1",
                Dictionary(
                    ("Subtype", new PdfName("Type1")),
                    ("BaseFont", new PdfName("Helvetica")))));
        PdfResourceScope child = resolver.CreateScope(
            ResourceDictionary(
                "Font",
                "F1",
                Dictionary(
                    ("Subtype", new PdfName("TrueType")),
                    ("BaseFont", new PdfName("Arial")))),
            parent);

        PdfResolvedFont result = child.ResolveFont(new PdfName("F1"));

        Assert.Equal("Arial", result.FaceName);
        Assert.Equal(PdfFontTechnology.TrueType, result.Technology);
    }

    [Fact]
    public void ResolvesImageWithoutDecodingStreamBytes()
    {
        byte[] encoded = [1, 2, 3, 4];
        var image = new PdfStream(
            Dictionary(
                ("Subtype", new PdfName("Image")),
                ("Width", new PdfInteger(640)),
                ("Height", new PdfInteger(480)),
                ("BitsPerComponent", new PdfInteger(8)),
                ("ColorSpace", new PdfName("DeviceRGB")),
                ("Filter", new PdfName("DCTDecode"))),
            encoded);
        PdfResourceScope scope = CreateResolver().CreateScope(
            ResourceDictionary("XObject", "Im1", image));

        PdfResolvedImageXObject result = Assert.IsType<PdfResolvedImageXObject>(
            scope.ResolveXObject(new PdfName("Im1")));

        Assert.Equal(640, result.Width);
        Assert.Equal(480, result.Height);
        Assert.Equal(8, result.BitsPerComponent);
        Assert.Equal(encoded, result.Stream.Data.ToArray());
        Assert.Equal(new PdfName("DCTDecode"), result.Filters);
    }

    [Fact]
    public void FormResourceScopeUsesLocalResourcesThenParentFallback()
    {
        var resolver = CreateResolver();
        PdfResourceScope pageScope = resolver.CreateScope(
            ResourceDictionary(
                "Font",
                "PageFont",
                Dictionary(
                    ("Subtype", new PdfName("Type1")),
                    ("BaseFont", new PdfName("Helvetica")))));
        PdfDictionary formResources = ResourceDictionary(
            "Font",
            "FormFont",
            Dictionary(
                ("Subtype", new PdfName("TrueType")),
                ("BaseFont", new PdfName("Arial"))));
        var form = new PdfStream(
            Dictionary(
                ("Subtype", new PdfName("Form")),
                ("BBox", new PdfArray([
                    new PdfInteger(0),
                    new PdfInteger(0),
                    new PdfInteger(100),
                    new PdfInteger(100)])),
                ("Resources", formResources)),
            ReadOnlyMemory<byte>.Empty);
        PdfResourceScope ownerScope = resolver.CreateScope(
            ResourceDictionary("XObject", "Fm1", form),
            pageScope);

        PdfResolvedFormXObject resolved = Assert.IsType<PdfResolvedFormXObject>(
            ownerScope.ResolveXObject(new PdfName("Fm1")));
        PdfResourceScope formScope = Assert.IsType<PdfResourceScope>(resolved.GetResourceScope());

        Assert.Equal("Arial", formScope.ResolveFont(new PdfName("FormFont")).FaceName);
        Assert.Equal("Helvetica", formScope.ResolveFont(new PdfName("PageFont")).FaceName);
    }

    [Fact]
    public void PreservesDeviceNColorantNamesAndTintTransform()
    {
        var colorSpace = new PdfArray([
            new PdfName("DeviceN"),
            new PdfArray([new PdfName("SpotOrange"), new PdfName("SpotBlue")]),
            new PdfName("DeviceCMYK"),
            new PdfDictionary([
                new KeyValuePair<PdfName, PdfObject>(new PdfName("FunctionType"), new PdfInteger(2))])]);
        PdfResourceScope scope = CreateResolver().CreateScope(
            ResourceDictionary("ColorSpace", "CS1", colorSpace));

        PdfResolvedColorSpace result = scope.ResolveColorSpace(new PdfName("CS1"));

        Assert.Equal(PdfColorSpaceKind.DeviceN, result.ColorSpaceKind);
        Assert.Equal(2, result.ComponentCount);
        Assert.Collection(
            result.ColorantNames,
            value => Assert.Equal("SpotOrange", value),
            value => Assert.Equal("SpotBlue", value));
        Assert.NotNull(result.TintTransform);
    }

    [Fact]
    public void ResolvesExtendedGraphicsStateMetadata()
    {
        PdfDictionary graphicsState = Dictionary(
            ("CA", new PdfReal(0.75)),
            ("ca", new PdfReal(0.5)),
            ("OP", new PdfBoolean(true)),
            ("op", new PdfBoolean(false)),
            ("OPM", new PdfInteger(1)),
            ("BM", new PdfName("Multiply")));
        PdfResourceScope scope = CreateResolver().CreateScope(
            ResourceDictionary("ExtGState", "GS1", graphicsState));

        PdfResolvedExtGState result = scope.ResolveExtGState(new PdfName("GS1"));

        Assert.Equal(0.75, result.StrokingAlpha);
        Assert.Equal(0.5, result.NonstrokingAlpha);
        Assert.True(result.OverprintStroke);
        Assert.False(result.OverprintFill);
        Assert.Equal(1, result.OverprintMode);
        Assert.Equal(new PdfName("Multiply"), result.BlendMode);
    }

    [Fact]
    public void RejectsImageThatExceedsConfiguredEncodedSizeLimit()
    {
        var resolver = new PdfResourceResolver(
            new DictionaryObjectResolver(),
            new PdfResourceResolutionOptions
            {
                MaximumImageEncodedBytes = 3
            });
        var image = new PdfStream(
            Dictionary(
                ("Subtype", new PdfName("Image")),
                ("Width", new PdfInteger(1)),
                ("Height", new PdfInteger(1)),
                ("BitsPerComponent", new PdfInteger(8))),
            new byte[4]);
        PdfResourceScope scope = resolver.CreateScope(
            ResourceDictionary("XObject", "Im1", image));

        PdfResourceResolutionException exception =
            Assert.Throws<PdfResourceResolutionException>(
                () => scope.ResolveXObject(new PdfName("Im1")));

        Assert.Equal(PdfResourceResolutionFailure.LimitExceeded, exception.Failure);
    }

    private static PdfResourceResolver CreateResolver() =>
        new(new DictionaryObjectResolver());

    private static PdfDictionary ResourceDictionary(
        string category,
        string name,
        PdfObject value) =>
        Dictionary(
            (category, Dictionary((name, value))));

    private static PdfDictionary Dictionary(
        params (string Key, PdfObject Value)[] entries) =>
        new(entries.Select(
            entry => new KeyValuePair<PdfName, PdfObject>(
                new PdfName(entry.Key),
                entry.Value)));

    private sealed class DictionaryObjectResolver : IPdfIndirectObjectResolver
    {
        private readonly Dictionary<PdfObjectId, PdfObject> objects;

        public DictionaryObjectResolver(
            params (PdfObjectId Id, PdfObject Value)[] entries)
        {
            objects = entries.ToDictionary(entry => entry.Id, entry => entry.Value);
        }

        public int ResolveCount { get; private set; }

        public PdfObject Resolve(PdfObjectId objectId)
        {
            ResolveCount++;
            return objects.TryGetValue(objectId, out PdfObject value)
                ? value
                : throw new KeyNotFoundException();
        }

        public bool TryResolve(PdfObjectId objectId, out PdfObject value) =>
            objects.TryGetValue(objectId, out value);
    }
}
