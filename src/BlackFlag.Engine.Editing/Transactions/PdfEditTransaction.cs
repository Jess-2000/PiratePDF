using BlackFlag.Engine.Editing.Documents;

namespace BlackFlag.Engine.Editing.Transactions;

/// <summary>
/// Owns one atomic editing scope for a PDF editing session.
/// </summary>
public sealed class PdfEditTransaction :
    IDisposable
{
    private readonly List<PdfEditChange> changes =
        new();

    private readonly PdfEditingSession session;

    internal PdfEditTransaction(
        PdfEditingSession session)
    {
        ArgumentNullException.ThrowIfNull(session);

        this.session = session;
        Id = Guid.NewGuid();
        State = PdfEditTransactionState.Active;
    }

    /// <summary>
    /// Gets the stable identity of this transaction.
    /// </summary>
    public Guid Id { get; }

    /// <summary>
    /// Gets the current transaction lifecycle state.
    /// </summary>
    public PdfEditTransactionState State { get; private set; }

    /// <summary>
    /// Gets the number of mutations currently recorded by the transaction.
    /// </summary>
    public int ChangeCount =>
        changes.Count;

    /// <summary>
    /// Commits the current edits and returns their ordered logical change set.
    /// </summary>
    public PdfEditChangeSet Commit()
    {
        EnsureActive();

        var changeSet =
            new PdfEditChangeSet(
                Id,
                changes.ToArray());

        State = PdfEditTransactionState.Committed;
        session.CompleteTransaction(this);
        session.RecordCommittedChangeSet(changeSet);

        return changeSet;
    }

    /// <summary>
    /// Reverses all recorded edits in reverse order and closes the transaction.
    /// </summary>
    public void Rollback()
    {
        EnsureActive();

        session.ReplayChanges(
            changes,
            undo: true);

        State = PdfEditTransactionState.RolledBack;
        session.CompleteTransaction(this);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (State == PdfEditTransactionState.Active)
        {
            Rollback();
        }
    }

    internal void Record(
        PdfEditChange change)
    {
        ArgumentNullException.ThrowIfNull(change);
        EnsureActive();
        changes.Add(change);
    }

    private void EnsureActive()
    {
        session.EnsureActive();

        if (State != PdfEditTransactionState.Active)
        {
            throw new InvalidOperationException(
                "The PDF edit transaction is no longer active.");
        }
    }
}
