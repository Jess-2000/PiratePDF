using BlackFlag.Engine.Syntax.Lexing;

namespace BlackFlag.Engine.Syntax.Tests.Lexing;

public sealed class PdfSourceSpanTests
{
    [Fact]
    public void ConstructorStoresPositionsAndCalculatesLength()
    {
        var start =
            new PdfSourcePosition(
                offset: 5,
                line: 2,
                column: 3);

        var end =
            new PdfSourcePosition(
                offset: 12,
                line: 2,
                column: 10);

        var span =
            new PdfSourceSpan(
                start,
                end);

        Assert.Equal(
            start,
            span.Start);

        Assert.Equal(
            end,
            span.End);

        Assert.Equal(
            7,
            span.Length);

        Assert.False(
            span.IsEmpty);
    }

    [Fact]
    public void EmptyAtCreatesEmptySpan()
    {
        var position =
            new PdfSourcePosition(
                offset: 10,
                line: 4,
                column: 2);

        var span =
            PdfSourceSpan.EmptyAt(position);

        Assert.Equal(
            position,
            span.Start);

        Assert.Equal(
            position,
            span.End);

        Assert.Equal(
            0,
            span.Length);

        Assert.True(
            span.IsEmpty);
    }

    [Fact]
    public void ConstructorRejectsEndBeforeStart()
    {
        var start =
            new PdfSourcePosition(
                offset: 10,
                line: 1,
                column: 11);

        var end =
            new PdfSourcePosition(
                offset: 5,
                line: 1,
                column: 6);

        Assert.Throws<ArgumentException>(
            () => new PdfSourceSpan(
                start,
                end));
    }
}
