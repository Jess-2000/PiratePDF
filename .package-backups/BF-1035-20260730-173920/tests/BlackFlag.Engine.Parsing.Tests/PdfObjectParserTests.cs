using System.Text;
using BlackFlag.Engine.IO.RandomAccess;
using BlackFlag.Engine.Objects;
using BlackFlag.Engine.Syntax.Lexing;
using BlackFlag.Engine.Syntax.Parsing;

namespace BlackFlag.Engine.Parsing.Tests;

public sealed class PdfObjectParserTests
{
    [Fact]
    public void ConstructorRejectsNullReader()
    {
        using var source =
            new TestRandomAccessSource([]);

        Assert.Throws<ArgumentNullException>(
            () => new PdfObjectParser(
                null!,
                source));
    }

    [Fact]
    public void ConstructorRejectsNullSource()
    {
        using TestRandomAccessSource source =
            CreateSource("null");

        PdfTokenReader reader =
            CreateReader(source);

        Assert.Throws<ArgumentNullException>(
            () => new PdfObjectParser(
                reader,
                null!));
    }

    [Fact]
    public void ParseReturnsCanonicalNull()
    {
        PdfObject result =
            Parse("null");

        Assert.Same(PdfNull.Instance, result);
    }

    [Theory]
    [InlineData("true", true)]
    [InlineData("false", false)]
    public void ParseReturnsBoolean(
        string sourceText,
        bool expectedValue)
    {
        PdfBoolean result =
            Assert.IsType<PdfBoolean>(
                Parse(sourceText));

        Assert.Equal(expectedValue, result.Value);
    }

    [Theory]
    [InlineData("0", 0L)]
    [InlineData("+17", 17L)]
    [InlineData("-42", -42L)]
    [InlineData("9223372036854775807", long.MaxValue)]
    [InlineData("-9223372036854775808", long.MinValue)]
    public void ParseReturnsInteger(
        string sourceText,
        long expectedValue)
    {
        PdfInteger result =
            Assert.IsType<PdfInteger>(
                Parse(sourceText));

        Assert.Equal(expectedValue, result.Value);
    }

    [Theory]
    [InlineData("0.0", 0.0)]
    [InlineData(".5", 0.5)]
    [InlineData("5.", 5.0)]
    [InlineData("+12.25", 12.25)]
    [InlineData("-7.5", -7.5)]
    public void ParseReturnsReal(
        string sourceText,
        double expectedValue)
    {
        PdfReal result =
            Assert.IsType<PdfReal>(
                Parse(sourceText));

        Assert.Equal(expectedValue, result.Value);
    }

    [Fact]
    public void ParseDecodesNameEscapes()
    {
        PdfName result =
            Assert.IsType<PdfName>(
                Parse("/Black#20Flag#23Engine"));

        Assert.Equal(
            "Black Flag#Engine",
            result.Value);
    }

    [Fact]
    public void ParseDecodesEmptyName()
    {
        PdfName result =
            Assert.IsType<PdfName>(
                Parse("/"));

        Assert.Empty(result.Value);
    }

    [Fact]
    public void ParseDecodesLiteralStringEscapes()
    {
        PdfLiteralString result =
            Assert.IsType<PdfLiteralString>(
                Parse(@"(Line\nTab\t\(value\)\\)"));

        Assert.Equal(
            "Line\nTab\t(value)\\",
            result.Value);
    }

    [Fact]
    public void ParseDecodesLiteralStringOctalEscapes()
    {
        PdfLiteralString result =
            Assert.IsType<PdfLiteralString>(
                Parse(@"(\101\102\103)"));

        Assert.Equal("ABC", result.Value);
    }

    [Fact]
    public void ParseRemovesEscapedLineContinuation()
    {
        PdfLiteralString result =
            Assert.IsType<PdfLiteralString>(
                Parse("(Black\\\r\nFlag)"));

        Assert.Equal("BlackFlag", result.Value);
    }

    [Fact]
    public void ParseNormalizesUnescapedCarriageReturnLineEndings()
    {
        PdfLiteralString result =
            Assert.IsType<PdfLiteralString>(
                Parse("(Black\r\nFlag\rEngine)"));

        Assert.Equal(
            "Black\nFlag\nEngine",
            result.Value);
    }

    [Fact]
    public void ParseDecodesHexadecimalString()
    {
        PdfHexString result =
            Assert.IsType<PdfHexString>(
                Parse("<42 6c 61 63 6B>"));

        Assert.Equal(
            Encoding.ASCII.GetBytes("Black"),
            result.Value.ToArray());
    }

    [Fact]
    public void ParsePadsOddHexadecimalDigitWithZero()
    {
        PdfHexString result =
            Assert.IsType<PdfHexString>(
                Parse("<ABC>"));

        Assert.Equal(
            new byte[] { 0xAB, 0xC0 },
            result.Value.ToArray());
    }

    [Fact]
    public void ParseConsumesExactlyOnePrimitiveToken()
    {
        using TestRandomAccessSource source =
            CreateSource("null true");

        PdfTokenReader reader =
            CreateReader(source);

        var parser =
            new PdfObjectParser(
                reader,
                source);

        Assert.Same(PdfNull.Instance, parser.Parse());

        PdfBoolean second =
            Assert.IsType<PdfBoolean>(
                parser.Parse());

        Assert.True(second.Value);
        Assert.True(reader.EndOfFile);
    }

    [Fact]
    public void ParseRejectsUnsupportedTokenWithoutConsumingIt()
    {
        using TestRandomAccessSource source =
            CreateSource("[null]");

        PdfTokenReader reader =
            CreateReader(source);

        var parser =
            new PdfObjectParser(
                reader,
                source);

        PdfUnexpectedTokenException exception =
            Assert.Throws<PdfUnexpectedTokenException>(
                parser.Parse);

        Assert.Equal(
            PdfTokenKind.ArrayStart,
            exception.ActualToken.Kind);

        Assert.Equal(
            PdfTokenKind.ArrayStart,
            reader.Peek().Kind);
    }

    [Fact]
    public void ParseRejectsIntegerOutsideSupportedRangeWithoutConsumingIt()
    {
        const string sourceText =
            "9223372036854775808";

        using TestRandomAccessSource source =
            CreateSource(sourceText);

        PdfTokenReader reader =
            CreateReader(source);

        var parser =
            new PdfObjectParser(
                reader,
                source);

        Assert.Throws<FormatException>(
            parser.Parse);

        Assert.Equal(
            PdfTokenKind.IntegerNumber,
            reader.Peek().Kind);
    }

    private static PdfObject Parse(
        string sourceText)
    {
        using TestRandomAccessSource source =
            CreateSource(sourceText);

        PdfTokenReader reader =
            CreateReader(source);

        var parser =
            new PdfObjectParser(
                reader,
                source);

        return parser.Parse();
    }

    private static TestRandomAccessSource CreateSource(
        string sourceText)
    {
        return new TestRandomAccessSource(
            Encoding.Latin1.GetBytes(
                sourceText));
    }

    private static PdfTokenReader CreateReader(
        TestRandomAccessSource source)
    {
        var cursor =
            new RandomAccessCursor(source);

        var lexer =
            new PdfLexer(cursor);

        return new PdfTokenReader(lexer);
    }
}
