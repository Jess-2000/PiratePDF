namespace BlackFlag.Engine.Objects.Tests;

using System.Collections;
using Xunit;

public sealed class PdfArrayTests
{
    [Fact]
    public void EmptyReturnsCanonicalInstance()
    {
        Assert.Same(PdfArray.Empty, PdfArray.Empty);
        Assert.Empty(PdfArray.Empty);
    }

    [Fact]
    public void TypeReturnsArray()
    {
        Assert.Equal(PdfObjectType.Array, PdfArray.Empty.Type);
    }

    [Fact]
    public void ConstructorRejectsNullEnumerable()
    {
        Assert.Throws<ArgumentNullException>(
            () => new PdfArray(null!));
    }

    [Fact]
    public void ConstructorRejectsNullElement()
    {
        PdfObject[] values =
        [
            new PdfInteger(1),
            null!,
        ];

        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(
            () => new PdfArray(values));

        Assert.Equal("items", exception.ParamName);
    }

    [Fact]
    public void ConstructorAcceptsPdfNull()
    {
        PdfArray array = new([PdfNull.Instance]);

        Assert.Same(PdfNull.Instance, array[0]);
    }

    [Fact]
    public void ConstructorMaterializesEnumerableOnce()
    {
        CountingEnumerable source = new(
        [
            new PdfInteger(1),
            new PdfInteger(2),
        ]);

        PdfArray array = new(source);

        Assert.Equal(1, source.EnumerationCount);
        Assert.Equal(2, array.Count);
    }

    [Fact]
    public void ConstructorCreatesDefensiveSnapshot()
    {
        List<PdfObject> source =
        [
            new PdfInteger(1),
            new PdfInteger(2),
        ];

        PdfArray array = new(source);
        source[0] = new PdfInteger(99);
        source.Add(new PdfInteger(3));

        Assert.Equal(2, array.Count);
        Assert.Equal(new PdfInteger(1), array[0]);
    }

    [Fact]
    public void IndexerReturnsObjectsInOriginalOrder()
    {
        PdfObject first = new PdfInteger(1);
        PdfObject second = new PdfName("Second");
        PdfArray array = new([first, second]);

        Assert.Same(first, array[0]);
        Assert.Same(second, array[1]);
    }

    [Fact]
    public void IndexerRejectsNegativeIndex()
    {
        PdfArray array = new([new PdfInteger(1)]);

        Assert.Throws<IndexOutOfRangeException>(() => array[-1]);
    }

    [Fact]
    public void IndexerRejectsIndexEqualToCount()
    {
        PdfArray array = new([new PdfInteger(1)]);

        Assert.Throws<IndexOutOfRangeException>(() => array[array.Count]);
    }

    [Fact]
    public void ConcreteEnumerationPreservesOrder()
    {
        PdfObject first = new PdfInteger(1);
        PdfObject second = new PdfInteger(2);
        PdfArray array = new([first, second]);
        List<PdfObject> actual = [];

        foreach (PdfObject value in array)
        {
            actual.Add(value);
        }

        Assert.Equal([first, second], actual);
    }

    [Fact]
    public void GenericInterfaceEnumerationPreservesOrder()
    {
        PdfArray array = new(
        [
            new PdfInteger(1),
            new PdfInteger(2),
        ]);

        IReadOnlyList<PdfObject> view = array;

        Assert.Equal(array, view);
    }

    [Fact]
    public void NonGenericInterfaceEnumerationPreservesOrder()
    {
        PdfArray array = new(
        [
            new PdfInteger(1),
            new PdfInteger(2),
        ]);

        IEnumerable view = array;
        List<PdfObject> actual = [];

        foreach (object? value in view)
        {
            actual.Add(Assert.IsAssignableFrom<PdfObject>(value));
        }

        Assert.Equal(array, actual);
    }

    [Fact]
    public void EqualsReturnsTrueForSameReference()
    {
        PdfArray array = new([new PdfInteger(1)]);

        Assert.True(array.Equals(array));
    }

    [Fact]
    public void EqualsReturnsFalseForNull()
    {
        Assert.False(PdfArray.Empty.Equals(null));
    }

    [Fact]
    public void EqualsReturnsFalseForDifferentObjectType()
    {
        Assert.False(PdfArray.Empty.Equals(PdfNull.Instance));
    }

    [Fact]
    public void EqualArraysUseElementValueEquality()
    {
        PdfArray left = new(
        [
            new PdfInteger(1),
            new PdfName("Name"),
        ]);

        PdfArray right = new(
        [
            new PdfInteger(1),
            new PdfName("Name"),
        ]);

        Assert.Equal(left, right);
        Assert.Equal(left.GetHashCode(), right.GetHashCode());
    }

    [Fact]
    public void EqualityIsOrderSensitive()
    {
        PdfArray left = new(
        [
            new PdfInteger(1),
            new PdfInteger(2),
        ]);

        PdfArray right = new(
        [
            new PdfInteger(2),
            new PdfInteger(1),
        ]);

        Assert.NotEqual(left, right);
    }

    [Fact]
    public void EqualityRejectsDifferentCounts()
    {
        PdfArray left = new([new PdfInteger(1)]);
        PdfArray right = new(
        [
            new PdfInteger(1),
            new PdfInteger(2),
        ]);

        Assert.NotEqual(left, right);
    }

    [Fact]
    public void NestedArraysUseRecursiveValueEquality()
    {
        PdfArray left = new(
        [
            new PdfArray([new PdfInteger(1)]),
        ]);

        PdfArray right = new(
        [
            new PdfArray([new PdfInteger(1)]),
        ]);

        Assert.Equal(left, right);
        Assert.Equal(left.GetHashCode(), right.GetHashCode());
    }

    [Fact]
    public void AcceptRejectsNullVisitor()
    {
        Assert.Throws<ArgumentNullException>(
            () => PdfArray.Empty.Accept<string>(null!));
    }

    [Fact]
    public void AcceptDispatchesToArrayOverload()
    {
        CollectionVisitor visitor = new();

        string result = PdfArray.Empty.Accept(visitor);

        Assert.Equal("Array", result);
    }

    [Fact]
    public void DefaultVisitorOverloadDelegatesToObjectFallback()
    {
        FallbackVisitor visitor = new();

        string result = visitor.Visit(PdfArray.Empty);

        Assert.Equal("Object", result);
    }

    [Fact]
    public void VisitorArrayOverloadRejectsNull()
    {
        FallbackVisitor visitor = new();

        Assert.Throws<ArgumentNullException>(
            () => visitor.Visit((PdfArray)null!));
    }

    private sealed class CollectionVisitor : PdfObjectVisitor<string>
    {
        public override string Visit(PdfObject value) => "Object";

        public override string Visit(PdfArray value) => "Array";
    }

    private sealed class FallbackVisitor : PdfObjectVisitor<string>
    {
        public override string Visit(PdfObject value) => "Object";
    }

    private sealed class CountingEnumerable : IEnumerable<PdfObject>
    {
        private readonly IReadOnlyList<PdfObject> values;

        public CountingEnumerable(IReadOnlyList<PdfObject> values)
        {
            this.values = values;
        }

        public int EnumerationCount { get; private set; }

        public IEnumerator<PdfObject> GetEnumerator()
        {
            EnumerationCount++;

            for (int index = 0; index < values.Count; index++)
            {
                yield return values[index];
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
