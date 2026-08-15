namespace BlackFlag.Engine.Editing.Outlines;

/// <summary>Identifies a controlled document-outline mutation.</summary>
public enum PdfOutlineMutationKind
{
    AddRoot = 0,
    RemoveRoot = 1,
    AddChild = 2,
    RemoveChild = 3,
    SetTitle = 4,
    SetOpenState = 5,
    SetDestination = 6,
    SetAction = 7,
    ClearTarget = 8
}
