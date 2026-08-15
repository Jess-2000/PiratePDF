using BlackFlag.Engine.Document.Decorations;

namespace BlackFlag.Engine.Editing.Decorations;

/// <summary>Captures immutable logical page-decoration state for validation and history.</summary>
public readonly record struct PdfPageDecorationSnapshot(
    PdfPageDecorationKind Kind,
    PdfPageDecorationRegion Region,
    PdfPageDecorationAlignment Alignment,
    PdfPageDecorationTarget Target,
    string? Text,
    PdfPageNumberingOptions? Numbering);
