namespace BlackFlag.Engine.Document.Outlines;

/// <summary>Represents an immutable logical navigation action for an outline item.</summary>
public sealed record PdfOutlineNavigationAction
{
    public PdfOutlineNavigationAction(
        PdfOutlineNavigationActionType actionType,
        string target)
    {
        if (!Enum.IsDefined(actionType))
        {
            throw new ArgumentOutOfRangeException(
                nameof(actionType),
                actionType,
                "The outline navigation action type is not supported.");
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(target);
        ActionType = actionType;
        Target = target;
    }

    public PdfOutlineNavigationActionType ActionType { get; }
    public string Target { get; }
}
