namespace BlackFlag.Engine.Document.Outlines;

/// <summary>Represents the authoritative logical state of one document-outline item.</summary>
public sealed class PdfOutlineItem
{
    private readonly List<PdfOutlineItem> children = [];
    private readonly IReadOnlyList<PdfOutlineItem> readOnlyChildren;
    private PdfOutlineItem? parent;

    public PdfOutlineItem(string title)
    {
        ArgumentNullException.ThrowIfNull(title);
        Id = Guid.NewGuid();
        Title = title;
        readOnlyChildren = children.AsReadOnly();
    }

    /// <summary>Gets the stable logical identity of the outline item.</summary>
    public Guid Id { get; }

    /// <summary>Gets the visible outline title.</summary>
    public string Title { get; internal set; }

    /// <summary>Gets whether the item's children are logically expanded.</summary>
    public bool IsOpen { get; internal set; }

    /// <summary>Gets the direct page destination, if present.</summary>
    public PdfOutlineDestination? Destination { get; internal set; }

    /// <summary>Gets the navigation action, if present.</summary>
    public PdfOutlineNavigationAction? Action { get; internal set; }

    /// <summary>Gets the ordered child outline items.</summary>
    public IReadOnlyList<PdfOutlineItem> Children => readOnlyChildren;

    internal PdfOutlineItem? Parent => parent;

    internal void InsertChild(int index, PdfOutlineItem child)
    {
        ArgumentNullException.ThrowIfNull(child);

        if (index < 0 || index > children.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }

        if (ReferenceEquals(child, this))
        {
            throw new InvalidOperationException("An outline item cannot be its own child.");
        }

        for (var ancestor = this; ancestor is not null; ancestor = ancestor.parent)
        {
            if (ReferenceEquals(ancestor, child))
            {
                throw new InvalidOperationException("The outline hierarchy cannot contain a cycle.");
            }
        }

        if (child.parent is not null)
        {
            throw new InvalidOperationException("The outline item is already attached to a parent item.");
        }

        children.Insert(index, child);
        child.parent = this;
    }

    internal int RemoveChild(PdfOutlineItem child)
    {
        ArgumentNullException.ThrowIfNull(child);
        var index = children.IndexOf(child);

        if (index < 0)
        {
            throw new InvalidOperationException("The outline item is not a child of this item.");
        }

        children.RemoveAt(index);
        child.parent = null;
        return index;
    }
}
