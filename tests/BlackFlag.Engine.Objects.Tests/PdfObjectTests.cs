using BlackFlag.Engine.Objects;
using Xunit;

namespace BlackFlag.Engine.Objects.Tests;

public sealed class PdfObjectTests
{
    [Fact]
    public void TypeReturnsConcreteObjectType()
    {
        var value = new TestPdfObject();

        Assert.Equal(PdfObjectType.Null, value.Type);
    }

    [Fact]
    public void AcceptReturnsVisitorResult()
    {
        var value = new TestPdfObject();
        var visitor = new ConstantVisitor(42);

        int result = value.Accept(visitor);

        Assert.Equal(42, result);
    }

    [Fact]
    public void AcceptPassesSameObjectToVisitor()
    {
        var value = new TestPdfObject();
        var visitor = new RecordingVisitor();

        value.Accept(visitor);

        Assert.Same(value, visitor.VisitedObject);
    }

    [Fact]
    public void AcceptRejectsNullVisitor()
    {
        var value = new TestPdfObject();

        Assert.Throws<ArgumentNullException>(
            () => value.Accept<object>(null!));
    }

    [Theory]
    [InlineData(PdfObjectType.Null)]
    [InlineData(PdfObjectType.Boolean)]
    [InlineData(PdfObjectType.IntegerNumber)]
    [InlineData(PdfObjectType.RealNumber)]
    [InlineData(PdfObjectType.Name)]
    [InlineData(PdfObjectType.LiteralString)]
    [InlineData(PdfObjectType.HexString)]
    [InlineData(PdfObjectType.Array)]
    [InlineData(PdfObjectType.Dictionary)]
    [InlineData(PdfObjectType.IndirectReference)]
    public void ObjectTypeDefinesDeclaredKind(PdfObjectType type)
    {
        Assert.True(Enum.IsDefined(type));
    }

    private sealed class TestPdfObject : PdfObject
    {
        public override PdfObjectType Type => PdfObjectType.Null;

        public override TResult Accept<TResult>(PdfObjectVisitor<TResult> visitor)
        {
            ArgumentNullException.ThrowIfNull(visitor);
            return visitor.Visit(this);
        }
    }

    private sealed class ConstantVisitor(int result) : PdfObjectVisitor<int>
    {
        public override int Visit(PdfObject value)
        {
            ArgumentNullException.ThrowIfNull(value);
            return result;
        }
    }

    private sealed class RecordingVisitor : PdfObjectVisitor<object?>
    {
        public PdfObject? VisitedObject { get; private set; }

        public override object? Visit(PdfObject value)
        {
            ArgumentNullException.ThrowIfNull(value);
            VisitedObject = value;
            return null;
        }
    }
}
