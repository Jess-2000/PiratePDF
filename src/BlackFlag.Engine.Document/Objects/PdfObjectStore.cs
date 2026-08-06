namespace BlackFlag.Engine.Document.Objects;

/// <summary>
/// Owns the indirect objects associated with a PDF document.
/// </summary>
public sealed class PdfObjectStore
{
    private readonly Dictionary<PdfObjectIdentifier, PdfObject>
        objects =
            new();

    public int Count =>
        objects.Count;

    public IEnumerable<PdfObjectIdentifier> Identifiers =>
        objects.Keys
            .OrderBy(identifier => identifier.ObjectNumber)
            .ThenBy(identifier => identifier.GenerationNumber);

    public void Add(
        PdfObjectIdentifier identifier,
        PdfObject value)
    {
        ArgumentNullException.ThrowIfNull(value);

        if (!objects.TryAdd(
                identifier,
                value))
        {
            throw new InvalidOperationException(
                $"Object '{identifier}' already exists in the store.");
        }
    }

    public void Set(
        PdfObjectIdentifier identifier,
        PdfObject value)
    {
        ArgumentNullException.ThrowIfNull(value);

        objects[identifier] = value;
    }

    public PdfObject Get(
        PdfObjectIdentifier identifier)
    {
        if (!objects.TryGetValue(
                identifier,
                out var value))
        {
            throw new KeyNotFoundException(
                $"Object '{identifier}' does not exist in the store.");
        }

        return value;
    }

    public T Get<T>(
        PdfObjectIdentifier identifier)
        where T : PdfObject
    {
        var value = Get(identifier);

        if (value is not T typedValue)
        {
            throw new InvalidCastException(
                $"Object '{identifier}' is '{value.GetType().Name}', " +
                $"not '{typeof(T).Name}'.");
        }

        return typedValue;
    }

    public bool TryGet(
        PdfObjectIdentifier identifier,
        out PdfObject? value)
    {
        return objects.TryGetValue(
            identifier,
            out value);
    }

    public bool Contains(
        PdfObjectIdentifier identifier)
    {
        return objects.ContainsKey(identifier);
    }

    public bool Remove(
        PdfObjectIdentifier identifier)
    {
        return objects.Remove(identifier);
    }

    public IReadOnlyDictionary<PdfObjectIdentifier, PdfObject>
        CreateSnapshot()
    {
        return new Dictionary<PdfObjectIdentifier, PdfObject>(
            objects);
    }
}
