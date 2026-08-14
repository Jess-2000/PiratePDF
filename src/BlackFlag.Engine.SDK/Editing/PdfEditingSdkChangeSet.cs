namespace BlackFlag.Engine.SDK.Editing;

/// <summary>
/// Describes the logical operations committed by one SDK edit transaction.
/// </summary>
public sealed record PdfEditingSdkChangeSet(
    Guid TransactionId,
    IReadOnlyList<PdfEditingSdkOperation> Operations);
