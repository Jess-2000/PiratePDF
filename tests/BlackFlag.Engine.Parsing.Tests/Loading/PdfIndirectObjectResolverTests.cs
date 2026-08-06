using System.Text;
using BlackFlag.Engine.Filters;
using BlackFlag.Engine.Objects;
using BlackFlag.Engine.Parsing.CrossReferences;
using BlackFlag.Engine.Parsing.Loading;

#nullable enable

namespace BlackFlag.Engine.Parsing.Tests.Loading;

public sealed class PdfIndirectObjectResolverTests
{
    [Fact]
    public void ResolvesAndCachesDirectObject()
    {
        using var source = CreateSource("1 0 obj 42 endobj");
        var resolver = CreateResolver(
            source,
            PdfObjectLocation.CreateDirect(
                new PdfObjectId(1, 0),
                offset: 0));

        PdfObject first = resolver.Resolve(new PdfObjectId(1, 0));
        PdfObject second = resolver.Resolve(new PdfObjectId(1, 0));

        PdfInteger value = Assert.IsType<PdfInteger>(first);
        Assert.Equal(42, value.Value);
        Assert.Same(first, second);
    }

    [Fact]
    public void RejectsIdentifierAtUnexpectedOffset()
    {
        using var source = CreateSource("2 0 obj 42 endobj");
        var resolver = CreateResolver(
            source,
            PdfObjectLocation.CreateDirect(
                new PdfObjectId(1, 0),
                offset: 0));

        PdfObjectResolutionException exception =
            Assert.Throws<PdfObjectResolutionException>(
                () => resolver.Resolve(new PdfObjectId(1, 0)));

        Assert.Equal(
            PdfObjectResolutionFailure.IdentifierMismatch,
            exception.Failure);
    }

    [Fact]
    public void TryResolveReturnsFalseForFreeObject()
    {
        using var source = CreateSource(string.Empty);
        var objectId = new PdfObjectId(3, 1);
        var resolver = CreateResolver(
            source,
            PdfObjectLocation.CreateFree(objectId));

        bool resolved = resolver.TryResolve(objectId, out PdfObject? value);

        Assert.False(resolved);
        Assert.Null(value);
    }

    [Fact]
    public void ResolvesObjectFromObjectStream()
    {
        const string Pdf =
            "1 0 obj\n"
            + "<< /Type /ObjStm /N 1 /First 4 /Length 6 >>\n"
            + "stream\n"
            + "2 0 42\n"
            + "endstream\n"
            + "endobj";

        using var source = CreateSource(Pdf);
        var resolver = CreateResolver(
            source,
            PdfObjectLocation.CreateDirect(
                new PdfObjectId(1, 0),
                offset: 0),
            PdfObjectLocation.CreateCompressed(
                objectNumber: 2,
                objectStreamNumber: 1,
                objectStreamIndex: 0));

        PdfObject result = resolver.Resolve(new PdfObjectId(2, 0));

        PdfInteger value = Assert.IsType<PdfInteger>(result);
        Assert.Equal(42, value.Value);
    }

    private static PdfIndirectObjectResolver CreateResolver(
        TestRandomAccessSource source,
        params PdfObjectLocation[] locations)
    {
        var table = new PdfObjectLocationTable(
            [locations]);

        return new PdfIndirectObjectResolver(
            source,
            table,
            new PdfFilterPipeline(PdfFilterRegistrations.CreateDefault()),
            new PdfFilterDecodeLimits(
                maximumDecodedBytes: 1024 * 1024,
                maximumExpansionRatio: 1000,
                maximumFilterCount: 8,
                bufferSize: 256));
    }

    private static TestRandomAccessSource CreateSource(string value) =>
        new(Encoding.ASCII.GetBytes(value));
}
