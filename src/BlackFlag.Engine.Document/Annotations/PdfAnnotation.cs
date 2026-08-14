using BlackFlag.Engine.Document.Pages;

namespace BlackFlag.Engine.Document.Annotations;

/// <summary>Represents the authoritative logical state of one PDF annotation.</summary>
public sealed class PdfAnnotation
{
    private PdfPage? parent;

    public PdfAnnotation(
        PdfAnnotationSubtype subtype,
        PdfAnnotationRectangle rectangle)
    {
        if (!Enum.IsDefined(subtype))
        {
            throw new ArgumentOutOfRangeException(
                nameof(subtype),
                subtype,
                "The PDF annotation subtype is not supported.");
        }

        Id = Guid.NewGuid();
        Subtype = subtype;
        Rectangle = rectangle;
    }

    public Guid Id { get; }
    public PdfAnnotationSubtype Subtype { get; }
    public PdfAnnotationRectangle Rectangle { get; internal set; }
    public string? Contents { get; internal set; }
    public PdfAnnotationOptions Flags { get; internal set; }

    internal PdfPage? Parent => parent;

    internal void AttachToPage(PdfPage page)
    {
        ArgumentNullException.ThrowIfNull(page);

        if (parent is not null)
        {
            throw new InvalidOperationException(
                "The annotation is already attached to a page.");
        }

        parent = page;
    }

    internal void DetachFromPage(PdfPage page)
    {
        ArgumentNullException.ThrowIfNull(page);

        if (!ReferenceEquals(parent, page))
        {
            throw new InvalidOperationException(
                "The annotation is not attached to the supplied page.");
        }

        parent = null;
    }
}
