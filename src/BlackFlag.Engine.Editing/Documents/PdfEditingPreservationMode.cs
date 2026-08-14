namespace BlackFlag.Engine.Editing.Documents;

/// <summary>
/// Defines how an editing session treats document state that is not directly
/// targeted by an edit.
/// </summary>
public enum PdfEditingPreservationMode
{
    /// <summary>
    /// Preserves untargeted logical structure and existing object identity
    /// wherever the requested edit does not require a change.
    /// </summary>
    PreserveUntargetedStructure = 0
}
