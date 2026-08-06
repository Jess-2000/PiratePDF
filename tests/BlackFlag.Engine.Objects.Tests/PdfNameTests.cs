using BlackFlag.Engine.Objects;
using Xunit;

namespace BlackFlag.Engine.Objects.Tests;

public sealed class PdfNameTests
{
    [Fact]
    public void ConstructorStoresExactValue()
    {
        const string expected = "Font#20Name";

        var value = new PdfName(expected);

        Assert.Same(expected, value.Value);
        Assert.Equal(PdfObjectType.Name, value.Type);
    }

    [Fact]
    public void ConstructorRejectsNull()
    {
        Assert.Throws<ArgumentNullException>(() => new PdfName(null!));
    }

    [Fact]
    public void EqualityIsOrdinalAndCaseSensitive()
    {
        var left = new PdfName("Font");
        var equal = new PdfName("Font");
        var differentCase = new PdfName("font");

        Assert.Equal(left, equal);
        Assert.True(left == equal);
        Assert.NotEqual(left, differentCase);
        Assert.False(left == differentCase);
        Assert.Equal(left.GetHashCode(), equal.GetHashCode());
    }

    [Fact]
    public void EmptyNameIsPreserved()
    {
        var value = new PdfName(string.Empty);

        Assert.Equal(string.Empty, value.Value);
    }

    [Fact]
    public void AcceptDispatchesToNameOverload()
    {
        var visitor = new RecordingVisitor();

        Assert.Equal("Name", new PdfName("Type").Accept(visitor));
    }

    private sealed class RecordingVisitor : PdfObjectVisitor<string>
    {
        public override string Visit(PdfObject value) => "Object";

        public override string Visit(PdfName value) => "Name";
    }
}

