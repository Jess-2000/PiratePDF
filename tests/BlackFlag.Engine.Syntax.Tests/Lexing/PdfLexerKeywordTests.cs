using BlackFlag.Engine.Syntax.Lexing;
using static BlackFlag.Engine.Syntax.Tests.Lexing.LexerTestFactory;

namespace BlackFlag.Engine.Syntax.Tests.Lexing;

public sealed class PdfLexerKeywordTests
{
    [Theory]
    [InlineData("true", PdfTokenKind.BooleanTrue)]
    [InlineData("false", PdfTokenKind.BooleanFalse)]
    [InlineData("null", PdfTokenKind.Null)]
    [InlineData("obj", PdfTokenKind.ObjectStart)]
    [InlineData("endobj", PdfTokenKind.ObjectEnd)]
    [InlineData("R", PdfTokenKind.IndirectReferenceMarker)]
    [InlineData("stream", PdfTokenKind.StreamStart)]
    [InlineData("endstream", PdfTokenKind.StreamEnd)]
    [InlineData("xref", PdfTokenKind.CrossReference)]
    [InlineData("trailer", PdfTokenKind.Trailer)]
    [InlineData("startxref", PdfTokenKind.StartCrossReference)]
    public void ReadTokenRecognizesReservedKeyword(
        string source,
        PdfTokenKind expectedKind)
    {
        PdfLexer lexer =
            CreateLexer(
                source);

        PdfToken token =
            lexer.ReadToken();

        Assert.Equal(
            expectedKind,
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
    [InlineData("True")]
    [InlineData("TRUE")]
    [InlineData("False")]
    [InlineData("FALSE")]
    [InlineData("Null")]
    [InlineData("NULL")]
    [InlineData("Obj")]
    [InlineData("ENDOBJ")]
    [InlineData("r")]
    [InlineData("Stream")]
    [InlineData("ENDSTREAM")]
    [InlineData("XRef")]
    [InlineData("Trailer")]
    [InlineData("StartXRef")]
    public void ReservedKeywordIsCaseSensitive(
        string source)
    {
        PdfLexer lexer =
            CreateLexer(
                source);

        PdfLexerException exception =
            Assert.Throws<PdfLexerException>(
                () => lexer.ReadToken());

        Assert.Contains(
            "Unknown PDF keyword",
            exception.Message,
            StringComparison.OrdinalIgnoreCase);

        Assert.Contains(
            source,
            exception.Message,
            StringComparison.Ordinal);

        Assert.Equal(
            PdfSourcePosition.Start,
            exception.Position);
    }

    [Theory]
    [InlineData("trueValue")]
    [InlineData("true1")]
    [InlineData("true#")]
    [InlineData("falseValue")]
    [InlineData("nullObject")]
    [InlineData("obj1")]
    [InlineData("endobjNow")]
    [InlineData("Reference")]
    [InlineData("streaming")]
    [InlineData("endstreamExtra")]
    [InlineData("xrefTable")]
    [InlineData("trailerData")]
    [InlineData("startxref0")]
    public void ReservedKeywordRequiresTokenBoundary(
        string source)
    {
        PdfLexer lexer =
            CreateLexer(
                source);

        PdfLexerException exception =
            Assert.Throws<PdfLexerException>(
                () => lexer.ReadToken());

        Assert.Contains(
            source.Length <= 16
                ? source
                : source[..16],
            exception.Message,
            StringComparison.Ordinal);

        Assert.Equal(
            PdfSourcePosition.Start,
            exception.Position);

        Assert.Equal(
            source.Length,
            lexer.Position.Offset);
    }

    [Theory]
    [InlineData("unknown")]
    [InlineData("foobar")]
    [InlineData("object")]
    [InlineData("reference")]
    [InlineData("xrefstream")]
    public void UnknownKeywordIsRejected(
        string source)
    {
        PdfLexer lexer =
            CreateLexer(
                source);

        PdfLexerException exception =
            Assert.Throws<PdfLexerException>(
                () => lexer.ReadToken());

        Assert.Contains(
            "Unknown PDF keyword",
            exception.Message,
            StringComparison.OrdinalIgnoreCase);

        Assert.Contains(
            source,
            exception.Message,
            StringComparison.Ordinal);

        Assert.Equal(
            PdfSourcePosition.Start,
            exception.Position);
    }

    [Theory]
    [InlineData("true]", PdfTokenKind.BooleanTrue)]
    [InlineData("false/", PdfTokenKind.BooleanFalse)]
    [InlineData("null<", PdfTokenKind.Null)]
    [InlineData("obj[", PdfTokenKind.ObjectStart)]
    [InlineData("endobj%", PdfTokenKind.ObjectEnd)]
    [InlineData("R>", PdfTokenKind.IndirectReferenceMarker)]
    [InlineData("stream(", PdfTokenKind.StreamStart)]
    [InlineData("endstream)", PdfTokenKind.StreamEnd)]
    [InlineData("xref<", PdfTokenKind.CrossReference)]
    [InlineData("trailer/", PdfTokenKind.Trailer)]
    [InlineData("startxref[", PdfTokenKind.StartCrossReference)]
    public void ReservedKeywordStopsBeforeDelimiter(
        string source,
        PdfTokenKind expectedKind)
    {
        PdfLexer lexer =
            CreateLexer(
                source);

        PdfToken token =
            lexer.ReadToken();

        Assert.Equal(
            expectedKind,
            token.Kind);

        Assert.Equal(
            source.Length - 1,
            token.Span.End.Offset);

        Assert.Equal(
            source.Length - 1,
            lexer.Position.Offset);
    }

    [Fact]
    public void ReservedKeywordsCanBeReadSequentially()
    {
        const string Source =
            "true false null obj endobj R stream endstream xref trailer startxref";

        PdfLexer lexer =
            CreateLexer(
                Source);

        PdfTokenKind[] expectedKinds =
        [
            PdfTokenKind.BooleanTrue,
            PdfTokenKind.BooleanFalse,
            PdfTokenKind.Null,
            PdfTokenKind.ObjectStart,
            PdfTokenKind.ObjectEnd,
            PdfTokenKind.IndirectReferenceMarker,
            PdfTokenKind.StreamStart,
            PdfTokenKind.StreamEnd,
            PdfTokenKind.CrossReference,
            PdfTokenKind.Trailer,
            PdfTokenKind.StartCrossReference,
            PdfTokenKind.EndOfFile
        ];

        foreach (PdfTokenKind expectedKind in expectedKinds)
        {
            PdfToken token =
                lexer.ReadToken();

            Assert.Equal(
                expectedKind,
                token.Kind);
        }

        Assert.Equal(
            Source.Length,
            lexer.Position.Offset);
    }

    [Fact]
    public void ReservedKeywordPreservesPositionAfterLeadingTrivia()
    {
        PdfLexer lexer =
            CreateLexer(
                "\r\n  startxref");

        PdfToken token =
            lexer.ReadToken();

        Assert.Equal(
            PdfTokenKind.StartCrossReference,
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
            13,
            token.Span.End.Offset);

        Assert.Equal(
            2,
            token.Span.End.Line);

        Assert.Equal(
            12,
            token.Span.End.Column);
    }

    [Fact]
    public void ReservedKeywordPreservesPositionAfterComment()
    {
        PdfLexer lexer =
            CreateLexer(
                "% object\r\nendobj");

        PdfToken token =
            lexer.ReadToken();

        Assert.Equal(
            PdfTokenKind.ObjectEnd,
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

        Assert.Equal(
            16,
            token.Span.End.Offset);

        Assert.Equal(
            2,
            token.Span.End.Line);

        Assert.Equal(
            7,
            token.Span.End.Column);
    }

    [Fact]
    public void LongestReservedKeywordMayEqualMaximumTokenLength()
    {
        PdfLexer lexer =
            new(
                CreateCursor(
                    "startxref"),
                new PdfLexerOptions
                {
                    MaximumTokenLength = 9
                });

        PdfToken token =
            lexer.ReadToken();

        Assert.Equal(
            PdfTokenKind.StartCrossReference,
            token.Kind);

        Assert.Equal(
            9,
            token.Span.Length);
    }

    [Fact]
    public void ReservedKeywordRespectsMaximumTokenLength()
    {
        PdfLexer lexer =
            new(
                CreateCursor(
                    "startxref"),
                new PdfLexerOptions
                {
                    MaximumTokenLength = 8
                });

        PdfLexerException exception =
            Assert.Throws<PdfLexerException>(
                () => lexer.ReadToken());

        Assert.Contains(
            "maximum of 8 bytes",
            exception.Message,
            StringComparison.OrdinalIgnoreCase);

        Assert.Equal(
            PdfSourcePosition.Start,
            exception.Position);
    }

    [Fact]
    public void UnknownKeywordRespectsMaximumTokenLength()
    {
        PdfLexer lexer =
            new(
                CreateCursor(
                    "abcdefghijk"),
                new PdfLexerOptions
                {
                    MaximumTokenLength = 10
                });

        PdfLexerException exception =
            Assert.Throws<PdfLexerException>(
                () => lexer.ReadToken());

        Assert.Contains(
            "maximum of 10 bytes",
            exception.Message,
            StringComparison.OrdinalIgnoreCase);

        Assert.Equal(
            PdfSourcePosition.Start,
            exception.Position);
    }

    [Fact]
    public void LongUnknownKeywordDiagnosticIsTruncated()
    {
        const string Source =
            "abcdefghijklmnopq";

        PdfLexer lexer =
            CreateLexer(
                Source);

        PdfLexerException exception =
            Assert.Throws<PdfLexerException>(
                () => lexer.ReadToken());

        Assert.Contains(
            "'abcdefghijklmnop...'",
            exception.Message,
            StringComparison.Ordinal);

        Assert.Equal(
            Source.Length,
            lexer.Position.Offset);
    }

    [Fact]
    public void UnknownKeywordReportsKeywordTextAndPosition()
    {
        PdfLexer lexer =
            CreateLexer(
                "A");

        PdfLexerException exception =
            Assert.Throws<PdfLexerException>(
                () => lexer.ReadToken());

        Assert.Contains(
            "Unknown PDF keyword",
            exception.Message,
            StringComparison.OrdinalIgnoreCase);

        Assert.Contains(
            "'A'",
            exception.Message,
            StringComparison.Ordinal);

        Assert.Equal(
            PdfSourcePosition.Start,
            exception.Position);

        Assert.Equal(
            1,
            lexer.Position.Offset);
    }

    [Theory]
    [InlineData("null", PdfTokenKind.Null)]
    [InlineData("endobj", PdfTokenKind.ObjectEnd)]
    [InlineData("endstream", PdfTokenKind.StreamEnd)]
    [InlineData("startxref", PdfTokenKind.StartCrossReference)]
    public void ReservedKeywordIsFollowedByEndOfFile(
        string source,
        PdfTokenKind expectedKind)
    {
        PdfLexer lexer =
            CreateLexer(
                source);

        PdfToken value =
            lexer.ReadToken();

        PdfToken endOfFile =
            lexer.ReadToken();

        Assert.Equal(
            expectedKind,
            value.Kind);

        Assert.Equal(
            PdfTokenKind.EndOfFile,
            endOfFile.Kind);

        Assert.Equal(
            source.Length,
            endOfFile.Span.Start.Offset);

        Assert.Equal(
            source.Length,
            endOfFile.Span.End.Offset);
    }
}
