using BlackFlag.Engine.Document.Forms;
using BlackFlag.Engine.Editing.Forms;
using BlackFlag.Engine.Editing.Ownership;
using BlackFlag.Engine.Editing.Structural;

namespace BlackFlag.Engine.Editing.Operations;

/// <summary>Describes one controlled AcroForm field edit.</summary>
public sealed class PdfFormFieldEditOperation : IPdfStructuralEditOperation
{
    internal PdfFormFieldEditOperation(
        PdfEditingOwnershipToken owner,
        PdfFormFieldMutationKind kind,
        Guid fieldId,
        PdfFormFieldType fieldType,
        PdfStructuralValueState<PdfFormFieldSnapshot> previousState,
        PdfStructuralValueState<PdfFormFieldSnapshot> currentState,
        Guid? parentFieldId = null,
        Guid? widgetAnnotationId = null,
        int? index = null)
    {
        Owner = owner;
        Kind = kind;
        FieldId = fieldId;
        FieldType = fieldType;
        PreviousState = previousState;
        CurrentState = currentState;
        ParentFieldId = parentFieldId;
        WidgetAnnotationId = widgetAnnotationId;
        Index = index;
    }

    public PdfEditingOwnershipToken Owner { get; }
    public PdfEditScope Scope => PdfEditScope.Document;
    public PdfStructureKind StructureKind => PdfStructureKind.Form;
    public PdfFormFieldMutationKind Kind { get; }
    public Guid FieldId { get; }
    public PdfFormFieldType FieldType { get; }
    public PdfStructuralValueState<PdfFormFieldSnapshot> PreviousState { get; }
    public PdfStructuralValueState<PdfFormFieldSnapshot> CurrentState { get; }
    public Guid? ParentFieldId { get; }
    public Guid? WidgetAnnotationId { get; }
    public int? Index { get; }
}
