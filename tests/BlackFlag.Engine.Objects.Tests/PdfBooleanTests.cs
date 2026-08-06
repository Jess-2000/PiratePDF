using BlackFlag.Engine.Objects;
using Xunit;

namespace BlackFlag.Engine.Objects.Tests;

public sealed class PdfBooleanTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ConstructorStoresValue(bool expected)
    {
        var value = new PdfBoolean(expected);

        Assert.Equal(expected, value.Value);
        Assert.Equal(PdfObjectType.Boolean, value.Type);
    }

    [Fact]
    public void EqualityUsesBooleanValue()
    {
        var left = new PdfBoolean(true);
        var equal = new PdfBoolean(true);
        var different = new PdfBoolean(false);

        Assert.Equal(left, equal);
        Assert.True(left == equal);
        Assert.False(left != equal);
        Assert.NotEqual(left, different);
        Assert.False(left == different);
        Assert.True(left != different);
        Assert.Equal(left.GetHashCode(), equal.GetHashCode());
    }

    [Fact]
    public void AcceptDispatchesToBooleanOverload()
    {
        var visitor = new RecordingVisitor();

        Assert.Equal("Boolean", new PdfBoolean(true).Accept(visitor));
    }

    private sealed class RecordingVisitor : PdfObjectVisitor<string>
    {
        public override string Visit(PdfObject value) => "Object";

        public override string Visit(PdfBoolean value) => "Boolean";
    }
}

