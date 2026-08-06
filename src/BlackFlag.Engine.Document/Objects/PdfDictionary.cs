using System.Collections;

using BlackFlag.Engine.Document.Objects.Primitives;

namespace BlackFlag.Engine.Document.Objects;

/// <summary>
/// Represents a PDF dictionary object.
/// </summary>
public sealed class PdfDictionary :
    PdfObject,
    IReadOnlyDictionary<PdfName, PdfObject>
{
    private readonly Dictionary<PdfName, PdfObject> entries;

    public PdfDictionary()
        : this([])
    {
    }

    public PdfDictionary(
        IEnumerable<KeyValuePair<PdfName, PdfObject>> entries)
        : base(PdfObjectKind.DictionaryValue)
    {
        ArgumentNullException.ThrowIfNull(entries);

        this.entries = [];

        foreach (var entry in entries)
        {
            Add(
                entry.Key,
                entry.Value);
        }
    }

    public int Count =>
        entries.Count;

    public IEnumerable<PdfName> Keys =>
        GetOrderedEntries()
            .Select(entry => entry.Key);

    public IEnumerable<PdfObject> Values =>
        GetOrderedEntries()
            .Select(entry => entry.Value);

    public PdfObject this[PdfName key]
    {
        get
        {
            ArgumentNullException.ThrowIfNull(key);

            return entries[key];
        }
    }

    public void Add(
        PdfName key,
        PdfObject value)
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(value);

        if (!entries.TryAdd(
                key,
                value))
        {
            throw new InvalidOperationException(
                $"Dictionary key '{key}' already exists.");
        }
    }

    public void Set(
        PdfName key,
        PdfObject value)
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(value);

        entries[key] = value;
    }

    public PdfObject Get(PdfName key)
    {
        ArgumentNullException.ThrowIfNull(key);

        if (!entries.TryGetValue(
                key,
                out var value))
        {
            throw new KeyNotFoundException(
                $"Dictionary key '{key}' does not exist.");
        }

        return value;
    }

    public T Get<T>(PdfName key)
        where T : PdfObject
    {
        var value = Get(key);

        if (value is not T typedValue)
        {
            throw new InvalidCastException(
                $"Dictionary value '{key}' is " +
                $"'{value.GetType().Name}', not '{typeof(T).Name}'.");
        }

        return typedValue;
    }

    public T? GetOptional<T>(PdfName key)
        where T : PdfObject
    {
        ArgumentNullException.ThrowIfNull(key);

        if (!entries.TryGetValue(
                key,
                out var value))
        {
            return null;
        }

        if (value is not T typedValue)
        {
            throw new InvalidCastException(
                $"Dictionary value '{key}' is " +
                $"'{value.GetType().Name}', not '{typeof(T).Name}'.");
        }

        return typedValue;
    }

    public bool ContainsKey(PdfName key)
    {
        ArgumentNullException.ThrowIfNull(key);

        return entries.ContainsKey(key);
    }

    public bool TryGetValue(
        PdfName key,
        out PdfObject value)
    {
        ArgumentNullException.ThrowIfNull(key);

        return entries.TryGetValue(
            key,
            out value!);
    }

    public bool Remove(PdfName key)
    {
        ArgumentNullException.ThrowIfNull(key);

        return entries.Remove(key);
    }

    public void Clear()
    {
        entries.Clear();
    }

    public PdfDictionary CreateCopy()
    {
        return new PdfDictionary(entries);
    }

    public IEnumerator<KeyValuePair<PdfName, PdfObject>>
        GetEnumerator()
    {
        return GetOrderedEntries().GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    private IEnumerable<KeyValuePair<PdfName, PdfObject>>
        GetOrderedEntries()
    {
        return entries
            .OrderBy(
                entry => entry.Key.GetLatin1Text(),
                StringComparer.Ordinal);
    }
}
