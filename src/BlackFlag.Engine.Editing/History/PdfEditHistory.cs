using BlackFlag.Engine.Editing.Documents;
using BlackFlag.Engine.Editing.Transactions;

namespace BlackFlag.Engine.Editing.History;

/// <summary>
/// Owns the committed undo and redo history for one PDF editing session.
/// </summary>
public sealed class PdfEditHistory
{
    private readonly PdfEditingSession session;
    private readonly Stack<PdfEditChangeSet> undoStack = new();
    private readonly Stack<PdfEditChangeSet> redoStack = new();

    internal PdfEditHistory(
        PdfEditingSession session)
    {
        ArgumentNullException.ThrowIfNull(session);
        this.session = session;
    }

    /// <summary>
    /// Gets whether at least one committed change set can be undone.
    /// </summary>
    public bool CanUndo =>
        undoStack.Count > 0;

    /// <summary>
    /// Gets whether at least one undone change set can be redone.
    /// </summary>
    public bool CanRedo =>
        redoStack.Count > 0;

    /// <summary>
    /// Gets the number of committed change sets available for undo.
    /// </summary>
    public int UndoCount =>
        undoStack.Count;

    /// <summary>
    /// Gets the number of undone change sets available for redo.
    /// </summary>
    public int RedoCount =>
        redoStack.Count;

    /// <summary>
    /// Undoes the most recently committed change set.
    /// </summary>
    public PdfEditChangeSet Undo()
    {
        session.EnsureHistoryReplayAllowed();

        if (!undoStack.TryPeek(out var changeSet))
        {
            throw new InvalidOperationException(
                "There are no committed PDF editing changes to undo.");
        }

        session.ReplayChangeSet(
            changeSet,
            undo: true);

        _ = undoStack.Pop();
        redoStack.Push(changeSet);

        return changeSet;
    }

    /// <summary>
    /// Redoes the most recently undone change set.
    /// </summary>
    public PdfEditChangeSet Redo()
    {
        session.EnsureHistoryReplayAllowed();

        if (!redoStack.TryPeek(out var changeSet))
        {
            throw new InvalidOperationException(
                "There are no undone PDF editing changes to redo.");
        }

        session.ReplayChangeSet(
            changeSet,
            undo: false);

        _ = redoStack.Pop();
        undoStack.Push(changeSet);

        return changeSet;
    }

    internal void RecordCommitted(
        PdfEditChangeSet changeSet)
    {
        ArgumentNullException.ThrowIfNull(changeSet);

        if (changeSet.IsEmpty)
        {
            return;
        }

        undoStack.Push(changeSet);
        redoStack.Clear();
    }
}
