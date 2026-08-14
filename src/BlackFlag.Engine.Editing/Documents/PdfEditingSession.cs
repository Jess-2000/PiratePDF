using System.Runtime.CompilerServices;

using BlackFlag.Engine.Document.Model;
using BlackFlag.Engine.Editing.Operations;
using BlackFlag.Engine.Editing.Ownership;
using BlackFlag.Engine.Editing.Transactions;

namespace BlackFlag.Engine.Editing.Documents;

/// <summary>
/// Owns controlled editing access to one PDF document.
/// </summary>
/// <remarks>
/// A document may have at most one active editing session. The session is the
/// ownership boundary for later mutation, transaction, undo, and redo work.
/// </remarks>
public sealed class PdfEditingSession :
    IDisposable
{
    private static readonly object OwnershipGate =
        new();

    private static readonly ConditionalWeakTable<PdfDocument, object>
        ActiveDocuments =
            new();

    private readonly PdfDocument document;
    private PdfEditTransaction? activeTransaction;
    private bool disposed;
    private bool replayingChanges;

    public PdfEditingSession(
        PdfDocument document,
        PdfEditingSessionOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(document);

        var resolvedOptions =
            options ?? new PdfEditingSessionOptions();

        ValidateOptions(resolvedOptions);

        lock (OwnershipGate)
        {
            if (ActiveDocuments.TryGetValue(
                    document,
                    out _))
            {
                throw new PdfEditingOwnershipException(
                    "The PDF document already has an active editing session.");
            }

            ActiveDocuments.Add(
                document,
                new object());
        }

        this.document = document;
        Options = resolvedOptions;
        Id = Guid.NewGuid();
        Document = new PdfEditableDocument(
            document,
            new PdfEditingOwnershipToken(Id),
            this);
    }

    /// <summary>
    /// Gets the stable identity of this editing session.
    /// </summary>
    public Guid Id { get; }

    /// <summary>
    /// Gets the options applied to this editing session.
    /// </summary>
    public PdfEditingSessionOptions Options { get; }

    /// <summary>
    /// Gets the controlled editable view owned by this session.
    /// </summary>
    public PdfEditableDocument Document { get; }

    /// <summary>
    /// Gets whether the editing session has been disposed.
    /// </summary>
    public bool IsDisposed =>
        disposed;

    /// <summary>
    /// Gets whether this editing session currently owns an active transaction.
    /// </summary>
    public bool HasActiveTransaction =>
        activeTransaction is not null;

    /// <summary>
    /// Begins a new transaction for mutations performed through this session.
    /// </summary>
    public PdfEditTransaction BeginTransaction()
    {
        EnsureActive();

        if (activeTransaction is not null)
        {
            throw new InvalidOperationException(
                "The editing session already has an active transaction.");
        }

        activeTransaction =
            new PdfEditTransaction(this);

        return activeTransaction;
    }

    /// <summary>
    /// Creates an ownership token for state created by this editing session.
    /// </summary>
    public PdfEditingOwnershipToken CreateOwnershipToken()
    {
        EnsureActive();

        return new PdfEditingOwnershipToken(Id);
    }

    /// <summary>
    /// Determines whether an ownership token belongs to this editing session.
    /// </summary>
    public bool Owns(PdfEditingOwnershipToken ownershipToken)
    {
        EnsureActive();

        return ownershipToken.SessionId == Id;
    }

    /// <summary>
    /// Verifies that an editing operation belongs to this editing session.
    /// </summary>
    public void EnsureOwnership(IPdfEditOperation operation)
    {
        ArgumentNullException.ThrowIfNull(operation);
        EnsureActive();

        if (!Owns(operation.Owner))
        {
            throw new PdfEditingOwnershipException(
                "The editing operation belongs to another editing session.");
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        if (activeTransaction is not null)
        {
            activeTransaction.Rollback();
        }

        lock (OwnershipGate)
        {
            ActiveDocuments.Remove(document);
        }

        disposed = true;
    }

    internal void RecordChange(
        IPdfEditOperation operation,
        Action undo,
        Action redo)
    {
        ArgumentNullException.ThrowIfNull(operation);
        ArgumentNullException.ThrowIfNull(undo);
        ArgumentNullException.ThrowIfNull(redo);
        EnsureActive();

        if (
            replayingChanges ||
            activeTransaction is null)
        {
            return;
        }

        activeTransaction.Record(
            new PdfEditChange(
                operation,
                undo,
                redo));
    }

    internal void CompleteTransaction(
        PdfEditTransaction transaction)
    {
        ArgumentNullException.ThrowIfNull(transaction);
        EnsureActive();

        if (!ReferenceEquals(
                activeTransaction,
                transaction))
        {
            throw new InvalidOperationException(
                "The transaction does not belong to the active editing scope.");
        }

        activeTransaction = null;
    }

    internal void ReplayChanges(
        IReadOnlyList<PdfEditChange> changes,
        bool undo)
    {
        ArgumentNullException.ThrowIfNull(changes);
        EnsureActive();

        if (replayingChanges)
        {
            throw new InvalidOperationException(
                "Editing changes are already being replayed.");
        }

        replayingChanges = true;

        try
        {
            if (undo)
            {
                for (var index = changes.Count - 1; index >= 0; index--)
                {
                    changes[index].Undo();
                }

                return;
            }

            for (var index = 0; index < changes.Count; index++)
            {
                changes[index].Redo();
            }
        }
        finally
        {
            replayingChanges = false;
        }
    }

    internal PdfDocument GetDocument()
    {
        EnsureActive();
        return document;
    }

    internal void EnsureActive()
    {
        ObjectDisposedException.ThrowIf(
            disposed,
            this);
    }

    private static void ValidateOptions(
        PdfEditingSessionOptions options)
    {
        if (!Enum.IsDefined(options.PreservationMode))
        {
            throw new ArgumentOutOfRangeException(
                nameof(options),
                options.PreservationMode,
                "The PDF editing preservation mode is not supported.");
        }
    }
}
