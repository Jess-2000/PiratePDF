namespace BlackFlag.Engine.Editing.Forms;

/// <summary>Identifies a controlled AcroForm field mutation.</summary>
public enum PdfFormFieldMutationKind
{
    AddRoot = 0,
    RemoveRoot = 1,
    AddChild = 2,
    RemoveChild = 3,
    SetPartialName = 4,
    ClearPartialName = 5,
    SetValue = 6,
    ClearValue = 7,
    SetDefaultValue = 8,
    ClearDefaultValue = 9,
    SetOptions = 10,
    AttachWidget = 11,
    DetachWidget = 12
}
