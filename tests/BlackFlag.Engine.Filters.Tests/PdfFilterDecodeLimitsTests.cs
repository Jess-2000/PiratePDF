using System;
using Xunit;

namespace BlackFlag.Engine.Filters.Tests;

public sealed class PdfFilterDecodeLimitsTests
{
    [Fact]
    public void ConstructorUsesDocumentedDefaults()
    {
        var limits =
            new PdfFilterDecodeLimits();

        Assert.Equal(
            PdfFilterDecodeLimits.DefaultMaximumDecodedBytes,
            limits.MaximumDecodedBytes);

        Assert.Equal(
            PdfFilterDecodeLimits.DefaultMaximumExpansionRatio,
            limits.MaximumExpansionRatio);

        Assert.Equal(
            PdfFilterDecodeLimits.DefaultMaximumFilterCount,
            limits.MaximumFilterCount);

        Assert.Equal(
            PdfFilterDecodeLimits.DefaultBufferSize,
            limits.BufferSize);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void ConstructorRejectsInvalidMaximumDecodedBytes(
        long value)
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new PdfFilterDecodeLimits(
                value,
                10,
                1,
                1024));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void ConstructorRejectsInvalidExpansionRatio(
        double value)
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new PdfFilterDecodeLimits(
                1024,
                value,
                1,
                1024));
    }
}
