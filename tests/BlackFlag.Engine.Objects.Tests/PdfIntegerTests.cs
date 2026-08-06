using BlackFlag.Engine.Objects;
using Xunit;

namespace BlackFlag.Engine.Objects.Tests;

public sealed class PdfIntegerTests
{
    [Theory]
    [InlineData(long.MinValue)]
    [InlineData(-1L)]
    [InlineData(0L)]
    [InlineData(1L)]
    [InlineData(long.MaxValue)]
    public void ConstructorStoresFullLongRange(long expected)
    {
        var value = new PdfInteger(expected);

        Assert.Equal(expected, value.Value);
        Assert.Equal(PdfObjectType.IntegerNumber, value.Type);
    }

    [Fact]
    public void EqualityUsesIntegerValue()
    {
        var left = new PdfInteger(42);
        var equal = new PdfInteger(42);
        var different = new PdfInteger(43);

        Assert.Equal(left, equal);
        Assert.True(left == equal);
        Assert.False(left != equal);
        Assert.NotEqual(left, different);
        Assert.Equal(left.GetHashCode(), equal.GetHashCode());
    }

    [Fact]
    public void AcceptDispatchesToIntegerOverload()
    {
        var visitor = new RecordingVisitor();

        Assert.Equal("Integer", new PdfInteger(1).Accept(visitor));
    }

    private sealed class RecordingVisitor : PdfObjectVisitor<string>
    {
        public override string Visit(PdfObject value) => "Object";

        public override string Visit(PdfInteger value) => "Integer";
    }
}

