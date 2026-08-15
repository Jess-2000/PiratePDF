namespace BlackFlag.Engine.Document.Decorations;

/// <summary>Represents one authoritative reusable logical page decoration.</summary>
public sealed class PdfPageDecoration
{
    public PdfPageDecoration(
        PdfPageDecorationKind kind,
        PdfPageDecorationRegion region,
        PdfPageDecorationAlignment alignment,
        PdfPageDecorationTarget target)
    {
        if (!Enum.IsDefined(kind))
        {
            throw new ArgumentOutOfRangeException(nameof(kind));
        }

        if (!Enum.IsDefined(region))
        {
            throw new ArgumentOutOfRangeException(nameof(region));
        }

        if (!Enum.IsDefined(alignment))
        {
            throw new ArgumentOutOfRangeException(nameof(alignment));
        }

        Id = Guid.NewGuid();
        Kind = kind;
        Region = region;
        Alignment = alignment;
        Target = target;
        Numbering = kind == PdfPageDecorationKind.PageNumber
            ? new PdfPageNumberingOptions(1)
            : null;
    }

    /// <summary>Gets the stable logical identity of this decoration.</summary>
    public Guid Id { get; }

    /// <summary>Gets the immutable logical decoration kind.</summary>
    public PdfPageDecorationKind Kind { get; }

    /// <summary>Gets the header/footer region.</summary>
    public PdfPageDecorationRegion Region { get; internal set; }

    /// <summary>Gets the horizontal placement within the region.</summary>
    public PdfPageDecorationAlignment Alignment { get; internal set; }

    /// <summary>Gets the page-selection rule.</summary>
    public PdfPageDecorationTarget Target { get; internal set; }

    /// <summary>Gets literal text for a text decoration, or null when absent/not applicable.</summary>
    public string? Text { get; internal set; }

    /// <summary>Gets numbering semantics for a page-number decoration.</summary>
    public PdfPageNumberingOptions? Numbering { get; internal set; }

    /// <summary>Gets the displayed integer assigned to a targeted physical page.</summary>
    public int GetDisplayedPageNumber(int pageIndex)
    {
        if (Kind != PdfPageDecorationKind.PageNumber || Numbering is null)
        {
            throw new InvalidOperationException(
                "Displayed page numbers are only available for page-number decorations.");
        }

        if (!Target.Includes(pageIndex))
        {
            throw new ArgumentOutOfRangeException(
                nameof(pageIndex),
                pageIndex,
                "The supplied page is not selected by this decoration.");
        }

        return checked(
            Numbering.FirstDisplayedNumber +
            pageIndex -
            Target.FirstPageIndex);
    }
}
