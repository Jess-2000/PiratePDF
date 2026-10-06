using BlackFlag.Engine.Objects;

namespace BlackFlag.Engine.Filters.Tests;

public sealed class PdfFilterDecodeContextTests
{
    [Fact]
    public void ConstructorPreservesOperationState()
    {
        var budget =
            new TestDecodeBudget();

        var parameters =
            new PdfDictionary(
                []);

        var context =
            new PdfFilterDecodeContext(
                budget,
                parameters,
                filterIndex: 2);

        Assert.Same(budget, context.Budget);
        Assert.Same(parameters, context.Parameters);
        Assert.Equal(2, context.FilterIndex);
    }

    [Fact]
    public void ConstructorRejectsNegativeFilterIndex()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new PdfFilterDecodeContext(
                new TestDecodeBudget(),
                filterIndex: -1));
    }

    private sealed class TestDecodeBudget :
        IPdfFilterDecodeBudget
    {
        public PdfFilterDecodeLimits Limits { get; } = new();

        public long EncodedBytes => 0;

        public long DecodedBytes => 0;

        public void ThrowIfCancellationRequested()
        {
        }

        public void AccountDecodedBytes(int byteCount)
        {
        }
    }
}
