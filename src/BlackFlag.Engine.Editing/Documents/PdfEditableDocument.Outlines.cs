using BlackFlag.Engine.Editing.Operations;
using BlackFlag.Engine.Document.Outlines;
using BlackFlag.Engine.Editing.Outlines;
using BlackFlag.Engine.Editing.Pages;
using BlackFlag.Engine.Editing.Structural;

namespace BlackFlag.Engine.Editing.Documents;

public sealed partial class PdfEditableDocument
{
    /// <summary>Creates a detached outline-item handle owned by this editing session.</summary>
    public PdfEditableOutlineItem CreateOutlineItem(string title)
    {
        ArgumentNullException.ThrowIfNull(title);
        return new PdfEditableOutlineItem(
            new PdfOutlineItem(title),
            owner);
    }

    /// <summary>Appends a detached item to the document-outline root.</summary>
    public PdfOutlineEditOperation AddOutlineItem(PdfEditableOutlineItem item)
    {
        EnsureOwnership(item);

        if (item.IsChild)
        {
            throw new InvalidOperationException(
                "A child outline item cannot also be attached at the outline root.");
        }

        var index = OutlineItems.Count;
        var snapshot = CreateOutlineItemSnapshot(item);
        var operation = new PdfOutlineEditOperation(
            owner,
            PdfOutlineMutationKind.AddRoot,
            item.Id,
            PdfStructuralValueState.Absent<PdfOutlineItemSnapshot>(),
            PdfStructuralValueState.Explicit(snapshot),
            index: index);

        session.ValidateOperation(operation);
        OutlineItems.Insert(index, item);
        session.RecordChange(
            operation,
            () => OutlineItems.Remove(item),
            () => OutlineItems.Insert(index, item));

        return operation;
    }

    /// <summary>Removes a root outline item while retaining its stable session handle.</summary>
    public PdfOutlineEditOperation RemoveOutlineItem(PdfEditableOutlineItem item)
    {
        EnsureOwnership(item);
        var index = OutlineItems.IndexOf(item);

        if (index < 0)
        {
            throw new InvalidOperationException(
                "The outline item is not attached at the document-outline root.");
        }

        var snapshot = CreateOutlineItemSnapshot(item);
        var operation = new PdfOutlineEditOperation(
            owner,
            PdfOutlineMutationKind.RemoveRoot,
            item.Id,
            PdfStructuralValueState.Explicit(snapshot),
            PdfStructuralValueState.Absent<PdfOutlineItemSnapshot>(),
            index: index);

        session.ValidateOperation(operation);
        _ = OutlineItems.Remove(item);
        session.RecordChange(
            operation,
            () => OutlineItems.Insert(index, item),
            () => OutlineItems.Remove(item));

        return operation;
    }

    /// <summary>Appends a detached outline item as a child of another item.</summary>
    public PdfOutlineEditOperation AddChildOutlineItem(
        PdfEditableOutlineItem parent,
        PdfEditableOutlineItem child)
    {
        EnsureOwnership(parent);
        EnsureOwnership(child);

        if (OutlineItems.Contains(child))
        {
            throw new InvalidOperationException(
                "A root outline item must be removed from the outline root before it can become a child.");
        }

        var index = parent.Children.Count;
        var previous = CreateOutlineItemSnapshot(child);
        var operation = new PdfOutlineEditOperation(
            owner,
            PdfOutlineMutationKind.AddChild,
            child.Id,
            PdfStructuralValueState.Explicit(previous),
            PdfStructuralValueState.Explicit(
                previous with { ParentItemId = parent.Id }),
            parent.Id,
            index);

        session.ValidateOperation(operation);
        parent.Children.Insert(index, child);
        session.RecordChange(
            operation,
            () => parent.Children.Remove(child),
            () => parent.Children.Insert(index, child));

        return operation;
    }

    /// <summary>Detaches a child outline item while retaining its stable handle.</summary>
    public PdfOutlineEditOperation RemoveChildOutlineItem(
        PdfEditableOutlineItem parent,
        PdfEditableOutlineItem child)
    {
        EnsureOwnership(parent);
        EnsureOwnership(child);
        var index = parent.Children.IndexOf(child);

        if (index < 0)
        {
            throw new InvalidOperationException(
                "The outline item is not a child of the supplied parent item.");
        }

        var previous = CreateOutlineItemSnapshot(child);
        var operation = new PdfOutlineEditOperation(
            owner,
            PdfOutlineMutationKind.RemoveChild,
            child.Id,
            PdfStructuralValueState.Explicit(previous),
            PdfStructuralValueState.Explicit(
                previous with { ParentItemId = null }),
            parent.Id,
            index);

        session.ValidateOperation(operation);
        _ = parent.Children.Remove(child);
        session.RecordChange(
            operation,
            () => parent.Children.Insert(index, child),
            () => parent.Children.Remove(child));

        return operation;
    }

    /// <summary>Sets the visible outline title explicitly, including an empty string.</summary>
    public PdfOutlineEditOperation SetOutlineTitle(
        PdfEditableOutlineItem item,
        string title)
    {
        ArgumentNullException.ThrowIfNull(title);
        return UpdateOutlineItem(
            item,
            PdfOutlineMutationKind.SetTitle,
            source => source.Title = title);
    }

    /// <summary>Sets whether the outline item's children are logically expanded.</summary>
    public PdfOutlineEditOperation SetOutlineOpenState(
        PdfEditableOutlineItem item,
        bool isOpen)
    {
        return UpdateOutlineItem(
            item,
            PdfOutlineMutationKind.SetOpenState,
            source => source.IsOpen = isOpen);
    }

    /// <summary>Sets a direct page destination and clears any navigation action.</summary>
    public PdfOutlineEditOperation SetOutlineDestination(
        PdfEditableOutlineItem item,
        PdfEditablePage page)
    {
        EnsureOwnership(page);

        if (!Pages.Contains(page))
        {
            throw new InvalidOperationException(
                "An outline destination must reference a page attached to the editable document.");
        }

        return UpdateOutlineItem(
            item,
            PdfOutlineMutationKind.SetDestination,
            source =>
            {
                source.Destination = new PdfOutlineDestination(page.Id);
                source.Action = null;
            });
    }

    /// <summary>Sets a navigation action and clears any direct page destination.</summary>
    public PdfOutlineEditOperation SetOutlineAction(
        PdfEditableOutlineItem item,
        PdfOutlineNavigationActionType actionType,
        string target)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(target);
        var action = new PdfOutlineNavigationAction(actionType, target);

        return UpdateOutlineItem(
            item,
            PdfOutlineMutationKind.SetAction,
            source =>
            {
                source.Action = action;
                source.Destination = null;
            });
    }

    /// <summary>Clears the item's destination or navigation action.</summary>
    public PdfOutlineEditOperation ClearOutlineTarget(PdfEditableOutlineItem item)
    {
        if (item.Destination is null && item.Action is null)
        {
            throw new KeyNotFoundException(
                "The outline item does not have an explicit destination or navigation action.");
        }

        return UpdateOutlineItem(
            item,
            PdfOutlineMutationKind.ClearTarget,
            source =>
            {
                source.Destination = null;
                source.Action = null;
            });
    }

    private PdfOutlineEditOperation UpdateOutlineItem(
        PdfEditableOutlineItem item,
        PdfOutlineMutationKind kind,
        Action<PdfOutlineItem> mutate)
    {
        ArgumentNullException.ThrowIfNull(mutate);
        EnsureOwnership(item);
        var source = item.GetSourceItem();
        var previous = CreateOutlineItemSnapshot(item);
        var preview = new PdfOutlineItem(source.Title)
        {
            IsOpen = source.IsOpen,
            Destination = source.Destination,
            Action = source.Action
        };
        mutate(preview);

        var current = new PdfOutlineItemSnapshot(
            preview.Title,
            preview.IsOpen,
            preview.Destination,
            preview.Action,
            previous.ParentItemId,
            previous.ChildCount);
        var operation = new PdfOutlineEditOperation(
            owner,
            kind,
            item.Id,
            PdfStructuralValueState.Explicit(previous),
            PdfStructuralValueState.Explicit(current),
            item.ParentItemId);

        session.ValidateOperation(operation);
        RestoreOutlineItemSnapshot(source, current);
        session.RecordChange(
            operation,
            () => RestoreOutlineItemSnapshot(source, previous),
            () => RestoreOutlineItemSnapshot(source, current));

        return operation;
    }

    private static PdfOutlineItemSnapshot CreateOutlineItemSnapshot(
        PdfEditableOutlineItem item)
    {
        var source = item.GetSourceItem();
        return new PdfOutlineItemSnapshot(
            source.Title,
            source.IsOpen,
            source.Destination,
            source.Action,
            source.Parent?.Id,
            source.Children.Count);
    }

    private static void RestoreOutlineItemSnapshot(
        PdfOutlineItem item,
        PdfOutlineItemSnapshot snapshot)
    {
        item.Title = snapshot.Title;
        item.IsOpen = snapshot.IsOpen;
        item.Destination = snapshot.Destination;
        item.Action = snapshot.Action;
    }
}
