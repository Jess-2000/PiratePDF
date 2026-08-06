using BlackFlag.Engine.Document.Objects;

namespace BlackFlag.Engine.Document.Tests.Objects;

public sealed class PdfObjectIdentifierTests
{
    [Fact]
    public void Constructor_WithValidValues_PreservesValues()
    {
        var identifier =
            new PdfObjectIdentifier(15, 2);

        Assert.Equal(
            15,
            identifier.ObjectNumber);

        Assert.Equal(
            2,
            identifier.GenerationNumber);

        Assert.Equal(
            "15 2",
            identifier.ToString());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Constructor_WithInvalidObjectNumber_Throws(
        int objectNumber)
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new PdfObjectIdentifier(
                objectNumber));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(65536)]
    public void Constructor_WithInvalidGenerationNumber_Throws(
        int generationNumber)
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new PdfObjectIdentifier(
                1,
                generationNumber));
    }
}