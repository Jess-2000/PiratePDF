namespace BlackFlag.Engine.Rendering.DisplayList;

/// <summary>Represents an immutable ordered semantic page display list.</summary>
public sealed class PdfDisplayList : IReadOnlyList<PdfDisplayItem>
{
    private readonly PdfDisplayItem[] items;

    /// <summary>Initializes an immutable display list.</summary>
    public PdfDisplayList(IEnumerable<PdfDisplayItem> items)
    {
        ArgumentNullException.ThrowIfNull(items);
        this.items = items.ToArray();
    }

    /// <inheritdoc />
    public int Count => items.Length;

    /// <inheritdoc />
    public PdfDisplayItem this[int index] => items[index];

    /// <inheritdoc />
    public IEnumerator<PdfDisplayItem> GetEnumerator() =>
        ((IEnumerable<PdfDisplayItem>)items).GetEnumerator();

    /// <inheritdoc />
    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() =>
        items.GetEnumerator();
}
