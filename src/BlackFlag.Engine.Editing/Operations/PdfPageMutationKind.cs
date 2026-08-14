namespace BlackFlag.Engine.Editing.Operations;

/// <summary>
/// Identifies a concrete page mutation supported by the editing subsystem.
/// </summary>
public enum PdfPageMutationKind
{
    Insert = 0,
    Remove = 1,
    Move = 2,
    Replace = 3,
    SetRotation = 4
}
