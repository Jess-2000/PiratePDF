using BlackFlag.Engine.Core.Objects;

namespace BlackFlag.Engine.Core.Tests.Objects;

public sealed class PdfPrimitiveObjectTests
{
    [Fact]
    public void NullUsesSharedInstance()
    {
        Assert.Same(
            PdfNull.Instance,
            PdfNull.Instance);

        Assert.Equal(
            PdfObjectKind.Null,
            PdfNull.Instance.Kind);

        Assert.Equal(
            "null",
            PdfNull.Instance.ToString());
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void BooleanFromValueReturnsExpectedInstance(
        bool value)
    {
        var result = PdfBoolean.FromValue(value);

        Assert.Equal(value, result.Value);
        Assert.Equal(
            PdfObjectKind.Boolean,
            result.Kind);

        Assert.Same(
            value ? PdfBoolean.True : PdfBoolean.False,
            result);
    }

    [Fact]
    public void IntegerStoresSignedValue()
    {
        var value = new PdfInteger(-125);

        Assert.Equal(-125, value.Value);
        Assert.Equal(
            PdfObjectKind.IntegerNumber,
            value.Kind);

        Assert.Equal("-125", value.ToString());
    }

    [Fact]
    public void RealUsesInvariantFormatting()
    {
        var value = new PdfReal(12.5);

        Assert.Equal(12.5, value.Value);
        Assert.Equal(
            PdfObjectKind.RealNumber,
            value.Kind);

        Assert.Contains(
            "12.5",
            value.ToString(),
            StringComparison.Ordinal);
    }

    [Fact]
    public void NameStoresDecodedValue()
    {
        var value = new PdfName("Type");

        Assert.Equal("Type", value.Value);
        Assert.Equal(
            PdfObjectKind.Name,
            value.Kind);

        Assert.Equal("/Type", value.ToString());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void NameRejectsInvalidValue(
        string? value)
    {
        Assert.ThrowsAny<ArgumentException>(
            () => new PdfName(value!));
    }

    [Fact]
    public void StringCopiesInputBytes()
    {
        var source = new byte[]
        {
            0x48,
            0x65,
            0x6C,
            0x6C,
            0x6F
        };

        var value = new PdfString(
            source,
            PdfStringKind.Literal);

        source[0] = 0x00;

        Assert.Equal(
            PdfObjectKind.StringValue,
            value.Kind);

        Assert.Equal(
            PdfStringKind.Literal,
            value.StringKind);

        Assert.Equal(
            new byte[]
            {
                0x48,
                0x65,
                0x6C,
                0x6C,
                0x6F
            },
            value.Value.ToArray());
    }
}