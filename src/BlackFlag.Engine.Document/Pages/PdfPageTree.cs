using System.Collections;

using BlackFlag.Engine.Document.Model;
using BlackFlag.Engine.Document.Objects;
using BlackFlag.Engine.Document.Objects.Primitives;

namespace BlackFlag.Engine.Document.Pages;

/// <summary>
/// Owns the ordered pages of a PDF document.
/// </summary>
public sealed class PdfPageTree :
    IReadOnlyList<PdfPage>,
    IPdfDictionaryBackedModel
{
    private readonly List<PdfPage> pages =
        new();

    private readonly PdfObjectBinding binding =
        new();

    public int Count =>
        pages.Count;

    public PdfPage this[int index] =>
        pages[index];

    internal PdfObjectIdentifier? ObjectIdentifier =>
        binding.Identifier;

    public void Add(PdfPage page)
    {
        ArgumentNullException.ThrowIfNull(page);

        EnsurePageCanBeAdded(page);

        pages.Add(page);
        page.AttachToParent(this);
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

        EnsurePageCanBeAdded(page);

        pages.Insert(
            index,
            page);

        page.AttachToParent(this);
    }

    public bool Remove(PdfPage page)
    {
        ArgumentNullException.ThrowIfNull(page);

        if (!pages.Remove(page))
        {
            return false;
        }

        page.DetachFromParent(this);

        return true;
    }

    public PdfPage RemoveAt(int index)
    {
        var page = pages[index];

        pages.RemoveAt(index);
        page.DetachFromParent(this);

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

    internal void BindObjectIdentifier(
        PdfObjectIdentifier identifier)
    {
        binding.Bind(identifier);
    }

    internal PdfObjectIdentifier GetRequiredObjectIdentifier()
    {
        return binding.GetRequiredIdentifier(
            nameof(PdfPageTree));
    }

    PdfDictionary
        IPdfDictionaryBackedModel.CreateDictionarySnapshot()
    {
        return CreateDictionarySnapshot();
    }

    internal PdfDictionary CreateDictionarySnapshot()
    {
        var dictionary = new PdfDictionary();

        dictionary.Add(
            PdfNames.Type,
            PdfNames.Pages);

        dictionary.Add(
            PdfNames.Count,
            new PdfInteger(Count));

        dictionary.Add(
            PdfNames.Kids,
            CreateKidsArray());

        return dictionary;
    }

    public IEnumerator<PdfPage> GetEnumerator()
    {
        return pages.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    private PdfArray CreateKidsArray()
    {
        var references = new PdfArray();

        foreach (var page in pages)
        {
            var identifier =
                page.ObjectIdentifier ??
                throw new InvalidOperationException(
                    $"Page '{page.Id}' has not been assigned an " +
                    "indirect-object identifier.");

            references.Add(
                new PdfIndirectReference(identifier));
        }

        return references;
    }

    private void EnsurePageCanBeAdded(
        PdfPage page)
    {
        if (pages.Contains(page))
        {
            throw new InvalidOperationException(
                "The page already belongs to this page tree.");
        }

        if (
            page.Parent is not null &&
            !ReferenceEquals(page.Parent, this))
        {
            throw new InvalidOperationException(
                "The page already belongs to another page tree.");
        }
    }
}
