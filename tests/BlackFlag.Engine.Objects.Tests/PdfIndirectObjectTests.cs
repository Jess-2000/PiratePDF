namespace BlackFlag.Engine.Objects.Tests;

using Xunit;

public sealed class PdfIndirectObjectTests
{
    [Fact]
    public void ConstructorStoresValidValues()
    {
        PdfObject value = new PdfInteger(42);
        PdfIndirectObject indirectObject = new(15, 2, value);

        Assert.Equal(15, indirectObject.ObjectNumber);
        Assert.Equal(2, indirectObject.GenerationNumber);
        Assert.Same(value, indirectObject.Value);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(int.MinValue)]
    public void ConstructorRejectsInvalidObjectNumber(int objectNumber)
    {
        ArgumentOutOfRangeException exception =
            Assert.Throws<ArgumentOutOfRangeException>(
                () => new PdfIndirectObject(
                    objectNumber,
                    0,
                    PdfNull.Instance));

        Assert.Equal("objectNumber", exception.ParamName);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(65536)]
    [InlineData(int.MinValue)]
    [InlineData(int.MaxValue)]
    public void ConstructorRejectsInvalidGenerationNumber(
        int generationNumber)
    {
        ArgumentOutOfRangeException exception =
            Assert.Throws<ArgumentOutOfRangeException>(
                () => new PdfIndirectObject(
                    1,
                    generationNumber,
                    PdfNull.Instance));

        Assert.Equal("generationNumber", exception.ParamName);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(65535)]
    public void ConstructorAcceptsGenerationBoundaries(int generationNumber)
    {
        PdfIndirectObject indirectObject = new(
            1,
            generationNumber,
            PdfNull.Instance);

        Assert.Equal(generationNumber, indirectObject.GenerationNumber);
    }

    [Fact]
    public void ConstructorRejectsNullValue()
    {
        ArgumentNullException exception =
            Assert.Throws<ArgumentNullException>(
                () => new PdfIndirectObject(1, 0, null!));

        Assert.Equal("value", exception.ParamName);
    }

    [Fact]
    public void ConstructorAcceptsPdfNull()
    {
        PdfIndirectObject indirectObject = new(1, 0, PdfNull.Instance);

        Assert.Same(PdfNull.Instance, indirectObject.Value);
    }

    [Fact]
    public void IsNotAPdfObject()
    {
        PdfIndirectObject indirectObject = new(1, 0, PdfNull.Instance);

        Assert.False(typeof(PdfObject).IsAssignableFrom(indirectObject.GetType()));
    }

    [Fact]
    public void EqualsReturnsTrueForSameReference()
    {
        PdfIndirectObject indirectObject = new(1, 0, PdfNull.Instance);

        Assert.True(indirectObject.Equals(indirectObject));
    }

    [Fact]
    public void EqualsReturnsFalseForNull()
    {
        PdfIndirectObject indirectObject = new(1, 0, PdfNull.Instance);

        Assert.False(indirectObject.Equals(null));
    }

    [Fact]
    public void EqualsReturnsFalseForDifferentObjectType()
    {
        PdfIndirectObject indirectObject = new(1, 0, PdfNull.Instance);

        Assert.False(indirectObject.Equals(PdfNull.Instance));
    }

    [Fact]
    public void EqualIndirectObjectsUseIdentifierAndValueEquality()
    {
        PdfIndirectObject left = new(15, 2, new PdfInteger(42));
        PdfIndirectObject right = new(15, 2, new PdfInteger(42));

        Assert.Equal(left, right);
        Assert.Equal(left.GetHashCode(), right.GetHashCode());
    }

    [Fact]
    public void EqualityRejectsDifferentObjectNumbers()
    {
        PdfIndirectObject left = new(15, 2, new PdfInteger(42));
        PdfIndirectObject right = new(16, 2, new PdfInteger(42));

        Assert.NotEqual(left, right);
    }

    [Fact]
    public void EqualityRejectsDifferentGenerationNumbers()
    {
        PdfIndirectObject left = new(15, 2, new PdfInteger(42));
        PdfIndirectObject right = new(15, 3, new PdfInteger(42));

        Assert.NotEqual(left, right);
    }

    [Fact]
    public void EqualityRejectsDifferentValues()
    {
        PdfIndirectObject left = new(15, 2, new PdfInteger(42));
        PdfIndirectObject right = new(15, 2, new PdfInteger(99));

        Assert.NotEqual(left, right);
    }

    [Fact]
    public void EqualitySupportsNestedCollectionValues()
    {
        PdfIndirectObject left = new(
            15,
            2,
            new PdfDictionary(
            [
                new(
                    new PdfName("Items"),
                    new PdfArray(
                    [
                        new PdfInteger(1),
                        new PdfIndirectReference(9, 0),
                    ])),
            ]));

        PdfIndirectObject right = new(
            15,
            2,
            new PdfDictionary(
            [
                new(
                    new PdfName("Items"),
                    new PdfArray(
                    [
                        new PdfInteger(1),
                        new PdfIndirectReference(9, 0),
                    ])),
            ]));

        Assert.Equal(left, right);
        Assert.Equal(left.GetHashCode(), right.GetHashCode());
    }

    [Fact]
    public void EqualityOperatorReturnsTrueForEqualObjects()
    {
        PdfIndirectObject left = new(15, 2, new PdfInteger(42));
        PdfIndirectObject right = new(15, 2, new PdfInteger(42));

        Assert.True(left == right);
        Assert.False(left != right);
    }

    [Fact]
    public void EqualityOperatorSupportsNullOperands()
    {
        PdfIndirectObject? left = null;
        PdfIndirectObject? right = null;
        PdfIndirectObject value = new(1, 0, PdfNull.Instance);

        Assert.True(left == right);
        Assert.False(left != right);
        Assert.False(left == value);
        Assert.True(left != value);
    }
}

