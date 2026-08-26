using BlackFlag.Engine.Document.Objects;
using BlackFlag.Engine.Document.Pages;
using BlackFlag.Engine.Editing.Ownership;

namespace BlackFlag.Engine.Editing.Operations;

/// <summary>
/// Describes one controlled page mutation performed by an editing session.
/// </summary>
public sealed class PdfPageEditOperation :
    IPdfEditOperation
{
    internal PdfPageEditOperation(
        PdfEditingOwnershipToken owner,
        PdfPageMutationKind kind,
        Guid pageId,
        Guid? relatedPageId = null,
        int? sourceIndex = null,
        int? destinationIndex = null,
        PdfPageRotation? rotation = null,
        PdfObjectIdentifier? objectIdentifier = null,
        PdfObjectIdentifier? relatedObjectIdentifier = null)
    {
        Owner = owner;
        Kind = kind;
        PageId = pageId;
        RelatedPageId = relatedPageId;
        SourceIndex = sourceIndex;
        DestinationIndex = destinationIndex;
        Rotation = rotation;
        ObjectIdentifier = objectIdentifier;
        RelatedObjectIdentifier = relatedObjectIdentifier;
    }

    /// <inheritdoc />
    public PdfEditingOwnershipToken Owner { get; }

    /// <inheritdoc />
    public PdfEditScope Scope =>
        PdfEditScope.Page;

    /// <summary>
    /// Gets the concrete page mutation represented by this operation.
    /// </summary>
    public PdfPageMutationKind Kind { get; }

    /// <summary>
    /// Gets the primary editable-page identity targeted by the operation.
    /// </summary>
    public Guid PageId { get; }

    /// <summary>
    /// Gets the related page identity for replacement operations.
    /// </summary>
    public Guid? RelatedPageId { get; }

    /// <summary>
    /// Gets the page index before the operation when applicable.
    /// </summary>
    public int? SourceIndex { get; }

    /// <summary>
    /// Gets the requested destination index when applicable.
    /// </summary>
    public int? DestinationIndex { get; }

    /// <summary>
    /// Gets the requested page rotation when applicable.
    /// </summary>
    public PdfPageRotation? Rotation { get; }

    internal PdfObjectIdentifier? ObjectIdentifier { get; }

    internal PdfObjectIdentifier? RelatedObjectIdentifier { get; }
}
