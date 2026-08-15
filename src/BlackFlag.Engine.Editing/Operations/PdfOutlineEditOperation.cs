using BlackFlag.Engine.Editing.Outlines;
using BlackFlag.Engine.Editing.Ownership;
using BlackFlag.Engine.Editing.Structural;

namespace BlackFlag.Engine.Editing.Operations;

/// <summary>Describes one controlled document-outline edit.</summary>
public sealed class PdfOutlineEditOperation : IPdfStructuralEditOperation
{
    internal PdfOutlineEditOperation(
        PdfEditingOwnershipToken owner,
        PdfOutlineMutationKind kind,
        Guid itemId,
        PdfStructuralValueState<PdfOutlineItemSnapshot> previousState,
        PdfStructuralValueState<PdfOutlineItemSnapshot> currentState,
        Guid? parentItemId = null,
        int? index = null)
    {
        Owner = owner;
        Kind = kind;
        ItemId = itemId;
        PreviousState = previousState;
        CurrentState = currentState;
        ParentItemId = parentItemId;
        Index = index;
    }

    public PdfEditingOwnershipToken Owner { get; }
    public PdfEditScope Scope => PdfEditScope.Document;
    public PdfStructureKind StructureKind => PdfStructureKind.Outline;
    public PdfOutlineMutationKind Kind { get; }
    public Guid ItemId { get; }
    public PdfStructuralValueState<PdfOutlineItemSnapshot> PreviousState { get; }
    public PdfStructuralValueState<PdfOutlineItemSnapshot> CurrentState { get; }
    public Guid? ParentItemId { get; }
    public int? Index { get; }
}
