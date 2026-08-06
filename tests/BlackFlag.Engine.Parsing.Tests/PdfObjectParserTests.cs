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
            CreateSource("obj");

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
            PdfTokenKind.ObjectStart,
            exception.ActualToken.Kind);

        Assert.Equal(
            PdfTokenKind.ObjectStart,
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

    [Fact]
    public void ParseReturnsCanonicalEmptyArray()
    {
        PdfArray result =
            Assert.IsType<PdfArray>(
                Parse("[]"));

        Assert.Same(PdfArray.Empty, result);
    }

    [Fact]
    public void ParseReturnsArrayContainingPrimitiveObjects()
    {
        PdfArray result =
            Assert.IsType<PdfArray>(
                Parse("[null true 42 3.5 /Name (text) <4142>]"));

        Assert.Equal(7, result.Count);
        Assert.Same(PdfNull.Instance, result[0]);
        Assert.True(
            Assert.IsType<PdfBoolean>(result[1]).Value);
        Assert.Equal(
            42,
            Assert.IsType<PdfInteger>(result[2]).Value);
        Assert.Equal(
            3.5,
            Assert.IsType<PdfReal>(result[3]).Value);
        Assert.Equal(
            "Name",
            Assert.IsType<PdfName>(result[4]).Value);
        Assert.Equal(
            "text",
            Assert.IsType<PdfLiteralString>(result[5]).Value);
        Assert.Equal(
            Encoding.ASCII.GetBytes("AB"),
            Assert.IsType<PdfHexString>(result[6]).Value.ToArray());
    }

    [Fact]
    public void ParseReturnsNestedArrays()
    {
        PdfArray result =
            Assert.IsType<PdfArray>(
                Parse("[1 [2 [3]] 4]"));

        Assert.Equal(3, result.Count);
        Assert.Equal(
            1,
            Assert.IsType<PdfInteger>(result[0]).Value);

        PdfArray second =
            Assert.IsType<PdfArray>(result[1]);

        Assert.Equal(
            2,
            Assert.IsType<PdfInteger>(second[0]).Value);

        PdfArray third =
            Assert.IsType<PdfArray>(second[1]);

        Assert.Equal(
            3,
            Assert.IsType<PdfInteger>(third[0]).Value);
        Assert.Equal(
            4,
            Assert.IsType<PdfInteger>(result[2]).Value);
    }

    [Fact]
    public void ParseReturnsCanonicalEmptyDictionary()
    {
        PdfDictionary result =
            Assert.IsType<PdfDictionary>(
                Parse("<<>>"));

        Assert.Same(PdfDictionary.Empty, result);
    }

    [Fact]
    public void ParseReturnsDictionaryContainingPrimitiveObjects()
    {
        PdfDictionary result =
            Assert.IsType<PdfDictionary>(
                Parse(
                    "<< /Null null /Flag true /Count 5 "
                    + "/Ratio 2.5 /Label (Black Flag) >>"));

        Assert.Equal(5, result.Count);
        Assert.Same(
            PdfNull.Instance,
            result[new PdfName("Null")]);
        Assert.True(
            Assert.IsType<PdfBoolean>(
                result[new PdfName("Flag")]).Value);
        Assert.Equal(
            5,
            Assert.IsType<PdfInteger>(
                result[new PdfName("Count")]).Value);
        Assert.Equal(
            2.5,
            Assert.IsType<PdfReal>(
                result[new PdfName("Ratio")]).Value);
        Assert.Equal(
            "Black Flag",
            Assert.IsType<PdfLiteralString>(
                result[new PdfName("Label")]).Value);
    }

    [Fact]
    public void ParseReturnsNestedDictionaries()
    {
        PdfDictionary result =
            Assert.IsType<PdfDictionary>(
                Parse("<< /Outer << /Inner << /Value 17 >> >> >>"));

        PdfDictionary outer =
            Assert.IsType<PdfDictionary>(
                result[new PdfName("Outer")]);

        PdfDictionary inner =
            Assert.IsType<PdfDictionary>(
                outer[new PdfName("Inner")]);

        Assert.Equal(
            17,
            Assert.IsType<PdfInteger>(
                inner[new PdfName("Value")]).Value);
    }

    [Fact]
    public void ParseReturnsMixedNestedCollections()
    {
        PdfDictionary result =
            Assert.IsType<PdfDictionary>(
                Parse(
                    "<< /Items [1 << /Enabled true >> [2 3]] "
                    + "/Metadata << /Names [/A /B] >> >>"));

        PdfArray items =
            Assert.IsType<PdfArray>(
                result[new PdfName("Items")]);

        PdfDictionary itemDictionary =
            Assert.IsType<PdfDictionary>(items[1]);

        Assert.True(
            Assert.IsType<PdfBoolean>(
                itemDictionary[new PdfName("Enabled")]).Value);

        PdfArray nestedItems =
            Assert.IsType<PdfArray>(items[2]);

        Assert.Equal(
            3,
            Assert.IsType<PdfInteger>(nestedItems[1]).Value);

        PdfDictionary metadata =
            Assert.IsType<PdfDictionary>(
                result[new PdfName("Metadata")]);

        PdfArray names =
            Assert.IsType<PdfArray>(
                metadata[new PdfName("Names")]);

        Assert.Equal(
            "B",
            Assert.IsType<PdfName>(names[1]).Value);
    }

    [Fact]
    public void ParseUsesLastDictionaryValueForDuplicateKey()
    {
        PdfDictionary result =
            Assert.IsType<PdfDictionary>(
                Parse("<< /Value 1 /Value 2 /Value 3 >>"));

        Assert.Single(result);
        Assert.Equal(
            3,
            Assert.IsType<PdfInteger>(
                result[new PdfName("Value")]).Value);
    }

    [Fact]
    public void ParseConsumesExactlyOneCollectionObject()
    {
        using TestRandomAccessSource source =
            CreateSource("[null] << /Value 9 >> true");

        PdfTokenReader reader =
            CreateReader(source);

        var parser =
            new PdfObjectParser(
                reader,
                source);

        PdfArray first =
            Assert.IsType<PdfArray>(parser.Parse());

        Assert.Single(first);
        Assert.Equal(
            PdfTokenKind.DictionaryStart,
            reader.Peek().Kind);

        PdfDictionary second =
            Assert.IsType<PdfDictionary>(parser.Parse());

        Assert.Equal(
            9,
            Assert.IsType<PdfInteger>(
                second[new PdfName("Value")]).Value);
        Assert.Equal(
            PdfTokenKind.BooleanTrue,
            reader.Peek().Kind);

        Assert.True(
            Assert.IsType<PdfBoolean>(parser.Parse()).Value);
        Assert.True(reader.EndOfFile);
    }

    [Fact]
    public void ParseRejectsInvalidDictionaryKeyWithoutConsumingIt()
    {
        using TestRandomAccessSource source =
            CreateSource("<< 17 null >>");

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
            PdfTokenKind.Name,
            exception.ExpectedKind);
        Assert.Equal(
            PdfTokenKind.IntegerNumber,
            exception.ActualToken.Kind);
        Assert.Equal(
            PdfTokenKind.IntegerNumber,
            reader.Peek().Kind);
    }

    [Fact]
    public void ParseRejectsUnterminatedArrayAtEndOfFile()
    {
        using TestRandomAccessSource source =
            CreateSource("[null true");

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
            PdfTokenKind.ArrayEnd,
            exception.ExpectedKind);
        Assert.Equal(
            PdfTokenKind.EndOfFile,
            exception.ActualToken.Kind);
        Assert.Equal(
            PdfTokenKind.EndOfFile,
            reader.Peek().Kind);
    }

    [Fact]
    public void ParseRejectsUnterminatedDictionaryAtEndOfFile()
    {
        using TestRandomAccessSource source =
            CreateSource("<< /Value 1");

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
            PdfTokenKind.DictionaryEnd,
            exception.ExpectedKind);
        Assert.Equal(
            PdfTokenKind.EndOfFile,
            exception.ActualToken.Kind);
        Assert.Equal(
            PdfTokenKind.EndOfFile,
            reader.Peek().Kind);
    }

    [Fact]
    public void ParseRejectsDictionaryWithoutValue()
    {
        using TestRandomAccessSource source =
            CreateSource("<< /Value >>");

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
            PdfTokenKind.DictionaryEnd,
            exception.ActualToken.Kind);
        Assert.Equal(
            PdfTokenKind.DictionaryEnd,
            reader.Peek().Kind);
    }

    [Theory]
    [InlineData("[null", PdfTokenKind.ArrayEnd)]
    [InlineData("<< /Array [1 2", PdfTokenKind.ArrayEnd)]
    [InlineData("<< /Nested << /Value 1", PdfTokenKind.DictionaryEnd)]
    public void ParseReportsInnermostUnterminatedCollection(
        string sourceText,
        PdfTokenKind expectedKind)
    {
        using TestRandomAccessSource source =
            CreateSource(sourceText);

        PdfTokenReader reader =
            CreateReader(source);

        var parser =
            new PdfObjectParser(
                reader,
                source);

        PdfUnexpectedTokenException exception =
            Assert.Throws<PdfUnexpectedTokenException>(
                parser.Parse);

        Assert.Equal(expectedKind, exception.ExpectedKind);
        Assert.Equal(
            PdfTokenKind.EndOfFile,
            exception.ActualToken.Kind);
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

