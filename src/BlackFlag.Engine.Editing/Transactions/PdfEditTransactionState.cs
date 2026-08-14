namespace BlackFlag.Engine.Editing.Transactions;

/// <summary>
/// Identifies the lifecycle state of a PDF edit transaction.
/// </summary>
public enum PdfEditTransactionState
{
    Active = 0,
    Committed = 1,
    RolledBack = 2
}
