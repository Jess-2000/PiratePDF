using BlackFlag.Engine.Document.Outlines;

namespace BlackFlag.Engine.Editing.Outlines;

/// <summary>Captures immutable common outline-item state for validation and history.</summary>
public readonly record struct PdfOutlineItemSnapshot(
    string Title,
    bool IsOpen,
    PdfOutlineDestination? Destination,
    PdfOutlineNavigationAction? Action,
    Guid? ParentItemId,
    int ChildCount);
