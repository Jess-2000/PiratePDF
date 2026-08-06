namespace BlackFlag.Engine.Filters.Tests;

public sealed class PdfFilterDecodeBudgetTests
{
    [Fact]
    public void AccountDecodedBytesAccumulatesUsage()
    {
        var budget =
            new PdfFilterDecodeBudget(
                encodedBytes: 10,
                CreateLimits(
                    maximumDecodedBytes: 100,
                    maximumExpansionRatio: 10));

        budget.AccountDecodedBytes(4);
        budget.AccountDecodedBytes(6);

        Assert.Equal(10, budget.DecodedBytes);
    }

    [Fact]
    public void AccountDecodedBytesRejectsDecodedByteLimit()
    {
        var budget =
            new PdfFilterDecodeBudget(
                encodedBytes: 10,
                CreateLimits(
                    maximumDecodedBytes: 5,
                    maximumExpansionRatio: 10));

        PdfFilterDecodeException exception =
            Assert.Throws<PdfFilterDecodeException>(
                () => budget.AccountDecodedBytes(6));

        Assert.Equal(
            PdfFilterDecodeFailure.DecodedByteLimitExceeded,
            exception.Failure);
    }

    [Fact]
    public void AccountDecodedBytesRejectsExpansionRatio()
    {
        var budget =
            new PdfFilterDecodeBudget(
                encodedBytes: 2,
                CreateLimits(
                    maximumDecodedBytes: 100,
                    maximumExpansionRatio: 2));

        PdfFilterDecodeException exception =
            Assert.Throws<PdfFilterDecodeException>(
                () => budget.AccountDecodedBytes(5));

        Assert.Equal(
            PdfFilterDecodeFailure.ExpansionRatioExceeded,
            exception.Failure);
    }

    [Fact]
    public void EmptyInputCannotProduceOutput()
    {
        var budget =
            new PdfFilterDecodeBudget(
                encodedBytes: 0,
                CreateLimits(
                    maximumDecodedBytes: 100,
                    maximumExpansionRatio: 100));

        PdfFilterDecodeException exception =
            Assert.Throws<PdfFilterDecodeException>(
                () => budget.AccountDecodedBytes(1));

        Assert.Equal(
            PdfFilterDecodeFailure.ExpansionRatioExceeded,
            exception.Failure);
    }

    [Fact]
    public void CancellationProducesClassifiedFailure()
    {
        using var cancellation =
            new CancellationTokenSource();

        cancellation.Cancel();

        var budget =
            new PdfFilterDecodeBudget(
                encodedBytes: 10,
                CreateLimits(),
                cancellation.Token);

        PdfFilterDecodeException exception =
            Assert.Throws<PdfFilterDecodeException>(
                budget.ThrowIfCancellationRequested);

        Assert.Equal(
            PdfFilterDecodeFailure.Cancelled,
            exception.Failure);
    }

    private static PdfFilterDecodeLimits CreateLimits(
        long maximumDecodedBytes = 1024,
        double maximumExpansionRatio = 100)
    {
        return new PdfFilterDecodeLimits(
            maximumDecodedBytes,
            maximumExpansionRatio,
            maximumFilterCount: 8,
            bufferSize: 16);
    }
}
