using BlackFlag.Engine.Objects;
using Xunit;

namespace BlackFlag.Engine.Objects.Tests;

public sealed class PdfObjectVisitorTests
{
    [Fact]
    public void VisitMayReturnReferenceResult()
    {
        var value = new TestPdfObject();
        var expected = new object();
        var visitor = new ReferenceVisitor(expected);

        object result = visitor.Visit(value);

        Assert.Same(expected, result);
    }

    [Fact]
    public void VisitMayReturnValueResult()
    {
        var value = new TestPdfObject();
        var visitor = new TypeVisitor();

        PdfObjectType result = visitor.Visit(value);

        Assert.Equal(PdfObjectType.Null, result);
    }

    [Fact]
    public void VisitorImplementationMayRejectNullObject()
    {
        var visitor = new TypeVisitor();

        Assert.Throws<ArgumentNullException>(
            () => visitor.Visit((PdfObject)null!));
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

    private sealed class ReferenceVisitor(object result) : PdfObjectVisitor<object>
    {
        public override object Visit(PdfObject value)
        {
            ArgumentNullException.ThrowIfNull(value);
            return result;
        }
    }

    private sealed class TypeVisitor : PdfObjectVisitor<PdfObjectType>
    {
        public override PdfObjectType Visit(PdfObject value)
        {
            ArgumentNullException.ThrowIfNull(value);
            return value.Type;
        }
    }
}

