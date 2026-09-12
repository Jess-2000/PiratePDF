using BlackFlag.Engine.Objects;
using BlackFlag.Engine.Parsing.CrossReferences;
using BlackFlag.Engine.Parsing.Loading;
using BlackFlag.Engine.Parsing.Resources;
using BlackFlag.Engine.Rendering.Resources;
using BlackFlag.Engine.Rendering.DisplayList;
using BlackFlag.Engine.Rendering.Graphics;
using BlackFlag.Engine.Rendering.Images;
using System.IO;
using System.Runtime.InteropServices;

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

    [Theory]
    [InlineData("XObject", 3)]
    [InlineData("XObject", 4)]
    [InlineData("XObject", 1024 * 1024)]
    [InlineData("Font", 3)]
    [InlineData("Font", 4)]
    [InlineData("Font", 1024 * 1024)]
    [InlineData("ColorSpace", 3)]
    [InlineData("ColorSpace", 4)]
    [InlineData("ColorSpace", 1024 * 1024)]
    public void StreamResourceLimitsUseActualLengthWithoutSnapshotAllocation(string category, int length)
    {
        var stream = new PdfStream(Dictionary(
            ("Length", new PdfInteger(0)),
            ("Subtype", new PdfName("Image")),
            ("Width", new PdfInteger(1)),
            ("Height", new PdfInteger(1)),
            ("BitsPerComponent", new PdfInteger(8)),
            ("N", new PdfInteger(3))), new byte[length]);
        PdfObject resource = category switch
        {
            "Font" => Dictionary(("Subtype", new PdfName("TrueType")),
                ("BaseFont", new PdfName("Example")),
                ("FontDescriptor", Dictionary(("FontFile2", stream)))),
            "ColorSpace" => new PdfArray([new PdfName("ICCBased"), stream]),
            _ => stream
        };
        var resolver = new PdfResourceResolver(new DictionaryObjectResolver(), new PdfResourceResolutionOptions
        {
            MaximumImageEncodedBytes = 3,
            MaximumEmbeddedFontBytes = 3,
            MaximumIccProfileBytes = 3
        });
        PdfResourceScope scope = resolver.CreateScope(ResourceDictionary(category, "R1", resource));
        var name = new PdfName("R1");
        Action resolve = () =>
        {
            switch (category)
            {
                case "Font": _ = scope.ResolveFont(name); break;
                case "ColorSpace": _ = scope.ResolveColorSpace(name); break;
                default: _ = scope.ResolveXObject(name); break;
            }
        };
        if (length > 3)
        {
            long allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
            PdfResourceResolutionException failure = Assert.Throws<PdfResourceResolutionException>(resolve);
            long allocated = GC.GetAllocatedBytesForCurrentThread() - allocatedBefore;
            Assert.Equal(PdfResourceResolutionFailure.LimitExceeded, failure.Failure);
            string label = category switch
            {
                "Font" => "Embedded font stream",
                "ColorSpace" => "ICC profile",
                _ => "Image stream"
            };
            Assert.Equal($"{label} size {length} exceeds the configured maximum of 3 bytes.", failure.Message);
            // One MiB input must be rejected without a same-sized public snapshot.
            if (length == 1024 * 1024) Assert.InRange(allocated, 0, 128 * 1024);
        }
        else
        {
            resolve();
            MemoryMarshal.AsMemory(stream.Data).Span.Fill(0xFF);
            resolve();
            Assert.Equal(new byte[length], stream.Data.ToArray());
        }
        Assert.Equal(length, stream.Length);
        Assert.Equal(0, Assert.IsType<PdfInteger>(stream.Dictionary[new PdfName("Length")]).Value);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(1024 * 1024)]
    public void ImageDecodePreservesSnapshotIsolationAndRejectsBeforeCopying(int length)
    {
        var stream = new PdfStream(Dictionary(
            ("Subtype", new PdfName("Image")), ("Width", new PdfInteger(1)),
            ("Height", new PdfInteger(1)), ("BitsPerComponent", new PdfInteger(8)),
            ("ColorSpace", new PdfName("DeviceGray"))), new byte[length]);
        PdfResourceScope scope = CreateResolver().CreateScope(ResourceDictionary("XObject", "Im1", stream));
        var image = Assert.IsType<PdfResolvedImageXObject>(scope.ResolveXObject(new PdfName("Im1")));
        var item = new PdfDisplayImage(image, null, PdfMatrix.Identity, new PdfGraphicsStateSnapshot(),
            new PdfContentSourceSpan(0, 0, length), null);
        var decoder = new PdfImageDecoder();
        var limits = new PdfImageDecodeLimits { MaximumEncodedBytes = 1 };
        if (length > 1)
        {
            long allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
            _ = Assert.Throws<InvalidDataException>(() => decoder.Decode(item, limits));
            long allocated = GC.GetAllocatedBytesForCurrentThread() - allocatedBefore;
            Assert.InRange(allocated, 0, 128 * 1024);
        }
        else
        {
            byte[] before = decoder.Decode(item, limits).Image.Pixels.ToArray();
            MemoryMarshal.AsMemory(stream.Data).Span[0] = 255;
            Assert.Equal(before, decoder.Decode(item, limits).Image.Pixels.ToArray());
            Assert.Equal(new byte[] { 0, 0, 0, 255 }, before);
        }
        Assert.Same(stream, image.Stream);
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
