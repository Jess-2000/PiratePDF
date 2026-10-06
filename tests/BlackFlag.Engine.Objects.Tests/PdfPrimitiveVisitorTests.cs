using Xunit;

namespace BlackFlag.Engine.Objects.Tests;

public sealed class PdfPrimitiveVisitorTests
{
    public static TheoryData<PdfObject, string> PrimitiveObjects =>
        new()
        {
            { PdfNull.Instance, nameof(PdfNull) },
            { new PdfBoolean(true), nameof(PdfBoolean) },
            { new PdfInteger(1), nameof(PdfInteger) },
            { new PdfReal(1.5), nameof(PdfReal) },
            { new PdfName("Name"), nameof(PdfName) },
            { new PdfLiteralString("Text"), nameof(PdfLiteralString) },
            { new PdfHexString(new byte[] { 0x01 }), nameof(PdfHexString) },
        };

    [Theory]
    [MemberData(nameof(PrimitiveObjects))]
    public void AcceptUsesMostSpecificOverload(
        PdfObject value,
        string expected)
    {
        var visitor = new SpecificVisitor();

        Assert.Equal(expected, value.Accept(visitor));
    }

    [Theory]
    [MemberData(nameof(PrimitiveObjects))]
    public void DefaultSpecificOverloadsDelegateToObjectFallback(
        PdfObject value,
        string expected)
    {
        var visitor = new FallbackVisitor();

        Assert.Equal(expected, value.Accept(visitor));
    }

    [Fact]
    public void SpecificVisitorOverloadRejectsNull()
    {
        var visitor = new FallbackVisitor();

        Assert.Throws<ArgumentNullException>(
            () => visitor.Visit((PdfNull)null!));
    }

    private sealed class SpecificVisitor : PdfObjectVisitor<string>
    {
        public override string Visit(PdfObject value) => nameof(PdfObject);
        public override string Visit(PdfNull value) => nameof(PdfNull);
        public override string Visit(PdfBoolean value) => nameof(PdfBoolean);
        public override string Visit(PdfInteger value) => nameof(PdfInteger);
        public override string Visit(PdfReal value) => nameof(PdfReal);
        public override string Visit(PdfName value) => nameof(PdfName);
        public override string Visit(PdfLiteralString value) =>
            nameof(PdfLiteralString);
        public override string Visit(PdfHexString value) =>
            nameof(PdfHexString);
    }

    private sealed class FallbackVisitor : PdfObjectVisitor<string>
    {
        public override string Visit(PdfObject value)
        {
            ArgumentNullException.ThrowIfNull(value);
            return value.GetType().Name;
        }
    }
}
