namespace BlackFlag.Engine.Document.Decorations;

/// <summary>
/// Describes the zero-based inclusive page range and parity targeted by a page decoration.
/// </summary>
public readonly record struct PdfPageDecorationTarget
{
    public PdfPageDecorationTarget(
        int firstPageIndex,
        int? lastPageIndex = null,
        PdfPageSelectionParity parity = PdfPageSelectionParity.All)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(firstPageIndex);

        if (lastPageIndex is < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(lastPageIndex));
        }

        if (lastPageIndex.HasValue && lastPageIndex.Value < firstPageIndex)
        {
            throw new ArgumentException(
                "The last page index cannot precede the first page index.",
                nameof(lastPageIndex));
        }

        if (!Enum.IsDefined(parity))
        {
            throw new ArgumentOutOfRangeException(nameof(parity));
        }

        FirstPageIndex = firstPageIndex;
        LastPageIndex = lastPageIndex;
        Parity = parity;
    }

    /// <summary>Gets a target that begins at the first page and continues through the document.</summary>
    public static PdfPageDecorationTarget AllPages => new(0);

    /// <summary>Gets the first targeted zero-based page index.</summary>
    public int FirstPageIndex { get; }

    /// <summary>Gets the optional final targeted zero-based page index.</summary>
    public int? LastPageIndex { get; }

    /// <summary>Gets the physical-page parity filter.</summary>
    public PdfPageSelectionParity Parity { get; }

    /// <summary>Determines whether a zero-based physical page index is selected.</summary>
    public bool Includes(int pageIndex)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(pageIndex);

        if (pageIndex < FirstPageIndex ||
            (LastPageIndex.HasValue && pageIndex > LastPageIndex.Value))
        {
            return false;
        }

        var oneBasedPageNumber = pageIndex + 1;
        return Parity switch
        {
            PdfPageSelectionParity.All => true,
            PdfPageSelectionParity.Odd => oneBasedPageNumber % 2 != 0,
            PdfPageSelectionParity.Even => oneBasedPageNumber % 2 == 0,
            _ => throw new InvalidOperationException("Unsupported page-selection parity.")
        };
    }
}
