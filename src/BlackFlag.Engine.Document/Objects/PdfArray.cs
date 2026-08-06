using System.Collections;

namespace BlackFlag.Engine.Document.Objects;

/// <summary>
/// Represents an ordered PDF array object.
/// </summary>
public sealed class PdfArray :
    PdfObject,
    IReadOnlyList<PdfObject>
{
    private readonly List<PdfObject> items;

    public PdfArray()
        : this([])
    {
    }

    public PdfArray(
        IEnumerable<PdfObject> items)
        : base(PdfObjectKind.ArrayValue)
    {
        ArgumentNullException.ThrowIfNull(items);

        this.items = [];

        foreach (var item in items)
        {
            ArgumentNullException.ThrowIfNull(item);

            this.items.Add(item);
        }
    }

    public int Count =>
        items.Count;

    public PdfObject this[int index] =>
        items[index];

    public void Add(PdfObject value)
    {
        ArgumentNullException.ThrowIfNull(value);

        items.Add(value);
    }

    public void Insert(
        int index,
        PdfObject value)
    {
        ArgumentNullException.ThrowIfNull(value);

        items.Insert(
            index,
            value);
    }

    public void Set(
        int index,
        PdfObject value)
    {
        ArgumentNullException.ThrowIfNull(value);

        items[index] = value;
    }

    public bool Remove(PdfObject value)
    {
        ArgumentNullException.ThrowIfNull(value);

        return items.Remove(value);
    }

    public PdfObject RemoveAt(int index)
    {
        var value = items[index];

        items.RemoveAt(index);

        return value;
    }

    public void Clear()
    {
        items.Clear();
    }

    public T Get<T>(int index)
        where T : PdfObject
    {
        var value = items[index];

        if (value is not T typedValue)
        {
            throw new InvalidCastException(
                $"Array item {index} is '{value.GetType().Name}', " +
                $"not '{typeof(T).Name}'.");
        }

        return typedValue;
    }

    public PdfArray CreateCopy()
    {
        return new PdfArray(items);
    }

    public IEnumerator<PdfObject> GetEnumerator()
    {
        return items.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}