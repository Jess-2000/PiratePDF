using BlackFlag.Engine.Syntax.Lexing;

namespace BlackFlag.Engine.Syntax.Tests.Lexing;

public sealed class PdfLexerExceptionTests
{
    [Fact]
    public void ConstructorStoresPosition()
    {
        var position =
            new PdfSourcePosition(
                offset: 18,
                line: 3,
                column: 5);

        var exception =
            new PdfLexerException(
                "Invalid token",
                position);

        Assert.Equal(
            position,
            exception.Position);
    }

    [Fact]
    public void ConstructorIncludesPositionInMessage()
    {
        var position =
            new PdfSourcePosition(
                offset: 18,
                line: 3,
                column: 5);

        var exception =
            new PdfLexerException(
                "Invalid token",
                position);

        Assert.Contains(
            "Invalid token",
            exception.Message,
            StringComparison.Ordinal);

        Assert.Contains(
            "Offset: 18",
            exception.Message,
            StringComparison.Ordinal);

        Assert.Contains(
            "line: 3",
            exception.Message,
            StringComparison.Ordinal);

        Assert.Contains(
            "column: 5",
            exception.Message,
            StringComparison.Ordinal);
    }

    [Fact]
    public void ConstructorStoresInnerException()
    {
        var position =
            PdfSourcePosition.Start;

        var innerException =
            new IOException(
                "Source read failed.");

        var exception =
            new PdfLexerException(
                "Unable to read the next token",
                position,
                innerException);

        Assert.Same(
            innerException,
            exception.InnerException);
    }

    [Fact]
    public void ConstructorRejectsEmptyMessage()
    {
        Assert.Throws<ArgumentException>(
            () => new PdfLexerException(
                string.Empty,
                PdfSourcePosition.Start));
    }

    [Fact]
    public void ConstructorRejectsNullInnerException()
    {
        Assert.Throws<ArgumentNullException>(
            () => new PdfLexerException(
                "Unable to read the next token",
                PdfSourcePosition.Start,
                null!));
    }
}
