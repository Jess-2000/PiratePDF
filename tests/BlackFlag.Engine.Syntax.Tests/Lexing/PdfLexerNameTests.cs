using BlackFlag.Engine.Syntax.Lexing;
using static BlackFlag.Engine.Syntax.Tests.Lexing.LexerTestFactory;

namespace BlackFlag.Engine.Syntax.Tests.Lexing;

public sealed class PdfLexerNameTests
{
    [Fact]
    public void ReadTokenRecognizesName()
    {
        PdfLexer lexer =
            CreateLexer(
                "/Type");

        PdfToken token =
            lexer.ReadToken();

        Assert.Equal(
            PdfTokenKind.Name,
            token.Kind);

        Assert.Equal(
            0,
            token.Span.Start.Offset);

        Assert.Equal(
            5,
            token.Span.End.Offset);
    }

    [Fact]
    public void ReadTokenRecognizesEmptyName()
    {
        PdfLexer lexer =
            CreateLexer(
                "/ ");

        PdfToken token =
            lexer.ReadToken();

        Assert.Equal(
            PdfTokenKind.Name,
            token.Kind);

        Assert.Equal(
            0,
            token.Span.Start.Offset);

        Assert.Equal(
            1,
            token.Span.End.Offset);
    }

    [Fact]
    public void ReadTokenAllowsHexadecimalEscapeInName()
    {
        PdfLexer lexer =
            CreateLexer(
                "/A#20Name");

        PdfToken token =
            lexer.ReadToken();

        Assert.Equal(
            PdfTokenKind.Name,
            token.Kind);

        Assert.Equal(
            0,
            token.Span.Start.Offset);

        Assert.Equal(
            9,
            token.Span.End.Offset);
    }

    [Fact]
    public void ReadTokenAllowsLowercaseHexadecimalEscapeInName()
    {
        PdfLexer lexer =
            CreateLexer(
                "/A#ffName");

        PdfToken token =
            lexer.ReadToken();

        Assert.Equal(
            PdfTokenKind.Name,
            token.Kind);

        Assert.Equal(
            9,
            token.Span.Length);
    }

    [Fact]
    public void NameStopsBeforeWhiteSpace()
    {
        PdfLexer lexer =
            CreateLexer(
                "/Type /Font");

        PdfToken first =
            lexer.ReadToken();

        PdfToken second =
            lexer.ReadToken();

        Assert.Equal(
            PdfTokenKind.Name,
            first.Kind);

        Assert.Equal(
            0,
            first.Span.Start.Offset);

        Assert.Equal(
            5,
            first.Span.End.Offset);

        Assert.Equal(
            PdfTokenKind.Name,
            second.Kind);

        Assert.Equal(
            6,
            second.Span.Start.Offset);

        Assert.Equal(
            11,
            second.Span.End.Offset);
    }

    [Fact]
    public void NameStopsBeforeDelimiter()
    {
        PdfLexer lexer =
            CreateLexer(
                "/Type]");

        PdfToken name =
            lexer.ReadToken();

        PdfToken delimiter =
            lexer.ReadToken();

        Assert.Equal(
            PdfTokenKind.Name,
            name.Kind);

        Assert.Equal(
            5,
            name.Span.End.Offset);

        Assert.Equal(
            PdfTokenKind.ArrayEnd,
            delimiter.Kind);

        Assert.Equal(
            5,
            delimiter.Span.Start.Offset);
    }

    [Fact]
    public void NameEscapeRejectsInvalidFirstDigit()
    {
        PdfLexer lexer =
            CreateLexer(
                "/A#G0");

        PdfLexerException exception =
            Assert.Throws<PdfLexerException>(
                () => lexer.ReadToken());

        Assert.Contains(
            "invalid hexadecimal digit",
            exception.Message,
            StringComparison.OrdinalIgnoreCase);

        Assert.Equal(
            3,
            exception.Position.Offset);
    }

    [Fact]
    public void NameEscapeRejectsInvalidSecondDigit()
    {
        PdfLexer lexer =
            CreateLexer(
                "/A#0G");

        PdfLexerException exception =
            Assert.Throws<PdfLexerException>(
                () => lexer.ReadToken());

        Assert.Contains(
            "invalid hexadecimal digit",
            exception.Message,
            StringComparison.OrdinalIgnoreCase);

        Assert.Equal(
            4,
            exception.Position.Offset);
    }

    [Theory]
    [InlineData("/A#")]
    [InlineData("/A#0")]
    public void NameEscapeRejectsIncompleteEscape(
        string source)
    {
        PdfLexer lexer =
            CreateLexer(
                source);

        PdfLexerException exception =
            Assert.Throws<PdfLexerException>(
                () => lexer.ReadToken());

        Assert.Contains(
            "incomplete",
            exception.Message,
            StringComparison.OrdinalIgnoreCase);

        Assert.Equal(
            2,
            exception.Position.Offset);
    }

    [Fact]
    public void NameRespectsMaximumTokenLength()
    {
        var lexer =
            new PdfLexer(
                CreateCursor(
                    "/abcd"),
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
    public void NameMayEqualMaximumTokenLength()
    {
        var lexer =
            new PdfLexer(
                CreateCursor(
                    "/abc"),
                new PdfLexerOptions
                {
                    MaximumTokenLength = 4
                });

        PdfToken token =
            lexer.ReadToken();

        Assert.Equal(
            PdfTokenKind.Name,
            token.Kind);

        Assert.Equal(
            4,
            token.Span.Length);
    }
}
