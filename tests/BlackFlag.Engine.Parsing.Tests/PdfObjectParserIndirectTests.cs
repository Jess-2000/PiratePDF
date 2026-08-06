using System.Text;
using BlackFlag.Engine.IO.RandomAccess;
using BlackFlag.Engine.Objects;
using BlackFlag.Engine.Syntax.Lexing;
using BlackFlag.Engine.Syntax.Parsing;

namespace BlackFlag.Engine.Parsing.Tests;

public sealed class PdfObjectParserIndirectTests
{
    [Theory]
    [InlineData("1 0 R", 1, 0)]
    [InlineData("15 2 R", 15, 2)]
    [InlineData("2147483647 65535 R", int.MaxValue, 65535)]
    public void ParseReturnsIndirectReference(
        string sourceText,
        int expectedObjectNumber,
        int expectedGenerationNumber)
    {
        PdfIndirectReference result =
            Assert.IsType<PdfIndirectReference>(
                ParseObject(sourceText));

        Assert.Equal(
            expectedObjectNumber,
            result.ObjectNumber);

        Assert.Equal(
            expectedGenerationNumber,
            result.GenerationNumber);
    }

    [Fact]
    public void ParseConsumesExactlyOneIndirectReference()
    {
        using TestRandomAccessSource source =
            CreateSource("12 0 R true");

        PdfTokenReader reader =
            CreateReader(source);

        var parser =
            new PdfObjectParser(
                reader,
                source);

        PdfIndirectReference reference =
            Assert.IsType<PdfIndirectReference>(
                parser.Parse());

        Assert.Equal(12, reference.ObjectNumber);
        Assert.Equal(0, reference.GenerationNumber);
        Assert.Equal(
            PdfTokenKind.BooleanTrue,
            reader.Peek().Kind);

        Assert.True(
            Assert.IsType<PdfBoolean>(
                parser.Parse()).Value);

        Assert.True(reader.EndOfFile);
    }

    [Fact]
    public void ParseReturnsArrayContainingIndirectReferences()
    {
        PdfArray result =
            Assert.IsType<PdfArray>(
                ParseObject("[1 0 R 2 3 R]"));

        Assert.Equal(2, result.Count);

        PdfIndirectReference first =
            Assert.IsType<PdfIndirectReference>(
                result[0]);

        PdfIndirectReference second =
            Assert.IsType<PdfIndirectReference>(
                result[1]);

        Assert.Equal(1, first.ObjectNumber);
        Assert.Equal(0, first.GenerationNumber);
        Assert.Equal(2, second.ObjectNumber);
        Assert.Equal(3, second.GenerationNumber);
    }

    [Fact]
    public void ParseReturnsDictionaryContainingIndirectReference()
    {
        PdfDictionary result =
            Assert.IsType<PdfDictionary>(
                ParseObject(
                    "<< /Parent 7 0 R /Metadata 9 4 R >>"));

        PdfIndirectReference parent =
            Assert.IsType<PdfIndirectReference>(
                result[new PdfName("Parent")]);

        PdfIndirectReference metadata =
            Assert.IsType<PdfIndirectReference>(
                result[new PdfName("Metadata")]);

        Assert.Equal(7, parent.ObjectNumber);
        Assert.Equal(0, parent.GenerationNumber);
        Assert.Equal(9, metadata.ObjectNumber);
        Assert.Equal(4, metadata.GenerationNumber);
    }

    [Theory]
    [InlineData("0 0 R")]
    [InlineData("-1 0 R")]
    [InlineData("2147483648 0 R")]
    public void ParseRejectsInvalidIndirectReferenceObjectNumber(
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

        Assert.Throws<FormatException>(
            parser.Parse);

        Assert.Equal(
            PdfTokenKind.IntegerNumber,
            reader.Peek().Kind);
    }

    [Theory]
    [InlineData("1 -1 R")]
    [InlineData("1 65536 R")]
    [InlineData("1 2147483648 R")]
    public void ParseRejectsInvalidIndirectReferenceGenerationNumber(
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

        Assert.Throws<FormatException>(
            parser.Parse);

        Assert.Equal(
            PdfTokenKind.IntegerNumber,
            reader.Peek().Kind);
    }

    [Fact]
    public void ParseTreatsTwoIntegersWithoutReferenceMarkerAsSeparateObjects()
    {
        using TestRandomAccessSource source =
            CreateSource("12 0 true");

        PdfTokenReader reader =
            CreateReader(source);

        var parser =
            new PdfObjectParser(
                reader,
                source);

        PdfInteger first =
            Assert.IsType<PdfInteger>(
                parser.Parse());

        PdfInteger second =
            Assert.IsType<PdfInteger>(
                parser.Parse());

        Assert.Equal(12, first.Value);
        Assert.Equal(0, second.Value);
        Assert.Equal(
            PdfTokenKind.BooleanTrue,
            reader.Peek().Kind);
    }

    [Theory]
    [InlineData(
        "12 0 obj null endobj",
        12,
        0)]
    [InlineData(
        "2147483647 65535 obj true endobj",
        int.MaxValue,
        65535)]
    public void ParseIndirectObjectReturnsHeaderAndPrimitiveValue(
        string sourceText,
        int expectedObjectNumber,
        int expectedGenerationNumber)
    {
        PdfIndirectObject result =
            ParseIndirectObject(sourceText);

        Assert.Equal(
            expectedObjectNumber,
            result.ObjectNumber);

        Assert.Equal(
            expectedGenerationNumber,
            result.GenerationNumber);
    }

    [Fact]
    public void ParseIndirectObjectReturnsCollectionValue()
    {
        PdfIndirectObject result =
            ParseIndirectObject(
                "18 2 obj "
                + "<< /Kids [3 0 R 4 1 R] /Enabled true >> "
                + "endobj");

        Assert.Equal(18, result.ObjectNumber);
        Assert.Equal(2, result.GenerationNumber);

        PdfDictionary dictionary =
            Assert.IsType<PdfDictionary>(
                result.Value);

        PdfArray kids =
            Assert.IsType<PdfArray>(
                dictionary[new PdfName("Kids")]);

        Assert.Equal(2, kids.Count);

        PdfIndirectReference first =
            Assert.IsType<PdfIndirectReference>(
                kids[0]);

        Assert.Equal(3, first.ObjectNumber);
        Assert.Equal(0, first.GenerationNumber);

        Assert.True(
            Assert.IsType<PdfBoolean>(
                dictionary[new PdfName("Enabled")]).Value);
    }

    [Fact]
    public void ParseIndirectObjectConsumesExactlyOneDeclaration()
    {
        using TestRandomAccessSource source =
            CreateSource(
                "5 0 obj (value) endobj 9 0 R");

        PdfTokenReader reader =
            CreateReader(source);

        var parser =
            new PdfObjectParser(
                reader,
                source);

        PdfIndirectObject indirectObject =
            parser.ParseIndirectObject();

        Assert.Equal(5, indirectObject.ObjectNumber);
        Assert.Equal(
            "value",
            Assert.IsType<PdfLiteralString>(
                indirectObject.Value).Value);

        PdfIndirectReference reference =
            Assert.IsType<PdfIndirectReference>(
                parser.Parse());

        Assert.Equal(9, reference.ObjectNumber);
        Assert.Equal(0, reference.GenerationNumber);
        Assert.True(reader.EndOfFile);
    }

    [Fact]
    public void ParseIndirectObjectRejectsMissingObjectNumberWithoutConsumption()
    {
        using TestRandomAccessSource source =
            CreateSource("true 0 obj null endobj");

        PdfTokenReader reader =
            CreateReader(source);

        var parser =
            new PdfObjectParser(
                reader,
                source);

        PdfUnexpectedTokenException exception =
            Assert.Throws<PdfUnexpectedTokenException>(
                parser.ParseIndirectObject);

        Assert.Equal(
            PdfTokenKind.IntegerNumber,
            exception.ExpectedKind);

        Assert.Equal(
            PdfTokenKind.BooleanTrue,
            exception.ActualToken.Kind);

        Assert.Equal(
            PdfTokenKind.BooleanTrue,
            reader.Peek().Kind);
    }

    [Fact]
    public void ParseIndirectObjectRejectsMissingGenerationNumberWithoutConsumption()
    {
        using TestRandomAccessSource source =
            CreateSource("5 true obj null endobj");

        PdfTokenReader reader =
            CreateReader(source);

        var parser =
            new PdfObjectParser(
                reader,
                source);

        PdfUnexpectedTokenException exception =
            Assert.Throws<PdfUnexpectedTokenException>(
                parser.ParseIndirectObject);

        Assert.Equal(
            PdfTokenKind.IntegerNumber,
            exception.ExpectedKind);

        Assert.Equal(
            PdfTokenKind.BooleanTrue,
            exception.ActualToken.Kind);

        Assert.Equal(
            PdfTokenKind.IntegerNumber,
            reader.Peek().Kind);
    }

    [Fact]
    public void ParseIndirectObjectRejectsMissingObjectStartWithoutConsumption()
    {
        using TestRandomAccessSource source =
            CreateSource("5 0 null");

        PdfTokenReader reader =
            CreateReader(source);

        var parser =
            new PdfObjectParser(
                reader,
                source);

        PdfUnexpectedTokenException exception =
            Assert.Throws<PdfUnexpectedTokenException>(
                parser.ParseIndirectObject);

        Assert.Equal(
            PdfTokenKind.ObjectStart,
            exception.ExpectedKind);

        Assert.Equal(
            PdfTokenKind.Null,
            exception.ActualToken.Kind);

        Assert.Equal(
            PdfTokenKind.IntegerNumber,
            reader.Peek().Kind);
    }

    [Theory]
    [InlineData("0 0 obj null endobj")]
    [InlineData("-1 0 obj null endobj")]
    [InlineData("2147483648 0 obj null endobj")]
    public void ParseIndirectObjectRejectsInvalidObjectNumberWithoutConsumption(
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

        Assert.Throws<FormatException>(
            parser.ParseIndirectObject);

        Assert.Equal(
            PdfTokenKind.IntegerNumber,
            reader.Peek().Kind);
    }

    [Theory]
    [InlineData("1 -1 obj null endobj")]
    [InlineData("1 65536 obj null endobj")]
    [InlineData("1 2147483648 obj null endobj")]
    public void ParseIndirectObjectRejectsInvalidGenerationWithoutConsumption(
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

        Assert.Throws<FormatException>(
            parser.ParseIndirectObject);

        Assert.Equal(
            PdfTokenKind.IntegerNumber,
            reader.Peek().Kind);
    }

    [Fact]
    public void ParseIndirectObjectRejectsMissingValue()
    {
        using TestRandomAccessSource source =
            CreateSource("5 0 obj endobj");

        PdfTokenReader reader =
            CreateReader(source);

        var parser =
            new PdfObjectParser(
                reader,
                source);

        PdfUnexpectedTokenException exception =
            Assert.Throws<PdfUnexpectedTokenException>(
                parser.ParseIndirectObject);

        Assert.Equal(
            PdfTokenKind.ObjectEnd,
            exception.ActualToken.Kind);

        Assert.Equal(
            PdfTokenKind.ObjectEnd,
            reader.Peek().Kind);
    }

    [Fact]
    public void ParseIndirectObjectRejectsMissingObjectEnd()
    {
        using TestRandomAccessSource source =
            CreateSource("5 0 obj null");

        PdfTokenReader reader =
            CreateReader(source);

        var parser =
            new PdfObjectParser(
                reader,
                source);

        PdfUnexpectedTokenException exception =
            Assert.Throws<PdfUnexpectedTokenException>(
                parser.ParseIndirectObject);

        Assert.Equal(
            PdfTokenKind.ObjectEnd,
            exception.ExpectedKind);

        Assert.Equal(
            PdfTokenKind.EndOfFile,
            exception.ActualToken.Kind);

        Assert.Equal(
            PdfTokenKind.EndOfFile,
            reader.Peek().Kind);
    }

    private static PdfObject ParseObject(
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

    private static PdfIndirectObject ParseIndirectObject(
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

        return parser.ParseIndirectObject();
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

