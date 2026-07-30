using BlackFlag.Engine.Syntax.Lexing;
using static BlackFlag.Engine.Syntax.Tests.Lexing.LexerTestFactory;

namespace BlackFlag.Engine.Syntax.Tests.Lexing;

public sealed class PdfLexerNumberTests
{
    [Theory]
    [InlineData("0")]
    [InlineData("1")]
    [InlineData("123")]
    [InlineData("-42")]
    [InlineData("+17")]
    public void ReadTokenRecognizesIntegerNumber(
        string source)
    {
        PdfLexer lexer =
            CreateLexer(
                source);

        PdfToken token =
            lexer.ReadToken();

        Assert.Equal(
            PdfTokenKind.IntegerNumber,
            token.Kind);

        Assert.Equal(
            0,
            token.Span.Start.Offset);

        Assert.Equal(
            source.Length,
            token.Span.End.Offset);

        Assert.Equal(
            source.Length,
            token.Span.Length);
    }

    [Theory]
    [InlineData("0.0")]
    [InlineData("3.14159")]
    [InlineData("-3.62")]
    [InlineData("+123.6")]
    [InlineData("4.")]
    [InlineData("-.002")]
    [InlineData(".5")]
    [InlineData("+.25")]
    public void ReadTokenRecognizesRealNumber(
        string source)
    {
        PdfLexer lexer =
            CreateLexer(
                source);

        PdfToken token =
            lexer.ReadToken();

        Assert.Equal(
            PdfTokenKind.RealNumber,
            token.Kind);

        Assert.Equal(
            0,
            token.Span.Start.Offset);

        Assert.Equal(
            source.Length,
            token.Span.End.Offset);

        Assert.Equal(
            source.Length,
            token.Span.Length);
    }

    [Theory]
    [InlineData(".")]
    [InlineData("+")]
    [InlineData("-")]
    [InlineData("+.")]
    [InlineData("-.")]
    public void NumberRequiresAtLeastOneDecimalDigit(
        string source)
    {
        PdfLexer lexer =
            CreateLexer(
                source);

        PdfLexerException exception =
            Assert.Throws<PdfLexerException>(
                () => lexer.ReadToken());

        Assert.Contains(
            "at least one decimal digit",
            exception.Message,
            StringComparison.OrdinalIgnoreCase);

        Assert.Equal(
            PdfSourcePosition.Start,
            exception.Position);
    }

    [Theory]
    [InlineData("3..2", 2)]
    [InlineData("12abc", 2)]
    [InlineData("--1", 1)]
    [InlineData("++1", 1)]
    [InlineData("+-1", 1)]
    [InlineData("-+1", 1)]
    public void NumberRejectsInvalidRegularByte(
        string source,
        long expectedOffset)
    {
        PdfLexer lexer =
            CreateLexer(
                source);

        PdfLexerException exception =
            Assert.Throws<PdfLexerException>(
                () => lexer.ReadToken());

        Assert.Contains(
            "in PDF number",
            exception.Message,
            StringComparison.OrdinalIgnoreCase);

        Assert.Equal(
            expectedOffset,
            exception.Position.Offset);
    }

    [Fact]
    public void NumberStopsBeforeWhiteSpace()
    {
        PdfLexer lexer =
            CreateLexer(
                "12 34");

        PdfToken first =
            lexer.ReadToken();

        PdfToken second =
            lexer.ReadToken();

        Assert.Equal(
            PdfTokenKind.IntegerNumber,
            first.Kind);

        Assert.Equal(
            0,
            first.Span.Start.Offset);

        Assert.Equal(
            2,
            first.Span.End.Offset);

        Assert.Equal(
            PdfTokenKind.IntegerNumber,
            second.Kind);

        Assert.Equal(
            3,
            second.Span.Start.Offset);

        Assert.Equal(
            5,
            second.Span.End.Offset);
    }

    [Fact]
    public void NumberStopsBeforeComment()
    {
        PdfLexer lexer =
            CreateLexer(
                "12% comment\n34");

        PdfToken first =
            lexer.ReadToken();

        PdfToken second =
            lexer.ReadToken();

        Assert.Equal(
            PdfTokenKind.IntegerNumber,
            first.Kind);

        Assert.Equal(
            0,
            first.Span.Start.Offset);

        Assert.Equal(
            2,
            first.Span.End.Offset);

        Assert.Equal(
            PdfTokenKind.IntegerNumber,
            second.Kind);

        Assert.Equal(
            12,
            second.Span.Start.Offset);

        Assert.Equal(
            14,
            second.Span.End.Offset);

        Assert.Equal(
            2,
            second.Span.Start.Line);

        Assert.Equal(
            1,
            second.Span.Start.Column);
    }

    [Fact]
    public void NumberStopsBeforeDelimiter()
    {
        PdfLexer lexer =
            CreateLexer(
                "12]");

        PdfToken number =
            lexer.ReadToken();

        PdfToken delimiter =
            lexer.ReadToken();

        Assert.Equal(
            PdfTokenKind.IntegerNumber,
            number.Kind);

        Assert.Equal(
            0,
            number.Span.Start.Offset);

        Assert.Equal(
            2,
            number.Span.End.Offset);

        Assert.Equal(
            PdfTokenKind.ArrayEnd,
            delimiter.Kind);

        Assert.Equal(
            2,
            delimiter.Span.Start.Offset);

        Assert.Equal(
            3,
            delimiter.Span.End.Offset);
    }

    [Fact]
    public void NumberPreservesPositionAfterLeadingWhiteSpace()
    {
        PdfLexer lexer =
            CreateLexer(
                "\r\n  -12.5");

        PdfToken token =
            lexer.ReadToken();

        Assert.Equal(
            PdfTokenKind.RealNumber,
            token.Kind);

        Assert.Equal(
            4,
            token.Span.Start.Offset);

        Assert.Equal(
            2,
            token.Span.Start.Line);

        Assert.Equal(
            3,
            token.Span.Start.Column);

        Assert.Equal(
            9,
            token.Span.End.Offset);

        Assert.Equal(
            2,
            token.Span.End.Line);

        Assert.Equal(
            8,
            token.Span.End.Column);
    }

    [Fact]
    public void NumberMayEqualMaximumTokenLength()
    {
        var lexer =
            new PdfLexer(
                CreateCursor(
                    "-123"),
                new PdfLexerOptions
                {
                    MaximumTokenLength = 4
                });

        PdfToken token =
            lexer.ReadToken();

        Assert.Equal(
            PdfTokenKind.IntegerNumber,
            token.Kind);

        Assert.Equal(
            4,
            token.Span.Length);
    }

    [Fact]
    public void RealNumberMayEqualMaximumTokenLength()
    {
        var lexer =
            new PdfLexer(
                CreateCursor(
                    "-1.5"),
                new PdfLexerOptions
                {
                    MaximumTokenLength = 4
                });

        PdfToken token =
            lexer.ReadToken();

        Assert.Equal(
            PdfTokenKind.RealNumber,
            token.Kind);

        Assert.Equal(
            4,
            token.Span.Length);
    }

    [Theory]
    [InlineData("-1234")]
    [InlineData("12.345")]
    public void NumberRespectsMaximumTokenLength(
        string source)
    {
        var lexer =
            new PdfLexer(
                CreateCursor(
                    source),
                new PdfLexerOptions
                {
                    MaximumTokenLength = 4
                });

        PdfLexerException exception =
            Assert.Throws<PdfLexerException>(
                () => lexer.ReadToken());

        Assert.Contains(
            "maximum of 4 bytes",
            exception.Message,
            StringComparison.OrdinalIgnoreCase);

        Assert.Equal(
            PdfSourcePosition.Start,
            exception.Position);
    }

    [Fact]
    public void SequentialNumbersRetainTheirOwnKindsAndSpans()
    {
        PdfLexer lexer =
            CreateLexer(
                "10 -2.5 +3");

        PdfToken first =
            lexer.ReadToken();

        PdfToken second =
            lexer.ReadToken();

        PdfToken third =
            lexer.ReadToken();

        PdfToken endOfFile =
            lexer.ReadToken();

        Assert.Equal(
            PdfTokenKind.IntegerNumber,
            first.Kind);

        Assert.Equal(
            0,
            first.Span.Start.Offset);

        Assert.Equal(
            2,
            first.Span.End.Offset);

        Assert.Equal(
            PdfTokenKind.RealNumber,
            second.Kind);

        Assert.Equal(
            3,
            second.Span.Start.Offset);

        Assert.Equal(
            7,
            second.Span.End.Offset);

        Assert.Equal(
            PdfTokenKind.IntegerNumber,
            third.Kind);

        Assert.Equal(
            8,
            third.Span.Start.Offset);

        Assert.Equal(
            10,
            third.Span.End.Offset);

        Assert.Equal(
            PdfTokenKind.EndOfFile,
            endOfFile.Kind);

        Assert.Equal(
            10,
            endOfFile.Span.Start.Offset);
    }
}