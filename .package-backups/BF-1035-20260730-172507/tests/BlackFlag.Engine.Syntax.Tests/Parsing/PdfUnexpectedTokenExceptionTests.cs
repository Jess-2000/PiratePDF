using BlackFlag.Engine.Syntax.Lexing;
using BlackFlag.Engine.Syntax.Parsing;

namespace BlackFlag.Engine.Syntax.Tests.Parsing;

public sealed class PdfUnexpectedTokenExceptionTests
{
    [Fact]
    public void ConstructorStoresExpectedAndActualTokens()
    {
        PdfToken token = CreateToken();

        var exception =
            new PdfUnexpectedTokenException(
                PdfTokenKind.Name,
                token);

        Assert.Equal(PdfTokenKind.Name, exception.ExpectedKind);
        Assert.Equal(token, exception.ActualToken);
        Assert.Equal(token.Span.Start, exception.Position);
    }

    [Fact]
    public void ConstructorCreatesLocationAwareMessage()
    {
        PdfToken token = CreateToken();

        var exception =
            new PdfUnexpectedTokenException(
                PdfTokenKind.Name,
                token);

        Assert.Contains("Expected token kind Name", exception.Message);
        Assert.Contains("encountered Null", exception.Message);
        Assert.Contains("Offset: 12", exception.Message);
        Assert.Contains("line: 3", exception.Message);
        Assert.Contains("column: 4", exception.Message);
    }

    [Fact]
    public void ConstructorStoresInnerException()
    {
        PdfToken token = CreateToken();
        var innerException = new InvalidOperationException("Failure.");

        var exception =
            new PdfUnexpectedTokenException(
                PdfTokenKind.Name,
                token,
                innerException);

        Assert.Same(innerException, exception.InnerException);
    }

    [Fact]
    public void ConstructorRejectsUnassignedExpectedKind()
    {
        PdfToken token = CreateToken();

        Assert.Throws<ArgumentOutOfRangeException>(
            () => new PdfUnexpectedTokenException(
                PdfTokenKind.None,
                token));
    }

    [Fact]
    public void ConstructorRejectsNullInnerException()
    {
        PdfToken token = CreateToken();

        Assert.Throws<ArgumentNullException>(
            () => new PdfUnexpectedTokenException(
                PdfTokenKind.Name,
                token,
                null!));
    }

    private static PdfToken CreateToken()
    {
        return new PdfToken(
            PdfTokenKind.Null,
            new PdfSourceSpan(
                new PdfSourcePosition(
                    offset: 12,
                    line: 3,
                    column: 4),
                new PdfSourcePosition(
                    offset: 16,
                    line: 3,
                    column: 8)));
    }
}
