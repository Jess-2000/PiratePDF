namespace BlackFlag.Engine.Editing.Decorations;

/// <summary>Identifies a controlled logical page-decoration mutation.</summary>
public enum PdfPageDecorationMutationKind
{
    Add = 0,
    Remove = 1,
    SetText = 2,
    ClearText = 3,
    SetPlacement = 4,
    SetTarget = 5,
    SetNumbering = 6
}
