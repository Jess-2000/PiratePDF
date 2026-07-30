using BlackFlag.Engine.Syntax.Lexing;
using static BlackFlag.Engine.Syntax.Tests.Lexing.LexerTestFactory;

namespace BlackFlag.Engine.Syntax.Tests.Lexing;

public sealed class PdfLexerHexadecimalStringTests
{
    [Theory]
    [InlineData("<>")]
    [InlineData("<00>")]
    [InlineData("<48656C6C6F>")]
    [InlineData("<abcdef>")]
    [InlineData("<ABCDEF>")]
    [InlineData("<AbCdEf0123456789>")]
    public void ReadTokenRecognizesHexadecimalString(
        string source)
    {
        PdfLexer lexer =
            CreateLexer(
                source);

        PdfToken token =
            lexer.ReadToken();

        Assert.Equal(
            PdfTokenKind.HexadecimalString,
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
    [InlineData("<F>")]
    [InlineData("<ABC>")]
    [InlineData("<12345>")]
    public void HexadecimalStringAllowsOddNumberOfDigits(
        string source)
    {
        PdfLexer lexer =
            CreateLexer(
                source);

        PdfToken token =
            lexer.ReadToken();

        Assert.Equal(
            PdfTokenKind.HexadecimalString,
            token.Kind);

        Assert.Equal(
            source.Length,
            token.Span.Length);
    }

    [Theory]
    [InlineData("<48 65 6C 6C 6F>")]
    [InlineData("<48\t65>")]
    [InlineData("<48\n65>")]
    [InlineData("<48\r65>")]
    [InlineData("<48\r\n65>")]
    [InlineData("<48\f65>")]
    public void HexadecimalStringAllowsPdfWhiteSpace(
        string source)
    {
        PdfLexer lexer =
            CreateLexer(
                source);

        PdfToken token =
            lexer.ReadToken();

        Assert.Equal(
            PdfTokenKind.HexadecimalString,
            token.Kind);

        Assert.Equal(
            source.Length,
            token.Span.Length);
    }

    [Fact]
    public void HexadecimalStringAllowsNullWhiteSpace()
    {
        byte[] source =
        [
            (byte)'<',
            (byte)'4',
            (byte)'8',
            0x00,
            (byte)'6',
            (byte)'5',
            (byte)'>'
        ];

        var lexer =
            new PdfLexer(
                new TestRandomAccessCursor(
                    source));

        PdfToken token =
            lexer.ReadToken();

        Assert.Equal(
            PdfTokenKind.HexadecimalString,
            token.Kind);

        Assert.Equal(
            source.Length,
            token.Span.Length);
    }

    [Fact]
    public void LessThanPairRemainsDictionaryStart()
    {
        PdfLexer lexer =
            CreateLexer(
                "<<");

        PdfToken token =
            lexer.ReadToken();

        Assert.Equal(
            PdfTokenKind.DictionaryStart,
            token.Kind);

        Assert.Equal(
            0,
            token.Span.Start.Offset);

        Assert.Equal(
            2,
            token.Span.End.Offset);
    }

    [Fact]
    public void HexadecimalStringStopsAtClosingAngleBracket()
    {
        PdfLexer lexer =
            CreateLexer(
                "<4865>]");

        PdfToken hexadecimalString =
            lexer.ReadToken();

        PdfToken arrayEnd =
            lexer.ReadToken();

        Assert.Equal(
            PdfTokenKind.HexadecimalString,
            hexadecimalString.Kind);

        Assert.Equal(
            0,
            hexadecimalString.Span.Start.Offset);

        Assert.Equal(
            6,
            hexadecimalString.Span.End.Offset);

        Assert.Equal(
            PdfTokenKind.ArrayEnd,
            arrayEnd.Kind);

        Assert.Equal(
            6,
            arrayEnd.Span.Start.Offset);

        Assert.Equal(
            7,
            arrayEnd.Span.End.Offset);
    }

    [Theory]
    [InlineData("<GG>", 1, "0x47")]
    [InlineData("<12Z4>", 3, "0x5A")]
    [InlineData("<12(4>", 3, "0x28")]
    [InlineData("<12/4>", 3, "0x2F")]
    public void HexadecimalStringRejectsInvalidByte(
        string source,
        long expectedOffset,
        string expectedByte)
    {
        PdfLexer lexer =
            CreateLexer(
                source);

        PdfLexerException exception =
            Assert.Throws<PdfLexerException>(
                () => lexer.ReadToken());

        Assert.Contains(
            expectedByte,
            exception.Message,
            StringComparison.OrdinalIgnoreCase);

        Assert.Contains(
            "PDF hexadecimal string",
            exception.Message,
            StringComparison.OrdinalIgnoreCase);

        Assert.Equal(
            expectedOffset,
            exception.Position.Offset);
    }

    [Theory]
    [InlineData("<")]
    [InlineData("<123")]
    [InlineData("<12 34")]
    public void UnterminatedHexadecimalStringIsRejected(
        string source)
    {
        PdfLexer lexer =
            CreateLexer(
                source);

        PdfLexerException exception =
            Assert.Throws<PdfLexerException>(
                () => lexer.ReadToken());

        Assert.Contains(
            "Unterminated PDF hexadecimal string",
            exception.Message,
            StringComparison.OrdinalIgnoreCase);

        Assert.Equal(
            PdfSourcePosition.Start,
            exception.Position);
    }

    [Fact]
    public void HexadecimalStringTracksLineFeed()
    {
        PdfLexer lexer =
            CreateLexer(
                "<48\n65>");

        PdfToken token =
            lexer.ReadToken();

        Assert.Equal(
            PdfTokenKind.HexadecimalString,
            token.Kind);

        Assert.Equal(
            2,
            token.Span.End.Line);

        Assert.Equal(
            4,
            token.Span.End.Column);
    }

    [Fact]
    public void HexadecimalStringTracksCarriageReturn()
    {
        PdfLexer lexer =
            CreateLexer(
                "<48\r65>");

        PdfToken token =
            lexer.ReadToken();

        Assert.Equal(
            PdfTokenKind.HexadecimalString,
            token.Kind);

        Assert.Equal(
            2,
            token.Span.End.Line);

        Assert.Equal(
            4,
            token.Span.End.Column);
    }

    [Fact]
    public void HexadecimalStringTracksCarriageReturnLineFeed()
    {
        PdfLexer lexer =
            CreateLexer(
                "<48\r\n65>");

        PdfToken token =
            lexer.ReadToken();

        Assert.Equal(
            PdfTokenKind.HexadecimalString,
            token.Kind);

        Assert.Equal(
            2,
            token.Span.End.Line);

        Assert.Equal(
            4,
            token.Span.End.Column);
    }

    [Fact]
    public void HexadecimalStringMayEqualMaximumTokenLength()
    {
        var lexer =
            new PdfLexer(
                CreateCursor(
                    "<123>"),
                new PdfLexerOptions
                {
                    MaximumTokenLength = 5
                });

        PdfToken token =
            lexer.ReadToken();

        Assert.Equal(
            PdfTokenKind.HexadecimalString,
            token.Kind);

        Assert.Equal(
            5,
            token.Span.Length);
    }

    [Fact]
    public void HexadecimalStringRespectsMaximumTokenLength()
    {
        var lexer =
            new PdfLexer(
                CreateCursor(
                    "<1234>"),
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
    public void HexadecimalStringWhiteSpaceCountsTowardMaximumLength()
    {
        var lexer =
            new PdfLexer(
                CreateCursor(
                    "<12 3>"),
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
    public void HexadecimalStringPreservesPositionAfterLeadingTrivia()
    {
        PdfLexer lexer =
            CreateLexer(
                "\r\n  <4865>");

        PdfToken token =
            lexer.ReadToken();

        Assert.Equal(
            PdfTokenKind.HexadecimalString,
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
            10,
            token.Span.End.Offset);

        Assert.Equal(
            2,
            token.Span.End.Line);

        Assert.Equal(
            9,
            token.Span.End.Column);
    }

    [Fact]
    public void SequentialHexadecimalStringsRetainTheirOwnSpans()
    {
        PdfLexer lexer =
            CreateLexer(
                "<01> <ABC>");

        PdfToken first =
            lexer.ReadToken();

        PdfToken second =
            lexer.ReadToken();

        PdfToken endOfFile =
            lexer.ReadToken();

        Assert.Equal(
            PdfTokenKind.HexadecimalString,
            first.Kind);

        Assert.Equal(
            0,
            first.Span.Start.Offset);

        Assert.Equal(
            4,
            first.Span.End.Offset);

        Assert.Equal(
            PdfTokenKind.HexadecimalString,
            second.Kind);

        Assert.Equal(
            5,
            second.Span.Start.Offset);

        Assert.Equal(
            10,
            second.Span.End.Offset);

        Assert.Equal(
            PdfTokenKind.EndOfFile,
            endOfFile.Kind);

        Assert.Equal(
            10,
            endOfFile.Span.Start.Offset);
    }
}