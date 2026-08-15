using BlackFlag.Engine.Document.Forms;

namespace BlackFlag.Engine.Editing.Forms;

/// <summary>Captures immutable common AcroForm field state for validation and history.</summary>
public readonly record struct PdfFormFieldSnapshot(
    PdfFormFieldType FieldType,
    string? PartialName,
    string? Value,
    string? DefaultValue,
    PdfFormFieldOptions Options,
    Guid? ParentFieldId,
    int ChildCount,
    int WidgetCount);
