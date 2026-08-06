using BlackFlag.Engine.Objects;
using Xunit;

namespace BlackFlag.Engine.Objects.Tests;

public sealed class PdfLiteralStringTests
{
    [Fact]
    public void ConstructorStoresExactDecodedValue()
    {
        const string expected = "Hello\r\nWorld \U0001F600";

        var value = new PdfLiteralString(expected);

        Assert.Same(expected, value.Value);
        Assert.Equal(PdfObjectType.LiteralString, value.Type);
    }

    [Fact]
    public void ConstructorRejectsNull()
    {
        Assert.Throws<ArgumentNullException>(
            () => new PdfLiteralString(null!));
    }

    [Fact]
    public void EqualityIsOrdinalAndCaseSensitive()
    {
        var left = new PdfLiteralString("Text");
        var equal = new PdfLiteralString("Text");
        var differentCase = new PdfLiteralString("text");

        Assert.Equal(left, equal);
        Assert.True(left == equal);
        Assert.NotEqual(left, differentCase);
        Assert.False(left == differentCase);
        Assert.Equal(left.GetHashCode(), equal.GetHashCode());
    }

    [Fact]
    public void AcceptDispatchesToLiteralStringOverload()
    {
        var visitor = new RecordingVisitor();

        Assert.Equal(
            "LiteralString",
            new PdfLiteralString("Text").Accept(visitor));
    }

    private sealed class RecordingVisitor : PdfObjectVisitor<string>
    {
        public override string Visit(PdfObject value) => "Object";

        public override string Visit(PdfLiteralString value) =>
            "LiteralString";
    }
}

