using BlackFlag.Engine.Syntax.Lexing;

namespace BlackFlag.Engine.Syntax.Tests.Lexing;

public sealed class PdfTokenTests
{
    [Fact]
    public void ConstructorStoresKindAndSpan()
    {
        var span =
            new PdfSourceSpan(
                new PdfSourcePosition(
                    offset: 4,
                    line: 1,
                    column: 5),
                new PdfSourcePosition(
                    offset: 8,
                    line: 1,
                    column: 9));

        var token =
            new PdfToken(
                PdfTokenKind.Name,
                span);

        Assert.Equal(
            PdfTokenKind.Name,
            token.Kind);

        Assert.Equal(
            span,
            token.Span);
    }

    [Fact]
    public void ConstructorRejectsUnassignedKind()
    {
        var span =
            new PdfSourceSpan(
                PdfSourcePosition.Start,
                new PdfSourcePosition(
                    offset: 1,
                    line: 1,
                    column: 2));

        Assert.Throws<ArgumentOutOfRangeException>(
            () => new PdfToken(
                PdfTokenKind.None,
                span));
    }

    [Fact]
    public void ConstructorRejectsEmptyNonEndOfFileToken()
    {
        var span =
            PdfSourceSpan.EmptyAt(
                PdfSourcePosition.Start);

        Assert.Throws<ArgumentException>(
            () => new PdfToken(
                PdfTokenKind.Keyword,
                span));
    }

    [Fact]
    public void EndOfFileAtCreatesEmptyEndOfFileToken()
    {
        var position =
            new PdfSourcePosition(
                offset: 100,
                line: 8,
                column: 12);

        var token =
            PdfToken.EndOfFileAt(position);

        Assert.Equal(
            PdfTokenKind.EndOfFile,
            token.Kind);

        Assert.Equal(
            position,
            token.Span.Start);

        Assert.Equal(
            position,
            token.Span.End);

        Assert.True(
            token.Span.IsEmpty);
    }
}
