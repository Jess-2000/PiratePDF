namespace BlackFlag.Engine.Editing.Annotations;

/// <summary>Identifies a controlled annotation mutation.</summary>
public enum PdfAnnotationMutationKind
{
    Add = 0,
    Remove = 1,
    SetRectangle = 2,
    SetContents = 3,
    ClearContents = 4,
    SetFlags = 5
}
