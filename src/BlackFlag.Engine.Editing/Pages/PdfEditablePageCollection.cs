using System.Collections;

using BlackFlag.Engine.Document.Pages;
using BlackFlag.Engine.Editing.Ownership;

namespace BlackFlag.Engine.Editing.Pages;

/// <summary>
/// Exposes stable editable handles for pages owned by an editing session.
/// </summary>
public sealed class PdfEditablePageCollection :
    IReadOnlyList<PdfEditablePage>
{
    private readonly List<PdfEditablePage> pages;
    private readonly PdfPageTree sourcePages;

    internal PdfEditablePageCollection(
        PdfPageTree sourcePages,
        PdfEditingOwnershipToken owner)
    {
        ArgumentNullException.ThrowIfNull(sourcePages);

        this.sourcePages = sourcePages;
        pages = new List<PdfEditablePage>(sourcePages.Count);

        for (var index = 0; index < sourcePages.Count; index++)
        {
            pages.Add(
                new PdfEditablePage(
                    sourcePages[index],
                    owner,
                    PdfEditablePageOrigin.SourceDocument,
                    index));
        }
    }

    /// <inheritdoc />
    public int Count =>
        pages.Count;

    /// <inheritdoc />
    public PdfEditablePage this[int index] =>
        pages[index];

    /// <summary>
    /// Returns the current editing-collection index of the supplied page.
    /// </summary>
    public int IndexOf(PdfEditablePage page)
    {
        ArgumentNullException.ThrowIfNull(page);
        return pages.IndexOf(page);
    }

    /// <summary>
    /// Determines whether the collection contains the supplied editable-page
    /// handle.
    /// </summary>
    public bool Contains(PdfEditablePage page)
    {
        ArgumentNullException.ThrowIfNull(page);
        return pages.Contains(page);
    }

    /// <inheritdoc />
    public IEnumerator<PdfEditablePage> GetEnumerator() =>
        pages.GetEnumerator();

    /// <inheritdoc />
    IEnumerator IEnumerable.GetEnumerator() =>
        GetEnumerator();

    internal void Insert(
        int index,
        PdfEditablePage page)
    {
        ArgumentNullException.ThrowIfNull(page);
        EnsureSynchronized();

        if (index < 0 || index > pages.Count)
        {
            throw new ArgumentOutOfRangeException(
                nameof(index));
        }

        if (pages.Contains(page))
        {
            throw new InvalidOperationException(
                "The editable page is already attached to this document.");
        }

        sourcePages.Insert(
            index,
            page.GetSourcePage());

        pages.Insert(
            index,
            page);
    }

    internal int Remove(
        PdfEditablePage page)
    {
        ArgumentNullException.ThrowIfNull(page);
        EnsureSynchronized();

        var index = pages.IndexOf(page);

        if (index < 0)
        {
            throw new InvalidOperationException(
                "The editable page is not attached to this document.");
        }

        var removedPage = sourcePages.RemoveAt(index);

        if (!ReferenceEquals(
                removedPage,
                page.GetSourcePage()))
        {
            throw new InvalidOperationException(
                "The authoritative page tree changed outside the editing session.");
        }

        pages.RemoveAt(index);

        return index;
    }

    internal int Move(
        PdfEditablePage page,
        int destinationIndex)
    {
        ArgumentNullException.ThrowIfNull(page);
        EnsureSynchronized();

        var sourceIndex = pages.IndexOf(page);

        if (sourceIndex < 0)
        {
            throw new InvalidOperationException(
                "The editable page is not attached to this document.");
        }

        if (
            destinationIndex < 0 ||
            destinationIndex >= pages.Count)
        {
            throw new ArgumentOutOfRangeException(
                nameof(destinationIndex));
        }

        sourcePages.Move(
            sourceIndex,
            destinationIndex);

        if (sourceIndex != destinationIndex)
        {
            pages.RemoveAt(sourceIndex);
            pages.Insert(
                destinationIndex,
                page);
        }

        return sourceIndex;
    }

    internal int Replace(
        PdfEditablePage target,
        PdfEditablePage replacement)
    {
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(replacement);
        EnsureSynchronized();

        var index = pages.IndexOf(target);

        if (index < 0)
        {
            throw new InvalidOperationException(
                "The target editable page is not attached to this document.");
        }

        if (pages.Contains(replacement))
        {
            throw new InvalidOperationException(
                "The replacement editable page is already attached to this document.");
        }

        var removedPage = sourcePages.RemoveAt(index);

        try
        {
            sourcePages.Insert(
                index,
                replacement.GetSourcePage());
        }
        catch
        {
            sourcePages.Insert(
                index,
                removedPage);

            throw;
        }

        pages[index] = replacement;

        return index;
    }

    internal void EnsureSynchronized()
    {
        if (sourcePages.Count != pages.Count)
        {
            throw new InvalidOperationException(
                "The authoritative page tree changed outside the editing session.");
        }

        for (var index = 0; index < pages.Count; index++)
        {
            if (!ReferenceEquals(
                    sourcePages[index],
                    pages[index].GetSourcePage()))
            {
                throw new InvalidOperationException(
                    "The authoritative page tree changed outside the editing session.");
            }
        }
    }
}
