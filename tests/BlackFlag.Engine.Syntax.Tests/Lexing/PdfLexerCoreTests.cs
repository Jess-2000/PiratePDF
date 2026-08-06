using BlackFlag.Engine.Syntax.Lexing;
using static BlackFlag.Engine.Syntax.Tests.Lexing.LexerTestFactory;

namespace BlackFlag.Engine.Syntax.Tests.Lexing;

public sealed class PdfLexerCoreTests
{
    [Fact]
    public void ConstructorUsesCursorPositionAsInitialOffset()
    {
        TestRandomAccessCursor cursor =
            CreateCursor(
                "abc");

        cursor.Seek(
            2);

        var lexer =
            new PdfLexer(
                cursor);

        Assert.Equal(
            2,
            lexer.Position.Offset);

        Assert.Equal(
            1,
            lexer.Position.Line);

        Assert.Equal(
            1,
            lexer.Position.Column);
    }

    [Fact]
    public void ReadTokenRecognizesArrayDelimiters()
    {
        PdfLexer lexer =
            CreateLexer(
                "[]");

        PdfToken start =
            lexer.ReadToken();

        PdfToken end =
            lexer.ReadToken();

        Assert.Equal(
            PdfTokenKind.ArrayStart,
            start.Kind);

        Assert.Equal(
            0,
            start.Span.Start.Offset);

        Assert.Equal(
            1,
            start.Span.End.Offset);

        Assert.Equal(
            PdfTokenKind.ArrayEnd,
            end.Kind);

        Assert.Equal(
            1,
            end.Span.Start.Offset);

        Assert.Equal(
            2,
            end.Span.End.Offset);
    }

    [Fact]
    public void ReadTokenRecognizesDictionaryDelimiters()
    {
        PdfLexer lexer =
            CreateLexer(
                "<< >>");

        PdfToken start =
            lexer.ReadToken();

        PdfToken end =
            lexer.ReadToken();

        Assert.Equal(
            PdfTokenKind.DictionaryStart,
            start.Kind);

        Assert.Equal(
            0,
            start.Span.Start.Offset);

        Assert.Equal(
            2,
            start.Span.End.Offset);

        Assert.Equal(
            PdfTokenKind.DictionaryEnd,
            end.Kind);

        Assert.Equal(
            3,
            end.Span.Start.Offset);

        Assert.Equal(
            5,
            end.Span.End.Offset);
    }

    [Fact]
    public void ReadTokenSkipsPdfWhiteSpace()
    {
        byte[] source =
        [
            0x00,
            0x09,
            0x0C,
            0x20,
            (byte)'['
        ];

        var lexer =
            new PdfLexer(
                new TestRandomAccessCursor(
                    source));

        PdfToken token =
            lexer.ReadToken();

        Assert.Equal(
            PdfTokenKind.ArrayStart,
            token.Kind);

        Assert.Equal(
            4,
            token.Span.Start.Offset);

        Assert.Equal(
            1,
            token.Span.Start.Line);

        Assert.Equal(
            5,
            token.Span.Start.Column);
    }

    [Fact]
    public void ReadTokenSkipsComment()
    {
        PdfLexer lexer =
            CreateLexer(
                "% comment\n[");

        PdfToken token =
            lexer.ReadToken();

        Assert.Equal(
            PdfTokenKind.ArrayStart,
            token.Kind);

        Assert.Equal(
            10,
            token.Span.Start.Offset);

        Assert.Equal(
            2,
            token.Span.Start.Line);

        Assert.Equal(
            1,
            token.Span.Start.Column);
    }

    [Fact]
    public void ReadTokenSkipsMultipleCommentsAndWhiteSpace()
    {
        PdfLexer lexer =
            CreateLexer(
                " % first\r\n% second\r  [");

        PdfToken token =
            lexer.ReadToken();

        Assert.Equal(
            PdfTokenKind.ArrayStart,
            token.Kind);

        Assert.Equal(
            3,
            token.Span.Start.Line);

        Assert.Equal(
            3,
            token.Span.Start.Column);
    }

    [Fact]
    public void CarriageReturnAdvancesOneLine()
    {
        PdfLexer lexer =
            CreateLexer(
                "\r[");

        PdfToken token =
            lexer.ReadToken();

        Assert.Equal(
            2,
            token.Span.Start.Line);

        Assert.Equal(
            1,
            token.Span.Start.Column);
    }

    [Fact]
    public void LineFeedAdvancesOneLine()
    {
        PdfLexer lexer =
            CreateLexer(
                "\n[");

        PdfToken token =
            lexer.ReadToken();

        Assert.Equal(
            2,
            token.Span.Start.Line);

        Assert.Equal(
            1,
            token.Span.Start.Column);
    }

    [Fact]
    public void CarriageReturnLineFeedAdvancesOneLine()
    {
        PdfLexer lexer =
            CreateLexer(
                "\r\n[");

        PdfToken token =
            lexer.ReadToken();

        Assert.Equal(
            2,
            token.Span.Start.Line);

        Assert.Equal(
            1,
            token.Span.Start.Column);

        Assert.Equal(
            2,
            token.Span.Start.Offset);
    }

    [Fact]
    public void RepeatedEndOfFileReadsReturnSamePosition()
    {
        PdfLexer lexer =
            CreateLexer(
                " ");

        PdfToken first =
            lexer.ReadToken();

        PdfToken second =
            lexer.ReadToken();

        Assert.Equal(
            PdfTokenKind.EndOfFile,
            first.Kind);

        Assert.Equal(
            PdfTokenKind.EndOfFile,
            second.Kind);

        Assert.Equal(
            first,
            second);

        Assert.Equal(
            1,
            first.Span.Start.Offset);

        Assert.True(
            first.Span.IsEmpty);
    }

    [Fact]
    public void CommentMayEndAtEndOfSource()
    {
        PdfLexer lexer =
            CreateLexer(
                "% comment");

        PdfToken token =
            lexer.ReadToken();

        Assert.Equal(
            PdfTokenKind.EndOfFile,
            token.Kind);

        Assert.Equal(
            9,
            token.Span.Start.Offset);

        Assert.Equal(
            1,
            token.Span.Start.Line);

        Assert.Equal(
            10,
            token.Span.Start.Column);
    }

    [Fact]
    public void SingleClosingAngleBracketIsRejected()
    {
        PdfLexer lexer =
            CreateLexer(
                ">");

        PdfLexerException exception =
            Assert.Throws<PdfLexerException>(
                () => lexer.ReadToken());

        Assert.Contains(
            "single closing angle bracket",
            exception.Message,
            StringComparison.OrdinalIgnoreCase);

        Assert.Equal(
            PdfSourcePosition.Start,
            exception.Position);
    }

    [Fact]
    public void UnsupportedTokenReportsByteAndPosition()
    {
        PdfLexer lexer =
            CreateLexer(
                "@");

        PdfLexerException exception =
            Assert.Throws<PdfLexerException>(
                () => lexer.ReadToken());

        Assert.Contains(
            "0x40",
            exception.Message,
            StringComparison.OrdinalIgnoreCase);

        Assert.Equal(
            PdfSourcePosition.Start,
            exception.Position);
    }
}
