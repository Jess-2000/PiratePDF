using System.Collections;

namespace BlackFlag.Engine.Document.Pages;

/// <summary>
/// Owns the ordered pages of a PDF document.
/// </summary>
public sealed class PdfPageTree :
    IReadOnlyList<PdfPage>
{
    private readonly List<PdfPage> pages =
        new();

    public int Count =>
        pages.Count;

    public PdfPage this[int index] =>
        pages[index];

    public void Add(PdfPage page)
    {
        ArgumentNullException.ThrowIfNull(page);

        EnsurePageIsNotPresent(page);

        pages.Add(page);
    }

    public void Insert(
        int index,
        PdfPage page)
    {
        ArgumentNullException.ThrowIfNull(page);

        if (index < 0 || index > pages.Count)
        {
            throw new ArgumentOutOfRangeException(
                nameof(index));
        }

        EnsurePageIsNotPresent(page);

        pages.Insert(
            index,
            page);
    }

    public bool Remove(PdfPage page)
    {
        ArgumentNullException.ThrowIfNull(page);

        return pages.Remove(page);
    }

    public PdfPage RemoveAt(int index)
    {
        var page = pages[index];

        pages.RemoveAt(index);

        return page;
    }

    public void Move(
        int sourceIndex,
        int destinationIndex)
    {
        if (
            sourceIndex < 0 ||
            sourceIndex >= pages.Count)
        {
            throw new ArgumentOutOfRangeException(
                nameof(sourceIndex));
        }

        if (
            destinationIndex < 0 ||
            destinationIndex >= pages.Count)
        {
            throw new ArgumentOutOfRangeException(
                nameof(destinationIndex));
        }

        if (sourceIndex == destinationIndex)
        {
            return;
        }

        var page = pages[sourceIndex];

        pages.RemoveAt(sourceIndex);
        pages.Insert(destinationIndex, page);
    }

    public int IndexOf(PdfPage page)
    {
        ArgumentNullException.ThrowIfNull(page);

        return pages.IndexOf(page);
    }

    public IEnumerator<PdfPage> GetEnumerator()
    {
        return pages.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    private void EnsurePageIsNotPresent(
        PdfPage page)
    {
        if (pages.Contains(page))
        {
            throw new InvalidOperationException(
                "The page already belongs to this page tree.");
        }
    }
}