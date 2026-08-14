namespace BlackFlag.Engine.Editing.Ownership;

/// <summary>
/// Identifies the editing session that owns an editing operation or mutable
/// editing-state object.
/// </summary>
public readonly record struct PdfEditingOwnershipToken(Guid SessionId);
