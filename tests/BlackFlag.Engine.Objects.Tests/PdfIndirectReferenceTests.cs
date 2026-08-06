namespace BlackFlag.Engine.Objects.Tests;

using Xunit;

public sealed class PdfIndirectReferenceTests
{
    [Fact]
    public void ConstructorStoresValidValues()
    {
        PdfIndirectReference reference = new(15, 2);

        Assert.Equal(15, reference.ObjectNumber);
        Assert.Equal(2, reference.GenerationNumber);
    }

    [Fact]
    public void TypeReturnsIndirectReference()
    {
        PdfIndirectReference reference = new(1, 0);

        Assert.Equal(PdfObjectType.IndirectReference, reference.Type);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(int.MinValue)]
    public void ConstructorRejectsInvalidObjectNumber(int objectNumber)
    {
        ArgumentOutOfRangeException exception =
            Assert.Throws<ArgumentOutOfRangeException>(
                () => new PdfIndirectReference(objectNumber, 0));

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
                () => new PdfIndirectReference(1, generationNumber));

        Assert.Equal("generationNumber", exception.ParamName);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(65535)]
    public void ConstructorAcceptsGenerationBoundaries(int generationNumber)
    {
        PdfIndirectReference reference = new(1, generationNumber);

        Assert.Equal(generationNumber, reference.GenerationNumber);
    }

    [Fact]
    public void EqualsReturnsTrueForSameReference()
    {
        PdfIndirectReference reference = new(1, 0);

        Assert.True(reference.Equals(reference));
    }

    [Fact]
    public void EqualsReturnsFalseForNull()
    {
        PdfIndirectReference reference = new(1, 0);

        Assert.False(reference.Equals(null));
    }

    [Fact]
    public void EqualsReturnsFalseForDifferentObjectType()
    {
        PdfIndirectReference reference = new(1, 0);

        Assert.False(reference.Equals(PdfNull.Instance));
    }

    [Fact]
    public void EqualReferencesUseObjectAndGenerationNumbers()
    {
        PdfIndirectReference left = new(15, 2);
        PdfIndirectReference right = new(15, 2);

        Assert.Equal(left, right);
        Assert.Equal(left.GetHashCode(), right.GetHashCode());
    }

    [Fact]
    public void EqualityRejectsDifferentObjectNumbers()
    {
        PdfIndirectReference left = new(15, 2);
        PdfIndirectReference right = new(16, 2);

        Assert.NotEqual(left, right);
    }

    [Fact]
    public void EqualityRejectsDifferentGenerationNumbers()
    {
        PdfIndirectReference left = new(15, 2);
        PdfIndirectReference right = new(15, 3);

        Assert.NotEqual(left, right);
    }

    [Fact]
    public void EqualityOperatorReturnsTrueForEqualReferences()
    {
        PdfIndirectReference left = new(15, 2);
        PdfIndirectReference right = new(15, 2);

        Assert.True(left == right);
        Assert.False(left != right);
    }

    [Fact]
    public void EqualityOperatorSupportsNullOperands()
    {
        PdfIndirectReference? left = null;
        PdfIndirectReference? right = null;
        PdfIndirectReference value = new(1, 0);

        Assert.True(left == right);
        Assert.False(left != right);
        Assert.False(left == value);
        Assert.True(left != value);
    }

    [Fact]
    public void AcceptRejectsNullVisitor()
    {
        PdfIndirectReference reference = new(1, 0);

        Assert.Throws<ArgumentNullException>(
            () => reference.Accept<string>(null!));
    }

    [Fact]
    public void AcceptDispatchesToIndirectReferenceOverload()
    {
        PdfIndirectReference reference = new(1, 0);
        IndirectReferenceVisitor visitor = new();

        string result = reference.Accept(visitor);

        Assert.Equal("IndirectReference", result);
    }

    [Fact]
    public void DefaultVisitorOverloadDelegatesToObjectFallback()
    {
        PdfIndirectReference reference = new(1, 0);
        FallbackVisitor visitor = new();

        string result = visitor.Visit(reference);

        Assert.Equal("Object", result);
    }

    [Fact]
    public void VisitorIndirectReferenceOverloadRejectsNull()
    {
        FallbackVisitor visitor = new();

        Assert.Throws<ArgumentNullException>(
            () => visitor.Visit((PdfIndirectReference)null!));
    }

    private sealed class IndirectReferenceVisitor :
        PdfObjectVisitor<string>
    {
        public override string Visit(PdfObject value) => "Object";

        public override string Visit(PdfIndirectReference value) =>
            "IndirectReference";
    }

    private sealed class FallbackVisitor : PdfObjectVisitor<string>
    {
        public override string Visit(PdfObject value) => "Object";
    }
}
