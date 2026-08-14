using BlackFlag.Engine.Document.Pages;
using BlackFlag.Engine.Editing.Geometry;
using BlackFlag.Engine.Editing.Ownership;
using BlackFlag.Engine.Editing.Structural;

namespace BlackFlag.Engine.Editing.Operations;

/// <summary>Describes one controlled page-geometry edit.</summary>
public sealed class PdfPageGeometryEditOperation : IPdfStructuralEditOperation
{
    internal PdfPageGeometryEditOperation(
        PdfEditingOwnershipToken owner,
        PdfPageGeometryMutationKind kind,
        Guid pageId,
        PdfPageBoxKind boxKind,
        PdfStructuralValueState<PdfPageBox> previousState,
        PdfStructuralValueState<PdfPageBox> currentState,
        PdfPageResizeAnchor? resizeAnchor = null)
    {
        Owner = owner;
        Kind = kind;
        PageId = pageId;
        BoxKind = boxKind;
        PreviousState = previousState;
        CurrentState = currentState;
        ResizeAnchor = resizeAnchor;
    }

    public PdfEditingOwnershipToken Owner { get; }
    public PdfEditScope Scope => PdfEditScope.Page;
    public PdfStructureKind StructureKind => PdfStructureKind.PageGeometry;
    public PdfPageGeometryMutationKind Kind { get; }
    public Guid PageId { get; }
    public PdfPageBoxKind BoxKind { get; }
    public PdfStructuralValueState<PdfPageBox> PreviousState { get; }
    public PdfStructuralValueState<PdfPageBox> CurrentState { get; }
    public PdfPageResizeAnchor? ResizeAnchor { get; }
}
