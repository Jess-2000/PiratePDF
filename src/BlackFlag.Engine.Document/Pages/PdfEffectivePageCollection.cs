using System.Collections;
using System.Collections.ObjectModel;

namespace BlackFlag.Engine.Document.Pages;

/// <summary>Provides immutable ordered access to loaded effective pages.</summary>
public sealed class PdfEffectivePageCollection : IReadOnlyList<PdfEffectivePage>
{
    private readonly ReadOnlyCollection<PdfEffectivePage> pages;

    public PdfEffectivePageCollection(IEnumerable<PdfEffectivePage> pages)
    {
        ArgumentNullException.ThrowIfNull(pages);
        PdfEffectivePage[] copy = pages.ToArray();
        if (copy.Any(static page => page is null))
        {
            throw new ArgumentException(
                "A page collection cannot contain a null page.",
                nameof(pages));
        }

        this.pages = Array.AsReadOnly(copy);
    }

    public int Count => pages.Count;
    public PdfEffectivePage this[int index] => pages[index];
    public IEnumerator<PdfEffectivePage> GetEnumerator() => pages.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
