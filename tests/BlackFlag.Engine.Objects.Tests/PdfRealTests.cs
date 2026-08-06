using BlackFlag.Engine.Objects;
using Xunit;

namespace BlackFlag.Engine.Objects.Tests;

public sealed class PdfRealTests
{
    [Theory]
    [InlineData(-123.5)]
    [InlineData(0.0)]
    [InlineData(123.5)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void ConstructorStoresValue(double expected)
    {
        var value = new PdfReal(expected);

        Assert.Equal(expected, value.Value);
        Assert.Equal(PdfObjectType.RealNumber, value.Type);
    }

    [Fact]
    public void EqualityUsesDoubleEqualsSemantics()
    {
        var left = new PdfReal(double.NaN);
        var equal = new PdfReal(double.NaN);
        var different = new PdfReal(1.0);

        Assert.Equal(left, equal);
        Assert.True(left == equal);
        Assert.False(left != equal);
        Assert.NotEqual(left, different);
        Assert.Equal(left.GetHashCode(), equal.GetHashCode());
    }

    [Fact]
    public void AcceptDispatchesToRealOverload()
    {
        var visitor = new RecordingVisitor();

        Assert.Equal("Real", new PdfReal(1.5).Accept(visitor));
    }

    private sealed class RecordingVisitor : PdfObjectVisitor<string>
    {
        public override string Visit(PdfObject value) => "Object";

        public override string Visit(PdfReal value) => "Real";
    }
}

