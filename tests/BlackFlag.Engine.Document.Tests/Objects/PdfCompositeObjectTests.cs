using BlackFlag.Engine.Document.Objects;
using BlackFlag.Engine.Document.Objects.Primitives;

namespace BlackFlag.Engine.Document.Tests.Objects;

public sealed class PdfCompositeObjectTests
{
    [Fact]
    public void PdfArray_PreservesItemOrder()
    {
        var array = new PdfArray(
        [
            new PdfInteger(10),
            new PdfBoolean(true),
            PdfNull.Instance
        ]);

        Assert.Equal(
            3,
            array.Count);

        Assert.Equal(
            10,
            array.Get<PdfInteger>(0).Value);

        Assert.True(
            array.Get<PdfBoolean>(1).Value);

        Assert.Same(
            PdfNull.Instance,
            array[2]);
    }

    [Fact]
    public void PdfArray_CopyOwnsIndependentList()
    {
        var original = new PdfArray(
        [
            new PdfInteger(1)
        ]);

        var copy = original.CreateCopy();

        copy.Add(
            new PdfInteger(2));

        Assert.Single(original);
        Assert.Equal(2, copy.Count);
    }

    [Fact]
    public void PdfDictionary_AddGetSetAndRemove_ManagesEntries()
    {
        var dictionary = new PdfDictionary();

        dictionary.Add(
            PdfNames.Count,
            new PdfInteger(2));

        Assert.Equal(
            2,
            dictionary
                .Get<PdfInteger>(PdfNames.Count)
                .Value);

        dictionary.Set(
            PdfNames.Count,
            new PdfInteger(3));

        Assert.Equal(
            3,
            dictionary
                .Get<PdfInteger>(PdfNames.Count)
                .Value);

        Assert.True(
            dictionary.Remove(PdfNames.Count));

        Assert.False(
            dictionary.ContainsKey(PdfNames.Count));
    }

    [Fact]
    public void PdfDictionary_AddDuplicateKey_Throws()
    {
        var dictionary = new PdfDictionary();

        dictionary.Add(
            PdfNames.Type,
            PdfNames.Catalog);

        Assert.Throws<InvalidOperationException>(
            () => dictionary.Add(
                PdfNames.Type,
                PdfNames.Page));
    }

    [Fact]
    public void PdfDictionary_EnumerationIsDeterministic()
    {
        var dictionary = new PdfDictionary();

        dictionary.Add(
            PdfName.FromLatin1("Zulu"),
            PdfNull.Instance);

        dictionary.Add(
            PdfName.FromLatin1("Alpha"),
            PdfNull.Instance);

        dictionary.Add(
            PdfName.FromLatin1("Middle"),
            PdfNull.Instance);

        var keys = dictionary.Keys
            .Select(key => key.GetLatin1Text())
            .ToArray();

        Assert.Equal(
        [
            "Alpha",
            "Middle",
            "Zulu"
        ],
            keys);
    }

    [Fact]
    public void PdfStream_DefensivelyCopiesDictionaryAndData()
    {
        var dictionary = new PdfDictionary();

        dictionary.Add(
            PdfNames.Length,
            new PdfInteger(3));

        var sourceData = new byte[]
        {
            1,
            2,
            3
        };

        var stream = new PdfStream(
            dictionary,
            sourceData);

        dictionary.Set(
            PdfNames.Length,
            new PdfInteger(99));

        sourceData[0] = 9;

        Assert.Equal(
            3,
            stream.Dictionary
                .Get<PdfInteger>(PdfNames.Length)
                .Value);

        Assert.Equal(
            [1, 2, 3],
            stream.ToByteArray());

        var returnedData =
            stream.ToByteArray();

        returnedData[0] = 9;

        Assert.Equal(
            [1, 2, 3],
            stream.ToByteArray());
    }

    [Fact]
    public void PdfIndirectReference_ResolvesStoredObject()
    {
        var store = new PdfObjectStore();
        var identifier =
            new PdfObjectIdentifier(12, 3);

        store.Add(
            identifier,
            new PdfInteger(42));

        var reference =
            new PdfIndirectReference(identifier);

        var resolved =
            reference.Resolve<PdfInteger>(store);

        Assert.Equal(
            42,
            resolved.Value);

        Assert.Equal(
            "12 3 R",
            reference.ToString());
    }

    [Fact]
    public void PdfIndirectReference_WithMatchingIdentifier_IsValueEqual()
    {
        var first =
            new PdfIndirectReference(5, 0);

        var second =
            new PdfIndirectReference(5, 0);

        Assert.Equal(first, second);
        Assert.True(first == second);

        Assert.Equal(
            first.GetHashCode(),
            second.GetHashCode());
    }
}