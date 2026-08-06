using BlackFlag.Engine.Document.Objects;
using BlackFlag.Engine.Document.Objects.Primitives;

namespace BlackFlag.Engine.Document.Tests.Objects.Primitives;

public sealed class PdfPrimitiveObjectTests
{
    [Fact]
    public void PdfNull_Instance_ReturnsSameObject()
    {
        Assert.Same(
            PdfNull.Instance,
            PdfNull.Instance);

        Assert.Equal(
            PdfObjectKind.NullValue,
            PdfNull.Instance.Kind);

        Assert.Equal(
            "null",
            PdfNull.Instance.ToString());
    }

    [Theory]
    [InlineData(true, "true")]
    [InlineData(false, "false")]
    public void PdfBoolean_WithValue_PreservesValue(
        bool value,
        string expectedText)
    {
        var pdfBoolean = new PdfBoolean(value);

        Assert.Equal(
            value,
            pdfBoolean.Value);

        Assert.Equal(
            PdfObjectKind.LogicalValue,
            pdfBoolean.Kind);

        Assert.Equal(
            expectedText,
            pdfBoolean.ToString());
    }

    [Fact]
    public void PdfInteger_WithValue_UsesInvariantFormatting()
    {
        var value = new PdfInteger(-123456789);

        Assert.Equal(
            -123456789,
            value.Value);

        Assert.Equal(
            "-123456789",
            value.ToString());

        Assert.Equal(
            PdfObjectKind.WholeNumber,
            value.Kind);
    }

    [Fact]
    public void PdfReal_WithFiniteValue_PreservesValue()
    {
        var value = new PdfReal(12.5);

        Assert.Equal(
            12.5,
            value.Value);

        Assert.Equal(
            PdfObjectKind.RealNumber,
            value.Kind);

        Assert.Equal(
            "12.5",
            value.ToString());
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void PdfReal_WithNonFiniteValue_Throws(
        double value)
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new PdfReal(value));
    }

    [Fact]
    public void PdfName_FromLatin1_PreservesBytes()
    {
        var name = PdfName.FromLatin1(
            "Type");

        Assert.Equal(
            PdfObjectKind.NameValue,
            name.Kind);

        Assert.Equal(
            "Type",
            name.GetLatin1Text());

        Assert.Equal(
            "/Type",
            name.ToString());

        Assert.Equal(
            [84, 121, 112, 101],
            name.ToByteArray());
    }

    [Fact]
    public void PdfName_Constructor_DefensivelyCopiesInput()
    {
        var bytes = new byte[]
        {
            84,
            121,
            112,
            101
        };

        var name = new PdfName(bytes);

        bytes[0] = 88;

        Assert.Equal(
            "Type",
            name.GetLatin1Text());

        var returnedBytes = name.ToByteArray();
        returnedBytes[0] = 88;

        Assert.Equal(
            "Type",
            name.GetLatin1Text());
    }

    [Fact]
    public void PdfName_WithEmptyValue_Throws()
    {
        Assert.Throws<ArgumentException>(
            () => new PdfName(
                ReadOnlySpan<byte>.Empty));
    }

    [Fact]
    public void PdfName_WithMatchingBytes_IsValueEqual()
    {
        var first = PdfName.FromLatin1("Catalog");
        var second = PdfName.FromLatin1("Catalog");

        Assert.Equal(first, second);
        Assert.True(first == second);
        Assert.Equal(
            first.GetHashCode(),
            second.GetHashCode());
    }

    [Fact]
    public void PdfString_WithEmptyValue_IsValid()
    {
        var value = new PdfString(
            ReadOnlySpan<byte>.Empty);

        Assert.True(value.IsEmpty);
        Assert.Equal(0, value.Length);
        Assert.Equal(
            PdfStringFormat.Literal,
            value.Format);
    }

    [Fact]
    public void PdfString_Constructor_DefensivelyCopiesInput()
    {
        var bytes = new byte[]
        {
            80,
            68,
            70
        };

        var value = new PdfString(
            bytes,
            PdfStringFormat.Hexadecimal);

        bytes[0] = 88;

        Assert.Equal(
            "PDF",
            value.GetLatin1Text());

        var returnedBytes = value.ToByteArray();
        returnedBytes[0] = 88;

        Assert.Equal(
            "PDF",
            value.GetLatin1Text());

        Assert.Equal(
            PdfStringFormat.Hexadecimal,
            value.Format);
    }

    [Fact]
    public void PdfString_WithDifferentFormats_IsNotEqual()
    {
        var literal = PdfString.FromLatin1(
            "PiratePDF",
            PdfStringFormat.Literal);

        var hexadecimal = PdfString.FromLatin1(
            "PiratePDF",
            PdfStringFormat.Hexadecimal);

        Assert.NotEqual(
            literal,
            hexadecimal);
    }
}
