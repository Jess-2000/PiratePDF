using BlackFlag.Engine.Syntax.Lexing;

namespace BlackFlag.Engine.Syntax.Tests.Lexing;

public sealed class PdfCharacterClassifierTests
{
    public static TheoryData<byte> WhiteSpaceBytes =>
        new()
        {
            0x00,
            0x09,
            0x0A,
            0x0C,
            0x0D,
            0x20
        };

    public static TheoryData<byte> DelimiterBytes =>
        new()
        {
            (byte)'(',
            (byte)')',
            (byte)'<',
            (byte)'>',
            (byte)'[',
            (byte)']',
            (byte)'{',
            (byte)'}',
            (byte)'/',
            (byte)'%'
        };

    [Theory]
    [MemberData(nameof(WhiteSpaceBytes))]
    public void IsWhiteSpaceRecognizesPdfWhiteSpace(
        byte value)
    {
        Assert.True(
            PdfCharacterClassifier.IsWhiteSpace(value));
    }

    [Theory]
    [InlineData((byte)'A')]
    [InlineData((byte)'0')]
    [InlineData((byte)'/')]
    [InlineData(0xFF)]
    public void IsWhiteSpaceRejectsNonWhiteSpace(
        byte value)
    {
        Assert.False(
            PdfCharacterClassifier.IsWhiteSpace(value));
    }

    [Theory]
    [MemberData(nameof(DelimiterBytes))]
    public void IsDelimiterRecognizesPdfDelimiters(
        byte value)
    {
        Assert.True(
            PdfCharacterClassifier.IsDelimiter(value));
    }

    [Theory]
    [InlineData((byte)'A')]
    [InlineData((byte)'0')]
    [InlineData((byte)' ')]
    [InlineData(0xFF)]
    public void IsDelimiterRejectsNonDelimiters(
        byte value)
    {
        Assert.False(
            PdfCharacterClassifier.IsDelimiter(value));
    }

    [Theory]
    [InlineData((byte)'0')]
    [InlineData((byte)'5')]
    [InlineData((byte)'9')]
    public void IsDecimalDigitRecognizesDigits(
        byte value)
    {
        Assert.True(
            PdfCharacterClassifier.IsDecimalDigit(value));
    }

    [Theory]
    [InlineData((byte)'/')]
    [InlineData((byte)':')]
    [InlineData((byte)'A')]
    public void IsDecimalDigitRejectsNonDigits(
        byte value)
    {
        Assert.False(
            PdfCharacterClassifier.IsDecimalDigit(value));
    }

    [Theory]
    [InlineData((byte)'0')]
    [InlineData((byte)'9')]
    [InlineData((byte)'A')]
    [InlineData((byte)'F')]
    [InlineData((byte)'a')]
    [InlineData((byte)'f')]
    public void IsHexadecimalDigitRecognizesHexadecimalDigits(
        byte value)
    {
        Assert.True(
            PdfCharacterClassifier.IsHexadecimalDigit(value));
    }

    [Theory]
    [InlineData((byte)'G')]
    [InlineData((byte)'g')]
    [InlineData((byte)'/')]
    public void IsHexadecimalDigitRejectsOtherBytes(
        byte value)
    {
        Assert.False(
            PdfCharacterClassifier.IsHexadecimalDigit(value));
    }

    [Theory]
    [InlineData((byte)' ', true)]
    [InlineData((byte)'/', true)]
    [InlineData((byte)'%', true)]
    [InlineData((byte)'A', false)]
    [InlineData((byte)'7', false)]
    [InlineData(0xFF, false)]
    public void IsTokenBoundaryReturnsExpectedValue(
        byte value,
        bool expected)
    {
        Assert.Equal(
            expected,
            PdfCharacterClassifier.IsTokenBoundary(value));
    }

    [Theory]
    [InlineData((byte)'A', true)]
    [InlineData((byte)'7', true)]
    [InlineData(0xFF, true)]
    [InlineData((byte)' ', false)]
    [InlineData((byte)'/', false)]
    public void IsRegularReturnsExpectedValue(
        byte value,
        bool expected)
    {
        Assert.Equal(
            expected,
            PdfCharacterClassifier.IsRegular(value));
    }

    [Theory]
    [InlineData((byte)'\r', true)]
    [InlineData((byte)'\n', true)]
    [InlineData((byte)' ', false)]
    [InlineData((byte)'A', false)]
    public void IsEndOfLineReturnsExpectedValue(
        byte value,
        bool expected)
    {
        Assert.Equal(
            expected,
            PdfCharacterClassifier.IsEndOfLine(value));
    }

    [Theory]
    [InlineData((byte)'0', 0)]
    [InlineData((byte)'9', 9)]
    [InlineData((byte)'A', 10)]
    [InlineData((byte)'F', 15)]
    [InlineData((byte)'a', 10)]
    [InlineData((byte)'f', 15)]
    public void GetHexadecimalValueReturnsExpectedValue(
        byte value,
        int expected)
    {
        Assert.Equal(
            expected,
            PdfCharacterClassifier.GetHexadecimalValue(value));
    }

    [Theory]
    [InlineData((byte)'G')]
    [InlineData((byte)'g')]
    [InlineData((byte)'/')]
    public void GetHexadecimalValueRejectsNonHexadecimalByte(
        byte value)
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => PdfCharacterClassifier.GetHexadecimalValue(value));
    }
}