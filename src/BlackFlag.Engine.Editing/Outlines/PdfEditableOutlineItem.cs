using BlackFlag.Engine.Document.Outlines;
using BlackFlag.Engine.Editing.Ownership;

namespace BlackFlag.Engine.Editing.Outlines;

/// <summary>Represents a stable editing-session handle for one document-outline item.</summary>
public sealed class PdfEditableOutlineItem
{
    private readonly PdfOutlineItem item;

    internal PdfEditableOutlineItem(
        PdfOutlineItem item,
        PdfEditingOwnershipToken owner)
    {
        ArgumentNullException.ThrowIfNull(item);
        this.item = item;
        Owner = owner;
        Children = new PdfEditableOutlineItemCollection(item, owner);
    }

    public Guid Id => item.Id;
    public string Title => item.Title;
    public bool IsOpen => item.IsOpen;
    public PdfOutlineDestination? Destination => item.Destination;
    public PdfOutlineNavigationAction? Action => item.Action;
    public Guid? ParentItemId => item.Parent?.Id;
    public bool IsChild => item.Parent is not null;
    public PdfEditableOutlineItemCollection Children { get; }

    internal PdfEditingOwnershipToken Owner { get; }
    internal PdfOutlineItem GetSourceItem() => item;
}
