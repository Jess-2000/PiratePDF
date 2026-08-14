using BlackFlag.Engine.Document.Annotations;

namespace BlackFlag.Engine.Editing.Annotations;

/// <summary>Captures immutable common annotation state for history and validation.</summary>
public readonly record struct PdfAnnotationSnapshot(
    PdfAnnotationSubtype Subtype,
    PdfAnnotationRectangle Rectangle,
    string? Contents,
    PdfAnnotationOptions Flags);
