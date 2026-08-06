using BlackFlag.Engine.Syntax.Lexing;

namespace BlackFlag.Engine.Syntax.Tests.Lexing;

public sealed class PdfLexerOptionsTests
{
    [Fact]
    public void ConstructorUsesDefaultMaximumTokenLength()
    {
        var options =
            new PdfLexerOptions();

        Assert.Equal(
            PdfLexerOptions.DefaultMaximumTokenLength,
            options.MaximumTokenLength);
    }

    [Fact]
    public void MaximumTokenLengthAcceptsPositiveValue()
    {
        var options =
            new PdfLexerOptions
            {
                MaximumTokenLength = 4096
            };

        Assert.Equal(
            4096,
            options.MaximumTokenLength);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void MaximumTokenLengthRejectsNonPositiveValue(
        int value)
    {
        var options =
            new PdfLexerOptions();

        Assert.Throws<ArgumentOutOfRangeException>(
            () => options.MaximumTokenLength = value);
    }
}
