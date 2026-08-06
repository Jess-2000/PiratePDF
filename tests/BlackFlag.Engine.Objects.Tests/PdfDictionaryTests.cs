namespace BlackFlag.Engine.Objects.Tests;

using System.Collections;
using Xunit;

public sealed class PdfDictionaryTests
{
    private static readonly PdfName FirstKey = new("First");
    private static readonly PdfName SecondKey = new("Second");

    [Fact]
    public void EmptyReturnsCanonicalInstance()
    {
        Assert.Same(PdfDictionary.Empty, PdfDictionary.Empty);
        Assert.Empty(PdfDictionary.Empty);
    }

    [Fact]
    public void TypeReturnsDictionary()
    {
        Assert.Equal(PdfObjectType.Dictionary, PdfDictionary.Empty.Type);
    }

    [Fact]
    public void ConstructorRejectsNullEnumerable()
    {
        Assert.Throws<ArgumentNullException>(
            () => new PdfDictionary(null!));
    }

    [Fact]
    public void ConstructorRejectsNullKey()
    {
        KeyValuePair<PdfName, PdfObject>[] entries =
        [
            new(null!, new PdfInteger(1)),
        ];

        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(
            () => new PdfDictionary(entries));

        Assert.Equal("entries", exception.ParamName);
    }

    [Fact]
    public void ConstructorRejectsNullValue()
    {
        KeyValuePair<PdfName, PdfObject>[] entries =
        [
            new(FirstKey, null!),
        ];

        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(
            () => new PdfDictionary(entries));

        Assert.Equal("entries", exception.ParamName);
    }

    [Fact]
    public void ConstructorAcceptsPdfNullValue()
    {
        PdfDictionary dictionary = new(
        [
            new(FirstKey, PdfNull.Instance),
        ]);

        Assert.Same(PdfNull.Instance, dictionary[FirstKey]);
    }

    [Fact]
    public void ConstructorRejectsDuplicateEqualKeys()
    {
        KeyValuePair<PdfName, PdfObject>[] entries =
        [
            new(new PdfName("Key"), new PdfInteger(1)),
            new(new PdfName("Key"), new PdfInteger(2)),
        ];

        ArgumentException exception = Assert.Throws<ArgumentException>(
            () => new PdfDictionary(entries));

        Assert.Equal("entries", exception.ParamName);
    }

    [Fact]
    public void DuplicateKeyComparisonIsCaseSensitive()
    {
        PdfDictionary dictionary = new(
        [
            new(new PdfName("Key"), new PdfInteger(1)),
            new(new PdfName("key"), new PdfInteger(2)),
        ]);

        Assert.Equal(2, dictionary.Count);
    }

    [Fact]
    public void ConstructorMaterializesEnumerableOnce()
    {
        CountingEnumerable source = new(
        [
            new(FirstKey, new PdfInteger(1)),
            new(SecondKey, new PdfInteger(2)),
        ]);

        PdfDictionary dictionary = new(source);

        Assert.Equal(1, source.EnumerationCount);
        Assert.Equal(2, dictionary.Count);
    }

    [Fact]
    public void ConstructorCreatesDefensiveSnapshot()
    {
        List<KeyValuePair<PdfName, PdfObject>> source =
        [
            new(FirstKey, new PdfInteger(1)),
        ];

        PdfDictionary dictionary = new(source);
        source[0] = new(FirstKey, new PdfInteger(99));
        source.Add(new(SecondKey, new PdfInteger(2)));

        Assert.Single(dictionary);
        Assert.Equal(new PdfInteger(1), dictionary[FirstKey]);
    }

    [Fact]
    public void IndexerUsesPdfNameValueEquality()
    {
        PdfDictionary dictionary = new(
        [
            new(new PdfName("Key"), new PdfInteger(1)),
        ]);

        PdfObject value = dictionary[new PdfName("Key")];

        Assert.Equal(new PdfInteger(1), value);
    }

    [Fact]
    public void IndexerRejectsNullKey()
    {
        Assert.Throws<ArgumentNullException>(
            () => _ = PdfDictionary.Empty[null!]);
    }

    [Fact]
    public void IndexerRejectsMissingKey()
    {
        Assert.Throws<KeyNotFoundException>(
            () => _ = PdfDictionary.Empty[FirstKey]);
    }

    [Fact]
    public void ContainsKeyReturnsTrueForPresentKey()
    {
        PdfDictionary dictionary = CreateDictionary();

        Assert.True(dictionary.ContainsKey(new PdfName("First")));
    }

    [Fact]
    public void ContainsKeyReturnsFalseForAbsentKey()
    {
        Assert.False(PdfDictionary.Empty.ContainsKey(FirstKey));
    }

    [Fact]
    public void ContainsKeyRejectsNullKey()
    {
        Assert.Throws<ArgumentNullException>(
            () => PdfDictionary.Empty.ContainsKey(null!));
    }

    [Fact]
    public void TryGetValueReturnsPresentValue()
    {
        PdfDictionary dictionary = CreateDictionary();

        bool found = dictionary.TryGetValue(
            new PdfName("Second"),
            out PdfObject value);

        Assert.True(found);
        Assert.Equal(new PdfInteger(2), value);
    }

    [Fact]
    public void TryGetValueReturnsFalseForAbsentKey()
    {
        bool found = PdfDictionary.Empty.TryGetValue(
            FirstKey,
            out PdfObject value);

        Assert.False(found);
        Assert.Null(value);
    }

    [Fact]
    public void TryGetValueDistinguishesAbsentKeyFromPdfNull()
    {
        PdfDictionary dictionary = new(
        [
            new(FirstKey, PdfNull.Instance),
        ]);

        bool found = dictionary.TryGetValue(FirstKey, out PdfObject value);

        Assert.True(found);
        Assert.Same(PdfNull.Instance, value);
    }

    [Fact]
    public void TryGetValueRejectsNullKey()
    {
        Assert.Throws<ArgumentNullException>(
            () => PdfDictionary.Empty.TryGetValue(null!, out _));
    }

    [Fact]
    public void KeysPreserveInsertionOrder()
    {
        PdfDictionary dictionary = CreateDictionary();

        Assert.Equal([FirstKey, SecondKey], dictionary.Keys);
    }

    [Fact]
    public void ValuesPreserveInsertionOrder()
    {
        PdfDictionary dictionary = CreateDictionary();

        Assert.Equal(
            [new PdfInteger(1), new PdfInteger(2)],
            dictionary.Values);
    }

    [Fact]
    public void ConcreteEnumerationPreservesInsertionOrder()
    {
        PdfDictionary dictionary = CreateDictionary();
        List<PdfName> actual = [];

        foreach (KeyValuePair<PdfName, PdfObject> entry in dictionary)
        {
            actual.Add(entry.Key);
        }

        Assert.Equal([FirstKey, SecondKey], actual);
    }

    [Fact]
    public void GenericInterfaceEnumerationPreservesInsertionOrder()
    {
        IReadOnlyDictionary<PdfName, PdfObject> dictionary =
            CreateDictionary();

        Assert.Equal([FirstKey, SecondKey], dictionary.Keys);
    }

    [Fact]
    public void NonGenericInterfaceEnumerationPreservesInsertionOrder()
    {
        IEnumerable dictionary = CreateDictionary();
        List<PdfName> actual = [];

        foreach (object? value in dictionary)
        {
            KeyValuePair<PdfName, PdfObject> entry =
                Assert.IsType<KeyValuePair<PdfName, PdfObject>>(value);

            actual.Add(entry.Key);
        }

        Assert.Equal([FirstKey, SecondKey], actual);
    }

    [Fact]
    public void EqualsReturnsTrueForSameReference()
    {
        PdfDictionary dictionary = CreateDictionary();

        Assert.True(dictionary.Equals(dictionary));
    }

    [Fact]
    public void EqualsReturnsFalseForNull()
    {
        Assert.False(PdfDictionary.Empty.Equals(null));
    }

    [Fact]
    public void EqualsReturnsFalseForDifferentObjectType()
    {
        Assert.False(PdfDictionary.Empty.Equals(PdfNull.Instance));
    }

    [Fact]
    public void EqualDictionariesUseKeyAndValueEquality()
    {
        PdfDictionary left = CreateDictionary();
        PdfDictionary right = new(
        [
            new(new PdfName("First"), new PdfInteger(1)),
            new(new PdfName("Second"), new PdfInteger(2)),
        ]);

        Assert.Equal(left, right);
        Assert.Equal(left.GetHashCode(), right.GetHashCode());
    }

    [Fact]
    public void EqualityIsIndependentOfInsertionOrder()
    {
        PdfDictionary left = CreateDictionary();
        PdfDictionary right = new(
        [
            new(SecondKey, new PdfInteger(2)),
            new(FirstKey, new PdfInteger(1)),
        ]);

        Assert.Equal(left, right);
        Assert.Equal(left.GetHashCode(), right.GetHashCode());
    }

    [Fact]
    public void EqualityRejectsDifferentCounts()
    {
        PdfDictionary left = CreateDictionary();
        PdfDictionary right = new(
        [
            new(FirstKey, new PdfInteger(1)),
        ]);

        Assert.NotEqual(left, right);
    }

    [Fact]
    public void EqualityRejectsDifferentValues()
    {
        PdfDictionary left = CreateDictionary();
        PdfDictionary right = new(
        [
            new(FirstKey, new PdfInteger(1)),
            new(SecondKey, new PdfInteger(99)),
        ]);

        Assert.NotEqual(left, right);
    }

    [Fact]
    public void EqualityRejectsDifferentKeys()
    {
        PdfDictionary left = CreateDictionary();
        PdfDictionary right = new(
        [
            new(FirstKey, new PdfInteger(1)),
            new(new PdfName("Other"), new PdfInteger(2)),
        ]);

        Assert.NotEqual(left, right);
    }

    [Fact]
    public void NestedCollectionsUseRecursiveValueEquality()
    {
        PdfDictionary left = new(
        [
            new(
                FirstKey,
                new PdfArray([new PdfInteger(1)])),
        ]);

        PdfDictionary right = new(
        [
            new(
                new PdfName("First"),
                new PdfArray([new PdfInteger(1)])),
        ]);

        Assert.Equal(left, right);
        Assert.Equal(left.GetHashCode(), right.GetHashCode());
    }

    [Fact]
    public void AcceptRejectsNullVisitor()
    {
        Assert.Throws<ArgumentNullException>(
            () => PdfDictionary.Empty.Accept<string>(null!));
    }

    [Fact]
    public void AcceptDispatchesToDictionaryOverload()
    {
        CollectionVisitor visitor = new();

        string result = PdfDictionary.Empty.Accept(visitor);

        Assert.Equal("Dictionary", result);
    }

    [Fact]
    public void DefaultVisitorOverloadDelegatesToObjectFallback()
    {
        FallbackVisitor visitor = new();

        string result = visitor.Visit(PdfDictionary.Empty);

        Assert.Equal("Object", result);
    }

    [Fact]
    public void VisitorDictionaryOverloadRejectsNull()
    {
        FallbackVisitor visitor = new();

        Assert.Throws<ArgumentNullException>(
            () => visitor.Visit((PdfDictionary)null!));
    }

    private static PdfDictionary CreateDictionary() =>
        new(
        [
            new(FirstKey, new PdfInteger(1)),
            new(SecondKey, new PdfInteger(2)),
        ]);

    private sealed class CollectionVisitor : PdfObjectVisitor<string>
    {
        public override string Visit(PdfObject value) => "Object";

        public override string Visit(PdfDictionary value) => "Dictionary";
    }

    private sealed class FallbackVisitor : PdfObjectVisitor<string>
    {
        public override string Visit(PdfObject value) => "Object";
    }

    private sealed class CountingEnumerable :
        IEnumerable<KeyValuePair<PdfName, PdfObject>>
    {
        private readonly IReadOnlyList<
            KeyValuePair<PdfName, PdfObject>> entries;

        public CountingEnumerable(
            IReadOnlyList<KeyValuePair<PdfName, PdfObject>> entries)
        {
            this.entries = entries;
        }

        public int EnumerationCount { get; private set; }

        public IEnumerator<KeyValuePair<PdfName, PdfObject>> GetEnumerator()
        {
            EnumerationCount++;

            for (int index = 0; index < entries.Count; index++)
            {
                yield return entries[index];
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}

