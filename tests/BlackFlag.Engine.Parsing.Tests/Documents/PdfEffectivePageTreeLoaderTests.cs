using BlackFlag.Engine.Document.Pages;
using BlackFlag.Engine.Objects;
using BlackFlag.Engine.Parsing.CrossReferences;
using BlackFlag.Engine.Parsing.Documents;
using BlackFlag.Engine.Parsing.Loading;

#nullable enable

namespace BlackFlag.Engine.Parsing.Tests.Documents;

public sealed class PdfEffectivePageTreeLoaderTests
{
    [Fact]
    public void LoadResolvesInheritedGeometryAndPrepressFallbacks()
    {
        var rootId = new PdfObjectId(1, 0);
        var pageId = new PdfObjectId(2, 0);
        var resolver = new DictionaryResolver(new Dictionary<PdfObjectId, PdfObject>
        {
            [rootId] = Dictionary(
                ("Type", new PdfName("Pages")),
                ("MediaBox", Array(0, 0, 612, 792)),
                ("Rotate", new PdfInteger(90)),
                ("Kids", new PdfArray([new PdfIndirectReference(2, 0)]))),
            [pageId] = Dictionary(("Type", new PdfName("Page")))
        });

        PdfEffectivePage page = new PdfEffectivePageTreeLoader(resolver)
            .Load(rootId)[0];

        Assert.Equal(PdfPageValueOrigin.Inherited, page.Geometry.MediaBox.Origin);
        Assert.Equal(PdfPageValueOrigin.Defaulted, page.Geometry.CropBox.Origin);
        Assert.Equal(PdfPageValueOrigin.Defaulted, page.Geometry.BleedBox.Origin);
        Assert.Equal(792, page.Geometry.DisplayWidth);
        Assert.Equal(612, page.Geometry.DisplayHeight);
    }

    [Fact]
    public void LoadRejectsPageTreeCycle()
    {
        var rootId = new PdfObjectId(1, 0);
        var resolver = new DictionaryResolver(new Dictionary<PdfObjectId, PdfObject>
        {
            [rootId] = Dictionary(
                ("Type", new PdfName("Pages")),
                ("Kids", new PdfArray([new PdfIndirectReference(1, 0)])))
        });

        Assert.Throws<PdfPageTreeLoadException>(
            () => new PdfEffectivePageTreeLoader(resolver).Load(rootId));
    }

    private static PdfDictionary Dictionary(
        params (string Key, PdfObject Value)[] entries) =>
        new(entries.Select(static entry =>
            new KeyValuePair<PdfName, PdfObject>(
                new PdfName(entry.Key),
                entry.Value)));

    private static PdfArray Array(params long[] values) =>
        new(values.Select(static value => (PdfObject)new PdfInteger(value)));

    private sealed class DictionaryResolver(
        IReadOnlyDictionary<PdfObjectId, PdfObject> objects)
        : IPdfIndirectObjectResolver
    {
        public PdfObject Resolve(PdfObjectId objectId) => objects[objectId];

        public bool TryResolve(PdfObjectId objectId, out PdfObject? value) =>
            objects.TryGetValue(objectId, out value);
    }
}
