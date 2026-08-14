using System.Collections;

using BlackFlag.Engine.Document.Pages;
using BlackFlag.Engine.Editing.Ownership;

namespace BlackFlag.Engine.Editing.Pages;

/// <summary>
/// Exposes stable editable handles for the pages present when an editing
/// session opens.
/// </summary>
public sealed class PdfEditablePageCollection :
    IReadOnlyList<PdfEditablePage>
{
    private readonly List<PdfEditablePage> pages;

    internal PdfEditablePageCollection(
        PdfPageTree sourcePages,
        PdfEditingOwnershipToken owner)
    {
        ArgumentNullException.ThrowIfNull(sourcePages);

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
}
