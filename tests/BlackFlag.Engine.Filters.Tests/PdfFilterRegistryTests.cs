using BlackFlag.Engine.Objects;

namespace BlackFlag.Engine.Filters.Tests;

public sealed class PdfFilterRegistryTests
{
    [Fact]
    public void TryGetReturnsRegisteredDecoder()
    {
        var decoder =
            new TestFilterDecoder(
                "Example",
                static (_, _, _) => { });

        var registry =
            new PdfFilterRegistry(
                [decoder]);

        bool found =
            registry.TryGet(
                new PdfName("Example"),
                out IPdfFilterDecoder? resolved);

        Assert.True(found);
        Assert.Same(decoder, resolved);
    }

    [Fact]
    public void ConstructorRejectsDuplicateFilterNames()
    {
        var first =
            new TestFilterDecoder(
                "Duplicate",
                static (_, _, _) => { });

        var second =
            new TestFilterDecoder(
                "Duplicate",
                static (_, _, _) => { });

        Assert.Throws<ArgumentException>(
            () => new PdfFilterRegistry(
                [first, second]));
    }

    [Fact]
    public void GetRequiredClassifiesUnsupportedFilter()
    {
        var registry =
            new PdfFilterRegistry();

        PdfFilterDecodeException exception =
            Assert.Throws<PdfFilterDecodeException>(
                () => registry.GetRequired(
                    new PdfName("Missing")));

        Assert.Equal(
            PdfFilterDecodeFailure.UnsupportedFilter,
            exception.Failure);
    }
}
