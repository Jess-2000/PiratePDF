using BlackFlag.Engine.Syntax.Lexing;
using static BlackFlag.Engine.Syntax.Tests.Lexing.LexerTestFactory;

namespace BlackFlag.Engine.Syntax.Tests.Lexing;

public sealed class PdfLexerLiteralStringTests
{
    [Theory]
    [InlineData("()")]
    [InlineData("(Hello)")]
    [InlineData("(Hello world)")]
    public void ReadTokenRecognizesLiteralString(
        string source)
    {
        PdfLexer lexer =
            CreateLexer(
                source);

        PdfToken token =
            lexer.ReadToken();

        Assert.Equal(
            PdfTokenKind.LiteralString,
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
    [InlineData("(outer (inner) outer)")]
    [InlineData("(a (b (c) b) a)")]
    [InlineData("((nested))")]
    public void LiteralStringSupportsNestedParentheses(
        string source)
    {
        PdfLexer lexer =
            CreateLexer(
                source);

        PdfToken token =
            lexer.ReadToken();

        Assert.Equal(
            PdfTokenKind.LiteralString,
            token.Kind);

        Assert.Equal(
            source.Length,
            token.Span.Length);
    }

    [Theory]
    [InlineData(@"(\()")]
    [InlineData(@"(\))")]
    [InlineData(@"(outer \(not nested\) outer)")]
    public void EscapedParenthesesDoNotChangeNestingDepth(
        string source)
    {
        PdfLexer lexer =
            CreateLexer(
                source);

        PdfToken token =
            lexer.ReadToken();

        Assert.Equal(
            PdfTokenKind.LiteralString,
            token.Kind);

        Assert.Equal(
            source.Length,
            token.Span.Length);
    }

    [Theory]
    [InlineData(@"(\n\r\t\b\f)")]
    [InlineData(@"(\\)")]
    [InlineData(@"(\q)")]
    public void LiteralStringAcceptsBackslashEscapes(
        string source)
    {
        PdfLexer lexer =
            CreateLexer(
                source);

        PdfToken token =
            lexer.ReadToken();

        Assert.Equal(
            PdfTokenKind.LiteralString,
            token.Kind);

        Assert.Equal(
            source.Length,
            token.Span.Length);
    }

    [Theory]
    [InlineData(@"(\1)")]
    [InlineData(@"(\12)")]
    [InlineData(@"(\123)")]
    [InlineData(@"(\1234)")]
    public void LiteralStringAcceptsOctalEscapes(
        string source)
    {
        PdfLexer lexer =
            CreateLexer(
                source);

        PdfToken token =
            lexer.ReadToken();

        Assert.Equal(
            PdfTokenKind.LiteralString,
            token.Kind);

        Assert.Equal(
            source.Length,
            token.Span.Length);
    }

    [Fact]
    public void OctalEscapeConsumesAtMostThreeDigits()
    {
        PdfLexer lexer =
            CreateLexer(
                @"(\1234)]");

        PdfToken literalString =
            lexer.ReadToken();

        PdfToken arrayEnd =
            lexer.ReadToken();

        Assert.Equal(
            PdfTokenKind.LiteralString,
            literalString.Kind);

        Assert.Equal(
            7,
            literalString.Span.Length);

        Assert.Equal(
            PdfTokenKind.ArrayEnd,
            arrayEnd.Kind);

        Assert.Equal(
            7,
            arrayEnd.Span.Start.Offset);
    }

    [Fact]
    public void EscapedLineFeedContinuesLiteralString()
    {
        PdfLexer lexer =
            CreateLexer(
                "(first\\\nsecond)");

        PdfToken token =
            lexer.ReadToken();

        Assert.Equal(
            PdfTokenKind.LiteralString,
            token.Kind);

        Assert.Equal(
            2,
            token.Span.End.Line);

        Assert.Equal(
            8,
            token.Span.End.Column);
    }

    [Fact]
    public void EscapedCarriageReturnContinuesLiteralString()
    {
        PdfLexer lexer =
            CreateLexer(
                "(first\\\rsecond)");

        PdfToken token =
            lexer.ReadToken();

        Assert.Equal(
            PdfTokenKind.LiteralString,
            token.Kind);

        Assert.Equal(
            2,
            token.Span.End.Line);

        Assert.Equal(
            8,
            token.Span.End.Column);
    }

    [Fact]
    public void EscapedCarriageReturnLineFeedContinuesLiteralString()
    {
        PdfLexer lexer =
            CreateLexer(
                "(first\\\r\nsecond)");

        PdfToken token =
            lexer.ReadToken();

        Assert.Equal(
            PdfTokenKind.LiteralString,
            token.Kind);

        Assert.Equal(
            2,
            token.Span.End.Line);

        Assert.Equal(
            8,
            token.Span.End.Column);
    }

    [Fact]
    public void UnescapedLineEndingUpdatesSourcePosition()
    {
        PdfLexer lexer =
            CreateLexer(
                "(first\nsecond)");

        PdfToken token =
            lexer.ReadToken();

        Assert.Equal(
            PdfTokenKind.LiteralString,
            token.Kind);

        Assert.Equal(
            2,
            token.Span.End.Line);

        Assert.Equal(
            8,
            token.Span.End.Column);
    }

    [Fact]
    public void LiteralStringStopsAtMatchingClosingParenthesis()
    {
        PdfLexer lexer =
            CreateLexer(
                "(value)]");

        PdfToken literalString =
            lexer.ReadToken();

        PdfToken arrayEnd =
            lexer.ReadToken();

        Assert.Equal(
            PdfTokenKind.LiteralString,
            literalString.Kind);

        Assert.Equal(
            0,
            literalString.Span.Start.Offset);

        Assert.Equal(
            7,
            literalString.Span.End.Offset);

        Assert.Equal(
            PdfTokenKind.ArrayEnd,
            arrayEnd.Kind);

        Assert.Equal(
            7,
            arrayEnd.Span.Start.Offset);
    }

    [Fact]
    public void EscapedClosingParenthesisDoesNotEndString()
    {
        PdfLexer lexer =
            CreateLexer(
                @"(first\)second)]");

        PdfToken literalString =
            lexer.ReadToken();

        PdfToken arrayEnd =
            lexer.ReadToken();

        Assert.Equal(
            PdfTokenKind.LiteralString,
            literalString.Kind);

        Assert.Equal(
            15,
            literalString.Span.End.Offset);

        Assert.Equal(
            PdfTokenKind.ArrayEnd,
            arrayEnd.Kind);

        Assert.Equal(
            15,
            arrayEnd.Span.Start.Offset);
    }

    [Fact]
    public void UnterminatedLiteralStringIsRejected()
    {
        PdfLexer lexer =
            CreateLexer(
                "(unterminated");

        PdfLexerException exception =
            Assert.Throws<PdfLexerException>(
                () => lexer.ReadToken());

        Assert.Contains(
            "Unterminated PDF literal string",
            exception.Message,
            StringComparison.OrdinalIgnoreCase);

        Assert.Equal(
            PdfSourcePosition.Start,
            exception.Position);
    }

    [Fact]
    public void UnterminatedNestedLiteralStringIsRejected()
    {
        PdfLexer lexer =
            CreateLexer(
                "(outer (inner)");

        PdfLexerException exception =
            Assert.Throws<PdfLexerException>(
                () => lexer.ReadToken());

        Assert.Contains(
            "Unterminated PDF literal string",
            exception.Message,
            StringComparison.OrdinalIgnoreCase);

        Assert.Equal(
            PdfSourcePosition.Start,
            exception.Position);
    }

    [Fact]
    public void TrailingBackslashIsRejected()
    {
        PdfLexer lexer =
            CreateLexer(
                "(value\\");

        PdfLexerException exception =
            Assert.Throws<PdfLexerException>(
                () => lexer.ReadToken());

        Assert.Contains(
            "Unterminated escape sequence",
            exception.Message,
            StringComparison.OrdinalIgnoreCase);

        Assert.Equal(
            7,
            exception.Position.Offset);
    }

    [Fact]
    public void LiteralStringMayEqualMaximumTokenLength()
    {
        var lexer =
            new PdfLexer(
                CreateCursor(
                    "(abc)"),
                new PdfLexerOptions
                {
                    MaximumTokenLength = 5
                });

        PdfToken token =
            lexer.ReadToken();

        Assert.Equal(
            PdfTokenKind.LiteralString,
            token.Kind);

        Assert.Equal(
            5,
            token.Span.Length);
    }

    [Fact]
    public void LiteralStringRespectsMaximumTokenLength()
    {
        var lexer =
            new PdfLexer(
                CreateCursor(
                    "(abcd)"),
                new PdfLexerOptions
                {
                    MaximumTokenLength = 5
                });

        PdfLexerException exception =
            Assert.Throws<PdfLexerException>(
                () => lexer.ReadToken());

        Assert.Contains(
            "maximum of 5 bytes",
            exception.Message,
            StringComparison.OrdinalIgnoreCase);

        Assert.Equal(
            PdfSourcePosition.Start,
            exception.Position);
    }

    [Fact]
    public void LiteralStringEscapeBytesCountTowardMaximumLength()
    {
        var lexer =
            new PdfLexer(
                CreateCursor(
                    @"(\123)"),
                new PdfLexerOptions
                {
                    MaximumTokenLength = 5
                });

        PdfLexerException exception =
            Assert.Throws<PdfLexerException>(
                () => lexer.ReadToken());

        Assert.Contains(
            "maximum of 5 bytes",
            exception.Message,
            StringComparison.OrdinalIgnoreCase);

        Assert.Equal(
            PdfSourcePosition.Start,
            exception.Position);
    }

    [Fact]
    public void LiteralStringPreservesPositionAfterLeadingTrivia()
    {
        PdfLexer lexer =
            CreateLexer(
                "\r\n  (value)");

        PdfToken token =
            lexer.ReadToken();

        Assert.Equal(
            PdfTokenKind.LiteralString,
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
            11,
            token.Span.End.Offset);

        Assert.Equal(
            2,
            token.Span.End.Line);

        Assert.Equal(
            10,
            token.Span.End.Column);
    }
}