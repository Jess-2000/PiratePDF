using BlackFlag.Engine.Objects;
using Xunit;

namespace BlackFlag.Engine.Objects.Tests;

public sealed class PdfNullTests
{
    [Fact]
    public void InstanceReturnsCanonicalObject()
    {
        Assert.Same(PdfNull.Instance, PdfNull.Instance);
    }

    [Fact]
    public void TypeReturnsNull()
    {
        Assert.Equal(PdfObjectType.Null, PdfNull.Instance.Type);
    }

    [Fact]
    public void EqualityUsesNullValueSemantics()
    {
        PdfNull value = PdfNull.Instance;

        Assert.True(value.Equals(PdfNull.Instance));
        Assert.True(value == PdfNull.Instance);
        Assert.False(value != PdfNull.Instance);
        Assert.False(value.Equals(null));
    }

    [Fact]
    public void AcceptDispatchesToNullOverload()
    {
        var visitor = new RecordingVisitor();

        string result = PdfNull.Instance.Accept(visitor);

        Assert.Equal("Null", result);
    }

    [Fact]
    public void AcceptRejectsNullVisitor()
    {
        Assert.Throws<ArgumentNullException>(
            () => PdfNull.Instance.Accept<string>(null!));
    }

    private sealed class RecordingVisitor : PdfObjectVisitor<string>
    {
        public override string Visit(PdfObject value) => "Object";

        public override string Visit(PdfNull value) => "Null";
    }
}

