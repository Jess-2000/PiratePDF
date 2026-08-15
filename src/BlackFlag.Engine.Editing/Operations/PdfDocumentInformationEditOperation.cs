using BlackFlag.Engine.Document.Metadata;
using BlackFlag.Engine.Editing.Metadata;
using BlackFlag.Engine.Editing.Ownership;
using BlackFlag.Engine.Editing.Structural;

namespace BlackFlag.Engine.Editing.Operations;

/// <summary>Describes one controlled document-information metadata edit.</summary>
public sealed class PdfDocumentInformationEditOperation :
    IPdfStructuralEditOperation
{
    internal PdfDocumentInformationEditOperation(
        PdfEditingOwnershipToken owner,
        PdfDocumentInformationMutationKind kind,
        PdfDocumentInformationField field,
        PdfStructuralValueState<string> previousState,
        PdfStructuralValueState<string> currentState)
    {
        Owner = owner;
        Kind = kind;
        Field = field;
        PreviousState = previousState;
        CurrentState = currentState;
    }

    public PdfEditingOwnershipToken Owner { get; }

    public PdfEditScope Scope =>
        PdfEditScope.Document;

    public PdfStructureKind StructureKind =>
        PdfStructureKind.Metadata;

    public PdfDocumentInformationMutationKind Kind { get; }

    public PdfDocumentInformationField Field { get; }

    public PdfStructuralValueState<string> PreviousState { get; }

    public PdfStructuralValueState<string> CurrentState { get; }
}
