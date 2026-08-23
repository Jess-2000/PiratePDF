using BlackFlag.Engine.Document.Annotations;
using BlackFlag.Engine.Document.Objects;
using BlackFlag.Engine.Editing.Ownership;

namespace BlackFlag.Engine.Editing.Annotations;

/// <summary>Represents a stable editing-session handle for one annotation.</summary>
public sealed class PdfEditableAnnotation
{
    private readonly PdfAnnotation annotation;

    internal PdfEditableAnnotation(
        PdfAnnotation annotation,
        PdfEditingOwnershipToken owner)
    {
        ArgumentNullException.ThrowIfNull(annotation);
        this.annotation = annotation;
        Owner = owner;
    }

    public Guid Id => annotation.Id;
    public PdfAnnotationSubtype Subtype => annotation.Subtype;
    public PdfAnnotationRectangle Rectangle => annotation.Rectangle;
    public string? Contents => annotation.Contents;
    public PdfAnnotationOptions Flags => annotation.Flags;
    public Guid? PageId => annotation.Parent?.Id;
    public bool IsAttached => annotation.Parent is not null;

    internal PdfEditingOwnershipToken Owner { get; }

    internal PdfObjectIdentifier? ObjectIdentifier =>
        annotation.ObjectIdentifier;

    internal PdfAnnotation GetSourceAnnotation() => annotation;
}
