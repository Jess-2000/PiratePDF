using System.Collections;
using BlackFlag.Engine.Document.Outlines;
using BlackFlag.Engine.Editing.Ownership;

namespace BlackFlag.Engine.Editing.Outlines;

/// <summary>Exposes stable editable handles for one ordered outline-item collection.</summary>
public sealed class PdfEditableOutlineItemCollection : IReadOnlyList<PdfEditableOutlineItem>
{
    private readonly List<PdfEditableOutlineItem> items;
    private readonly PdfOutline? outline;
    private readonly PdfOutlineItem? parent;

    internal PdfEditableOutlineItemCollection(
        PdfOutline outline,
        PdfEditingOwnershipToken owner)
    {
        ArgumentNullException.ThrowIfNull(outline);
        this.outline = outline;
        items = CreateHandles(outline.Items, owner);
    }

    internal PdfEditableOutlineItemCollection(
        PdfOutlineItem parent,
        PdfEditingOwnershipToken owner)
    {
        ArgumentNullException.ThrowIfNull(parent);
        this.parent = parent;
        items = CreateHandles(parent.Children, owner);
    }

    public int Count => items.Count;
    public PdfEditableOutlineItem this[int index] => items[index];

    public bool Contains(PdfEditableOutlineItem item)
    {
        ArgumentNullException.ThrowIfNull(item);
        return items.Contains(item);
    }

    public int IndexOf(PdfEditableOutlineItem item)
    {
        ArgumentNullException.ThrowIfNull(item);
        return items.IndexOf(item);
    }

    public IEnumerator<PdfEditableOutlineItem> GetEnumerator() => items.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    internal void Insert(int index, PdfEditableOutlineItem item)
    {
        ArgumentNullException.ThrowIfNull(item);
        EnsureSynchronized();

        if (index < 0 || index > items.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }

        if (items.Contains(item))
        {
            throw new InvalidOperationException("The outline item is already attached to this collection.");
        }

        if (outline is not null)
        {
            outline.InsertItem(index, item.GetSourceItem());
        }
        else
        {
            parent!.InsertChild(index, item.GetSourceItem());
        }

        items.Insert(index, item);
    }

    internal int Remove(PdfEditableOutlineItem item)
    {
        ArgumentNullException.ThrowIfNull(item);
        EnsureSynchronized();
        var index = items.IndexOf(item);

        if (index < 0)
        {
            throw new InvalidOperationException("The outline item is not attached to this collection.");
        }

        if (outline is not null)
        {
            _ = outline.RemoveItem(item.GetSourceItem());
        }
        else
        {
            _ = parent!.RemoveChild(item.GetSourceItem());
        }

        items.RemoveAt(index);
        return index;
    }

    private void EnsureSynchronized()
    {
        var source = outline is not null ? outline.Items : parent!.Children;

        if (items.Count != source.Count)
        {
            throw new InvalidOperationException(
                "The authoritative outline collection changed outside the editing session.");
        }

        for (var index = 0; index < items.Count; index++)
        {
            if (!ReferenceEquals(items[index].GetSourceItem(), source[index]))
            {
                throw new InvalidOperationException(
                    "The authoritative outline collection changed outside the editing session.");
            }
        }
    }

    private static List<PdfEditableOutlineItem> CreateHandles(
        IReadOnlyList<PdfOutlineItem> source,
        PdfEditingOwnershipToken owner)
    {
        var result = new List<PdfEditableOutlineItem>(source.Count);

        for (var index = 0; index < source.Count; index++)
        {
            result.Add(new PdfEditableOutlineItem(source[index], owner));
        }

        return result;
    }
}
