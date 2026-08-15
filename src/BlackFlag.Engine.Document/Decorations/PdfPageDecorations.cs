namespace BlackFlag.Engine.Document.Decorations;

/// <summary>Owns the authoritative ordered page-decoration definitions for a document.</summary>
public sealed class PdfPageDecorations
{
    private readonly List<PdfPageDecoration> items = [];
    private readonly IReadOnlyList<PdfPageDecoration> readOnlyItems;

    public PdfPageDecorations()
    {
        readOnlyItems = items.AsReadOnly();
    }

    /// <summary>Gets the ordered logical decoration definitions.</summary>
    public IReadOnlyList<PdfPageDecoration> Items => readOnlyItems;

    internal void Insert(int index, PdfPageDecoration decoration)
    {
        ArgumentNullException.ThrowIfNull(decoration);

        if (index < 0 || index > items.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }

        if (items.Contains(decoration))
        {
            throw new InvalidOperationException("The page decoration is already attached to this document.");
        }

        items.Insert(index, decoration);
    }

    internal int Remove(PdfPageDecoration decoration)
    {
        ArgumentNullException.ThrowIfNull(decoration);
        var index = items.IndexOf(decoration);

        if (index < 0)
        {
            throw new InvalidOperationException("The page decoration is not attached to this document.");
        }

        items.RemoveAt(index);
        return index;
    }
}
