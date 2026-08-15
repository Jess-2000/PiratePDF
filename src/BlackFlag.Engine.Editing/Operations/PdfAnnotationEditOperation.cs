using BlackFlag.Engine.Document.Annotations;
using BlackFlag.Engine.Editing.Annotations;
using BlackFlag.Engine.Editing.Ownership;
using BlackFlag.Engine.Editing.Structural;

namespace BlackFlag.Engine.Editing.Operations;

/// <summary>Describes one controlled page-annotation edit.</summary>
public sealed class PdfAnnotationEditOperation : IPdfStructuralEditOperation
{
    internal PdfAnnotationEditOperation(
        PdfEditingOwnershipToken owner,
        PdfAnnotationMutationKind kind,
        Guid pageId,
        Guid annotationId,
        PdfAnnotationSubtype subtype,
        PdfStructuralValueState<PdfAnnotationSnapshot> previousState,
        PdfStructuralValueState<PdfAnnotationSnapshot> currentState,
        int? index = null)
    {
        Owner = owner;
        Kind = kind;
        PageId = pageId;
        AnnotationId = annotationId;
        Subtype = subtype;
        PreviousState = previousState;
        CurrentState = currentState;
        Index = index;
    }

    public PdfEditingOwnershipToken Owner { get; }
    public PdfEditScope Scope => PdfEditScope.Page;
    public PdfStructureKind StructureKind => PdfStructureKind.Annotation;
    public PdfAnnotationMutationKind Kind { get; }
    public Guid PageId { get; }
    public Guid AnnotationId { get; }
    public PdfAnnotationSubtype Subtype { get; }
    public PdfStructuralValueState<PdfAnnotationSnapshot> PreviousState { get; }
    public PdfStructuralValueState<PdfAnnotationSnapshot> CurrentState { get; }
    public int? Index { get; }
}
