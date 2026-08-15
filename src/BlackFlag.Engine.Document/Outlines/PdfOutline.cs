namespace BlackFlag.Engine.Document.Outlines;

/// <summary>Represents the authoritative ordered root of the PDF document outline.</summary>
public sealed class PdfOutline
{
    private readonly List<PdfOutlineItem> items = [];
    private readonly IReadOnlyList<PdfOutlineItem> readOnlyItems;

    public PdfOutline()
    {
        readOnlyItems = items.AsReadOnly();
    }

    /// <summary>Gets the ordered root outline items.</summary>
    public IReadOnlyList<PdfOutlineItem> Items => readOnlyItems;

    internal void InsertItem(int index, PdfOutlineItem item)
    {
        ArgumentNullException.ThrowIfNull(item);

        if (index < 0 || index > items.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }

        if (items.Contains(item))
        {
            throw new InvalidOperationException("The outline item is already attached at the outline root.");
        }

        if (item.Parent is not null)
        {
            throw new InvalidOperationException("A child outline item cannot also be attached at the outline root.");
        }

        items.Insert(index, item);
    }

    internal int RemoveItem(PdfOutlineItem item)
    {
        ArgumentNullException.ThrowIfNull(item);
        var index = items.IndexOf(item);

        if (index < 0)
        {
            throw new InvalidOperationException("The outline item is not attached at the outline root.");
        }

        items.RemoveAt(index);
        return index;
    }
}
