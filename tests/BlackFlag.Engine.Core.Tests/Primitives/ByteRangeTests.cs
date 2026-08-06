using BlackFlag.Engine.Core.Primitives;

namespace BlackFlag.Engine.Core.Tests.Primitives;

public sealed class ByteRangeTests
{
    [Fact]
    public void Constructor_WithValidValues_SetsProperties()
    {
        var range = new ByteRange(offset: 100, length: 25);

        Assert.Equal(100, range.Offset);
        Assert.Equal(25, range.Length);
        Assert.Equal(125, range.End);
        Assert.False(range.IsEmpty);
    }

    [Fact]
    public void Constructor_WithZeroLength_CreatesEmptyRange()
    {
        var range = new ByteRange(offset: 100, length: 0);

        Assert.True(range.IsEmpty);
        Assert.Equal(100, range.End);
    }

    [Fact]
    public void Constructor_WithNegativeOffset_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new ByteRange(offset: -1, length: 10));
    }

    [Fact]
    public void Constructor_WithNegativeLength_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new ByteRange(offset: 0, length: -1));
    }

    [Fact]
    public void End_WhenRangeOverflows_Throws()
    {
        var range = new ByteRange(long.MaxValue, 1);

        Assert.Throws<InvalidOperationException>(
            () => _ = range.End);
    }

    [Theory]
    [InlineData(100, true)]
    [InlineData(124, true)]
    [InlineData(99, false)]
    [InlineData(125, false)]
    public void ContainsOffset_ReturnsExpectedResult(
        long offset,
        bool expected)
    {
        var range = new ByteRange(offset: 100, length: 25);

        Assert.Equal(expected, range.Contains(offset));
    }

    [Fact]
    public void ContainsRange_WhenFullyInside_ReturnsTrue()
    {
        var outer = new ByteRange(offset: 100, length: 100);
        var inner = new ByteRange(offset: 125, length: 25);

        Assert.True(outer.Contains(inner));
    }

    [Fact]
    public void ContainsRange_WhenPartlyOutside_ReturnsFalse()
    {
        var outer = new ByteRange(offset: 100, length: 100);
        var inner = new ByteRange(offset: 190, length: 20);

        Assert.False(outer.Contains(inner));
    }

    [Fact]
    public void Intersects_WhenRangesOverlap_ReturnsTrue()
    {
        var first = new ByteRange(offset: 100, length: 50);
        var second = new ByteRange(offset: 125, length: 50);

        Assert.True(first.Intersects(second));
        Assert.True(second.Intersects(first));
    }

    [Fact]
    public void Intersects_WhenRangesTouchButDoNotOverlap_ReturnsFalse()
    {
        var first = new ByteRange(offset: 100, length: 25);
        var second = new ByteRange(offset: 125, length: 25);

        Assert.False(first.Intersects(second));
    }
}
