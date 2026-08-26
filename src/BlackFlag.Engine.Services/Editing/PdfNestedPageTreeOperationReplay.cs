using BlackFlag.Engine.Document.Objects;
using BlackFlag.Engine.Editing.Documents;
using BlackFlag.Engine.Editing.Operations;
using BlackFlag.Engine.Editing.Transactions;
using BlackFlag.Engine.Writing;

namespace BlackFlag.Engine.Services.Editing;

internal static class PdfNestedPageTreeOperationReplay
{
    public static List<PdfNestedPageTreePlacement> CreateInitialPlacements(
        PdfPageTreeHierarchySnapshot hierarchy)
    {
        var placements =
            new List<PdfNestedPageTreePlacement>(
                hierarchy.Leaves.Count);

        foreach (PdfPageTreeHierarchySnapshot.Leaf leaf
                in hierarchy.Leaves)
        {
            placements.Add(
                PdfNestedPageTreePlacement.Existing(
                    leaf.Identifier,
                    leaf.ParentIdentifier));
        }

        return placements;
    }

    public static void Replay(
        PdfEditingSession session,
        PdfEditChangeSet changeSet,
        List<PdfNestedPageTreePlacement> placements)
    {
        for (int index = 0; index < changeSet.Count; index++)
        {
            if (changeSet[index] is not PdfPageEditOperation operation)
            {
                throw new NotSupportedException(
                    "Nested structural page Writing accepts only page editing operations.");
            }

            session.EnsureOwnership(operation);

            switch (operation.Kind)
            {
                case PdfPageMutationKind.Insert:
                    ReplayInsert(
                        operation,
                        placements);
                    break;

                case PdfPageMutationKind.Remove:
                    ReplayRemove(
                        operation,
                        placements);
                    break;

                case PdfPageMutationKind.Move:
                    ReplayMove(
                        operation,
                        placements);
                    break;

                case PdfPageMutationKind.Replace:
                    ReplayReplace(
                        operation,
                        placements);
                    break;

                case PdfPageMutationKind.SetRotation:
                    throw new NotSupportedException(
                        "Page rotation requires page-state Writing.");

                default:
                    throw new NotSupportedException(
                        $"Page mutation '{operation.Kind}' is not supported by nested structural Writing.");
            }
        }
    }

    private static void ReplayInsert(
        PdfPageEditOperation operation,
        List<PdfNestedPageTreePlacement> placements)
    {
        int index =
            operation.DestinationIndex ??
            throw new InvalidOperationException(
                "Insert operation is missing its destination index.");

        PdfObjectIdentifier parent =
            ResolveInsertionParent(
                placements,
                index);

        placements.Insert(
            index,
            PdfNestedPageTreePlacement.Pending(
                operation.PageId,
                parent));
    }

    private static void ReplayRemove(
        PdfPageEditOperation operation,
        List<PdfNestedPageTreePlacement> placements)
    {
        int index =
            operation.SourceIndex ??
            throw new InvalidOperationException(
                "Remove operation is missing its source index.");

        PdfNestedPageTreePlacement removed =
            GetRequiredPlacement(
                placements,
                index);

        ValidateOperationIdentity(
            operation,
            removed);

        placements.RemoveAt(index);
    }

    private static void ReplayMove(
        PdfPageEditOperation operation,
        List<PdfNestedPageTreePlacement> placements)
    {
        int sourceIndex =
            operation.SourceIndex ??
            throw new InvalidOperationException(
                "Move operation is missing its source index.");
        int destinationIndex =
            operation.DestinationIndex ??
            throw new InvalidOperationException(
                "Move operation is missing its destination index.");

        PdfNestedPageTreePlacement moved =
            GetRequiredPlacement(
                placements,
                sourceIndex);

        ValidateOperationIdentity(
            operation,
            moved);

        placements.RemoveAt(sourceIndex);

        PdfObjectIdentifier parent =
            ResolveInsertionParent(
                placements,
                destinationIndex);

        placements.Insert(
            destinationIndex,
            moved with
            {
                ParentIdentifier = parent
            });
    }

    private static void ReplayReplace(
        PdfPageEditOperation operation,
        List<PdfNestedPageTreePlacement> placements)
    {
        int index =
            operation.SourceIndex ??
            throw new InvalidOperationException(
                "Replace operation is missing its source index.");

        PdfNestedPageTreePlacement existing =
            GetRequiredPlacement(
                placements,
                index);

        ValidateOperationIdentity(
            operation,
            existing);

        Guid replacementId =
            operation.RelatedPageId ??
            throw new InvalidOperationException(
                "Replace operation is missing its replacement page identity.");

        placements[index] =
            operation.RelatedObjectIdentifier.HasValue
                ? PdfNestedPageTreePlacement.Existing(
                    operation.RelatedObjectIdentifier.Value,
                    existing.ParentIdentifier)
                : PdfNestedPageTreePlacement.Pending(
                    replacementId,
                    existing.ParentIdentifier);
    }

    private static PdfNestedPageTreePlacement GetRequiredPlacement(
        List<PdfNestedPageTreePlacement> placements,
        int index)
    {
        if (index < 0 || index >= placements.Count)
        {
            throw new InvalidOperationException(
                $"Committed structural page index '{index}' is outside the replayed source-page range.");
        }

        return placements[index];
    }

    private static void ValidateOperationIdentity(
        PdfPageEditOperation operation,
        PdfNestedPageTreePlacement placement)
    {
        if (operation.ObjectIdentifier.HasValue)
        {
            if (placement.ObjectIdentifier !=
                operation.ObjectIdentifier)
            {
                throw new InvalidOperationException(
                    $"Committed page operation for '{operation.ObjectIdentifier}' does not match the page found at its recorded source index.");
            }

            return;
        }

        if (placement.PageId != operation.PageId)
        {
            throw new InvalidOperationException(
                $"Committed page operation for '{operation.PageId}' does not match the pending page found at its recorded source index.");
        }
    }

    private static PdfObjectIdentifier ResolveInsertionParent(
        List<PdfNestedPageTreePlacement> placements,
        int index)
    {
        if (index < 0 || index > placements.Count)
        {
            throw new InvalidOperationException(
                $"Committed destination index '{index}' is outside the replayed page range.");
        }

        if (index < placements.Count)
        {
            return placements[index].ParentIdentifier;
        }

        if (index > 0)
        {
            return placements[index - 1].ParentIdentifier;
        }

        throw new NotSupportedException(
            "Cannot infer a nested target /Pages node when inserting into an empty document.");
    }
}

internal sealed record PdfNestedPageTreePlacement(
    PdfObjectIdentifier? ObjectIdentifier,
    Guid? PageId,
    PdfObjectIdentifier ParentIdentifier)
{
    public static PdfNestedPageTreePlacement Existing(
        PdfObjectIdentifier identifier,
        PdfObjectIdentifier parentIdentifier) =>
        new(
            identifier,
            PageId: null,
            parentIdentifier);

    public static PdfNestedPageTreePlacement Pending(
        Guid pageId,
        PdfObjectIdentifier parentIdentifier) =>
        new(
            ObjectIdentifier: null,
            pageId,
            parentIdentifier);
}
