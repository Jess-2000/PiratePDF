using System.Collections;
using BlackFlag.Engine.Document.Decorations;
using BlackFlag.Engine.Editing.Ownership;

namespace BlackFlag.Engine.Editing.Decorations;

/// <summary>Exposes stable editable handles for the ordered document page decorations.</summary>
public sealed class PdfEditablePageDecorationCollection : IReadOnlyList<PdfEditablePageDecoration>
{
    private readonly PdfPageDecorations decorations;
    private readonly List<PdfEditablePageDecoration> items;

    internal PdfEditablePageDecorationCollection(
        PdfPageDecorations decorations,
        PdfEditingOwnershipToken owner)
    {
        ArgumentNullException.ThrowIfNull(decorations);
        this.decorations = decorations;
        items = new List<PdfEditablePageDecoration>(decorations.Items.Count);

        for (var index = 0; index < decorations.Items.Count; index++)
        {
            items.Add(new PdfEditablePageDecoration(decorations.Items[index], owner));
        }
    }

    public int Count => items.Count;
    public PdfEditablePageDecoration this[int index] => items[index];

    public bool Contains(PdfEditablePageDecoration decoration)
    {
        ArgumentNullException.ThrowIfNull(decoration);
        return items.Contains(decoration);
    }

    public int IndexOf(PdfEditablePageDecoration decoration)
    {
        ArgumentNullException.ThrowIfNull(decoration);
        return items.IndexOf(decoration);
    }

    public IEnumerator<PdfEditablePageDecoration> GetEnumerator() => items.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    internal void Insert(int index, PdfEditablePageDecoration decoration)
    {
        ArgumentNullException.ThrowIfNull(decoration);
        EnsureSynchronized();

        if (index < 0 || index > items.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }

        if (items.Contains(decoration))
        {
            throw new InvalidOperationException("The page decoration is already attached to this document.");
        }

        decorations.Insert(index, decoration.GetSourceDecoration());
        items.Insert(index, decoration);
    }

    internal int Remove(PdfEditablePageDecoration decoration)
    {
        ArgumentNullException.ThrowIfNull(decoration);
        EnsureSynchronized();
        var index = items.IndexOf(decoration);

        if (index < 0)
        {
            throw new InvalidOperationException("The page decoration is not attached to this document.");
        }

        _ = decorations.Remove(decoration.GetSourceDecoration());
        items.RemoveAt(index);
        return index;
    }

    private void EnsureSynchronized()
    {
        if (items.Count != decorations.Items.Count)
        {
            throw new InvalidOperationException(
                "The authoritative page-decoration collection changed outside the editing session.");
        }

        for (var index = 0; index < items.Count; index++)
        {
            if (!ReferenceEquals(items[index].GetSourceDecoration(), decorations.Items[index]))
            {
                throw new InvalidOperationException(
                    "The authoritative page-decoration collection changed outside the editing session.");
            }
        }
    }
}
