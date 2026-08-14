namespace BlackFlag.Engine.SDK.Editing;

/// <summary>
/// Describes one logical mutation accepted by the editing subsystem.
/// </summary>
public sealed record PdfEditingSdkOperation(
    string Scope,
    string Kind,
    Guid? PageId = null,
    Guid? RelatedPageId = null);
