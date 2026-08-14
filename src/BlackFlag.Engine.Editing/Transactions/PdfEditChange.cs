using BlackFlag.Engine.Editing.Operations;

namespace BlackFlag.Engine.Editing.Transactions;

internal sealed class PdfEditChange
{
    private readonly Action redo;
    private readonly Action undo;

    internal PdfEditChange(
        IPdfEditOperation operation,
        Action undo,
        Action redo)
    {
        ArgumentNullException.ThrowIfNull(operation);
        ArgumentNullException.ThrowIfNull(undo);
        ArgumentNullException.ThrowIfNull(redo);

        Operation = operation;
        this.undo = undo;
        this.redo = redo;
    }

    internal IPdfEditOperation Operation { get; }

    internal void Undo() =>
        undo();

    internal void Redo() =>
        redo();
}
