using System.Collections;

using BlackFlag.Engine.Editing.Operations;

namespace BlackFlag.Engine.Editing.Transactions;

/// <summary>
/// Represents the ordered logical changes committed by one edit transaction.
/// </summary>
public sealed class PdfEditChangeSet :
    IReadOnlyList<IPdfEditOperation>
{
    private readonly IReadOnlyList<PdfEditChange> changes;

    internal PdfEditChangeSet(
        Guid transactionId,
        IReadOnlyList<PdfEditChange> changes)
    {
        ArgumentNullException.ThrowIfNull(changes);

        TransactionId = transactionId;
        this.changes = changes;
    }

    /// <summary>
    /// Gets the identity of the transaction that produced this change set.
    /// </summary>
    public Guid TransactionId { get; }

    /// <inheritdoc />
    public int Count =>
        changes.Count;

    /// <summary>
    /// Gets whether the committed transaction contained no mutations.
    /// </summary>
    public bool IsEmpty =>
        changes.Count == 0;

    /// <inheritdoc />
    public IPdfEditOperation this[int index] =>
        changes[index].Operation;

    /// <inheritdoc />
    public IEnumerator<IPdfEditOperation> GetEnumerator()
    {
        for (var index = 0; index < changes.Count; index++)
        {
            yield return changes[index].Operation;
        }
    }

    /// <inheritdoc />
    IEnumerator IEnumerable.GetEnumerator() =>
        GetEnumerator();

    internal IReadOnlyList<PdfEditChange> GetChanges() =>
        changes;
}
