using BlackFlag.Engine.Editing.Decorations;
using BlackFlag.Engine.Editing.Ownership;
using BlackFlag.Engine.Editing.Structural;

namespace BlackFlag.Engine.Editing.Operations;

/// <summary>Describes one controlled logical page-decoration edit.</summary>
public sealed class PdfPageDecorationEditOperation : IPdfStructuralEditOperation
{
    internal PdfPageDecorationEditOperation(
        PdfEditingOwnershipToken owner,
        PdfPageDecorationMutationKind kind,
        Guid decorationId,
        PdfStructuralValueState<PdfPageDecorationSnapshot> previousState,
        PdfStructuralValueState<PdfPageDecorationSnapshot> currentState,
        int? index = null)
    {
        Owner = owner;
        Kind = kind;
        DecorationId = decorationId;
        PreviousState = previousState;
        CurrentState = currentState;
        Index = index;
    }

    public PdfEditingOwnershipToken Owner { get; }
    public PdfEditScope Scope => PdfEditScope.Document;
    public PdfStructureKind StructureKind => PdfStructureKind.PageDecoration;
    public PdfPageDecorationMutationKind Kind { get; }
    public Guid DecorationId { get; }
    public PdfStructuralValueState<PdfPageDecorationSnapshot> PreviousState { get; }
    public PdfStructuralValueState<PdfPageDecorationSnapshot> CurrentState { get; }
    public int? Index { get; }
}
