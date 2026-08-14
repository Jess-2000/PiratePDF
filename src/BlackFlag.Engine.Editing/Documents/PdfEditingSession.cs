using System.Runtime.CompilerServices;
using BlackFlag.Engine.Document.Model;
using BlackFlag.Engine.Editing.History;
using BlackFlag.Engine.Editing.Operations;
using BlackFlag.Engine.Editing.Ownership;
using BlackFlag.Engine.Editing.Preservation;
using BlackFlag.Engine.Editing.Transactions;
using BlackFlag.Engine.Editing.Validation;

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
            [];

    private readonly PdfDocument document;
    private readonly System.Collections.ObjectModel.ReadOnlyCollection<IPdfEditingValidator> validators;
    private readonly PdfEditingPreservationContract preservationContract;
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

        var validatorSnapshot =
            resolvedOptions.Validators.ToArray();

        validators =
            Array.AsReadOnly(
                validatorSnapshot);

        preservationContract =
            PdfEditingPreservationContract.Default;

        Options =
            new PdfEditingSessionOptions
            {
                PreservationMode =
                    resolvedOptions.PreservationMode,
                Validators =
                    validators
            };
        Id = Guid.NewGuid();
        Document = new PdfEditableDocument(
            document,
            new PdfEditingOwnershipToken(Id),
            this);
        History = new PdfEditHistory(this);
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
    /// Gets the preservation contract enforced by this editing session.
    /// </summary>
    public PdfEditingPreservationContract PreservationContract =>
        preservationContract;

    /// <summary>
    /// Gets the controlled editable view owned by this session.
    /// </summary>
    public PdfEditableDocument Document { get; }

    /// <summary>
    /// Gets the committed undo and redo history owned by this session.
    /// </summary>
    public PdfEditHistory History { get; }

    /// <summary>
    /// Gets the validators captured when this editing session was created.
    /// </summary>
    public IReadOnlyList<IPdfEditingValidator> Validators =>
        validators;

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

        activeTransaction?.Rollback();

        lock (OwnershipGate)
        {
            ActiveDocuments.Remove(document);
        }

        disposed = true;
    }

    internal void ValidateOperation(
        IPdfEditOperation operation)
    {
        ArgumentNullException.ThrowIfNull(operation);
        EnsureActive();
        EnsureOwnership(operation);

        for (var index = 0; index < validators.Count; index++)
        {
            validators[index].Validate(
                this,
                operation);
        }
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

        var appliedChanges =
            new List<PdfEditChange>(
                changes.Count);

        try
        {
            try
            {
                if (undo)
                {
                    for (var index = changes.Count - 1; index >= 0; index--)
                    {
                        changes[index].Undo();
                        appliedChanges.Add(
                            changes[index]);
                    }

                    return;
                }

                for (var index = 0; index < changes.Count; index++)
                {
                    changes[index].Redo();
                    appliedChanges.Add(
                        changes[index]);
                }
            }
            catch (Exception replayException)
            {
                try
                {
                    CompensateReplay(
                        appliedChanges,
                        undo);
                }
                catch (Exception compensationException)
                {
                    throw new AggregateException(
                        "Editing replay failed and its compensation also failed.",
                        replayException,
                        compensationException);
                }

                throw;
            }
        }
        finally
        {
            replayingChanges = false;
        }
    }

    private static void CompensateReplay(
        List<PdfEditChange> appliedChanges,
        bool undo)
    {
        if (undo)
        {
            for (var index = appliedChanges.Count - 1; index >= 0; index--)
            {
                appliedChanges[index].Redo();
            }

            return;
        }

        for (var index = appliedChanges.Count - 1; index >= 0; index--)
        {
            appliedChanges[index].Undo();
        }
    }

    internal void RecordCommittedChangeSet(
        PdfEditChangeSet changeSet)
    {
        ArgumentNullException.ThrowIfNull(changeSet);
        EnsureActive();
        History.RecordCommitted(changeSet);
    }

    internal void EnsureHistoryReplayAllowed()
    {
        EnsureActive();

        if (activeTransaction is not null)
        {
            throw new InvalidOperationException(
                "Undo and redo are not allowed while an edit transaction is active.");
        }

        if (replayingChanges)
        {
            throw new InvalidOperationException(
                "Editing changes are already being replayed.");
        }
    }

    internal void ReplayChangeSet(
        PdfEditChangeSet changeSet,
        bool undo)
    {
        ArgumentNullException.ThrowIfNull(changeSet);
        EnsureHistoryReplayAllowed();

        ReplayChanges(
            changeSet.GetChanges(),
            undo);
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

        ArgumentNullException.ThrowIfNull(options.Validators);

        for (var index = 0; index < options.Validators.Count; index++)
        {
            if (options.Validators[index] is null)
            {
                throw new ArgumentException(
                    "Editing validators cannot contain null entries.",
                    nameof(options));
            }
        }
    }
}
