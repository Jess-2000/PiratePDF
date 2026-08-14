namespace BlackFlag.Engine.SDK.Editing;

/// <summary>
/// Defines preservation behavior requested for an SDK editing session.
/// </summary>
public enum PdfEditingSdkPreservationMode
{
    /// <summary>
    /// Preserves untargeted logical structure and existing object identity
    /// wherever the requested edit does not require a change.
    /// </summary>
    PreserveUntargetedStructure = 0
}
