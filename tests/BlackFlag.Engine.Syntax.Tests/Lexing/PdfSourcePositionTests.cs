using BlackFlag.Engine.Syntax.Lexing;

namespace BlackFlag.Engine.Syntax.Tests.Lexing;

public sealed class PdfSourcePositionTests
{
    [Fact]
    public void ConstructorStoresPositionValues()
    {
        var position =
            new PdfSourcePosition(
                offset: 42,
                line: 3,
                column: 7);

        Assert.Equal(
            42,
            position.Offset);

        Assert.Equal(
            3,
            position.Line);

        Assert.Equal(
            7,
            position.Column);
    }

    [Fact]
    public void StartRepresentsBeginningOfSource()
    {
        var position =
            PdfSourcePosition.Start;

        Assert.Equal(
            0,
            position.Offset);

        Assert.Equal(
            1,
            position.Line);

        Assert.Equal(
            1,
            position.Column);
    }

    [Fact]
    public void ConstructorRejectsNegativeOffset()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new PdfSourcePosition(
                offset: -1,
                line: 1,
                column: 1));
    }

    [Fact]
    public void ConstructorRejectsLineBelowOne()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new PdfSourcePosition(
                offset: 0,
                line: 0,
                column: 1));
    }

    [Fact]
    public void ConstructorRejectsColumnBelowOne()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new PdfSourcePosition(
                offset: 0,
                line: 1,
                column: 0));
    }
}