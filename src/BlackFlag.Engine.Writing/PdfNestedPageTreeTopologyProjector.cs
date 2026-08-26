using BlackFlag.Engine.Document.Model;
using BlackFlag.Engine.Document.Objects;
using BlackFlag.Engine.Document.Objects.Primitives;
using BlackFlag.Engine.Document.Pages;

namespace BlackFlag.Engine.Writing;

/// <summary>
/// Projects final child membership across any number of existing `/Pages`
/// nodes while preserving node identities and hierarchy topology.
/// </summary>
public static class PdfNestedPageTreeTopologyProjector
{
    private static readonly PdfName KidsName =
        PdfName.FromLatin1("Kids");
    private static readonly PdfName CountName =
        PdfName.FromLatin1("Count");
    private static readonly PdfName ParentName =
        PdfName.FromLatin1("Parent");

    internal static PdfObjectWriteProjectionSet Create(
        PdfDocument document,
        PdfPageTreeHierarchySnapshot hierarchy,
        IReadOnlyDictionary<
            PdfObjectIdentifier,
            PdfObjectIdentifier[]> finalChildrenByNode,
        IReadOnlyDictionary<PdfPage, PdfObjectIdentifier>
            pendingPageBindings)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(hierarchy);
        ArgumentNullException.ThrowIfNull(finalChildrenByNode);
        ArgumentNullException.ThrowIfNull(pendingPageBindings);

        PdfNestedPageTreeTopologyAnalysis analysis =
            PdfNestedPageTreeTopologyAnalyzer.Analyze(
                hierarchy,
                finalChildrenByNode,
                pendingPageBindings);

        IReadOnlyDictionary<
            PdfObjectIdentifier,
            PdfObjectIdentifier> finalLeafParents =
                analysis.FinalLeafParents;
        IReadOnlyDictionary<PdfObjectIdentifier, int> finalCounts =
            analysis.FinalCounts;

        var projections =
            new Dictionary<
                PdfObjectIdentifier,
                PdfDictionary>();

        ProjectChangedNodes(
            document,
            hierarchy,
            finalChildrenByNode,
            finalCounts,
            projections);

        ProjectMovedAndPendingLeaves(
            document,
            hierarchy,
            finalLeafParents,
            pendingPageBindings,
            projections);

        if (projections.Count == 0)
        {
            throw new InvalidOperationException(
                "The final nested page-tree topology produced no writable projection.");
        }

        return new PdfObjectWriteProjectionSet(
            projections
                .OrderBy(
                    pair =>
                        pair.Key.ObjectNumber)
                .ThenBy(
                    pair =>
                        pair.Key.GenerationNumber)
                .Select(
                    pair =>
                        new PdfObjectWriteProjection(
                            pair.Key,
                            pair.Value)));
    }

    private static void ProjectChangedNodes(
        PdfDocument document,
        PdfPageTreeHierarchySnapshot hierarchy,
        IReadOnlyDictionary<
            PdfObjectIdentifier,
            PdfObjectIdentifier[]> finalChildrenByNode,
        IReadOnlyDictionary<PdfObjectIdentifier, int> finalCounts,
        Dictionary<
            PdfObjectIdentifier,
            PdfDictionary> projections)
    {
        foreach (PdfPageTreeHierarchySnapshot.Node node
                in hierarchy.Nodes)
        {
            PdfObjectIdentifier[] finalChildren =
                finalChildrenByNode[
                    node.Identifier];

            bool kidsChanged =
                !node.ChildIdentifiers.SequenceEqual(
                    finalChildren);
            bool countChanged =
                node.DescendantPageCount !=
                finalCounts[
                    node.Identifier];

            if (!kidsChanged &&
                !countChanged)
            {
                continue;
            }

            PdfDictionary projected =
                GetDictionary(
                    document,
                    node.Identifier)
                .CreateCopy();

            if (kidsChanged)
            {
                projected.Set(
                    KidsName,
                    new PdfArray(
                        finalChildren.Select(
                            child =>
                                (PdfObject)new PdfIndirectReference(
                                    child))));
            }

            projected.Set(
                CountName,
                new PdfInteger(
                    finalCounts[
                        node.Identifier]));

            projections[node.Identifier] =
                projected;
        }
    }

    private static void ProjectMovedAndPendingLeaves(
        PdfDocument document,
        PdfPageTreeHierarchySnapshot hierarchy,
        IReadOnlyDictionary<
            PdfObjectIdentifier,
            PdfObjectIdentifier> finalLeafParents,
        IReadOnlyDictionary<PdfPage, PdfObjectIdentifier>
            pendingPageBindings,
        Dictionary<
            PdfObjectIdentifier,
            PdfDictionary> projections)
    {
        var originalParents =
            hierarchy.Leaves.ToDictionary(
                leaf =>
                    leaf.Identifier,
                leaf =>
                    leaf.ParentIdentifier);

        foreach (KeyValuePair<
                    PdfObjectIdentifier,
                    PdfObjectIdentifier> entry
                in finalLeafParents)
        {
            if (TryGetPendingPage(
                    pendingPageBindings,
                    entry.Key,
                    out PdfPage? pendingPage) &&
                pendingPage is not null)
            {
                PdfDictionary projected =
                    pendingPage.CreateDictionarySnapshot();

                projected.Set(
                    ParentName,
                    new PdfIndirectReference(
                        entry.Value));

                projections[entry.Key] =
                    projected;
                continue;
            }

            if (!originalParents.TryGetValue(
                    entry.Key,
                    out PdfObjectIdentifier originalParent))
            {
                throw new InvalidOperationException(
                    $"Existing final page '{entry.Key}' is not present in the original hierarchy.");
            }

            if (originalParent ==
                entry.Value)
            {
                continue;
            }

            PdfDictionary existingProjected =
                GetDictionary(
                    document,
                    entry.Key)
                .CreateCopy();

            existingProjected.Set(
                ParentName,
                new PdfIndirectReference(
                    entry.Value));

            projections[entry.Key] =
                existingProjected;
        }
    }

    private static bool TryGetPendingPage(
        IReadOnlyDictionary<PdfPage, PdfObjectIdentifier>
            pendingPageBindings,
        PdfObjectIdentifier identifier,
        out PdfPage? page)
    {
        foreach (KeyValuePair<
                    PdfPage,
                    PdfObjectIdentifier> pair
                in pendingPageBindings)
        {
            if (pair.Value ==
                identifier)
            {
                page = pair.Key;
                return true;
            }
        }

        page = null;
        return false;
    }

    private static PdfDictionary GetDictionary(
        PdfDocument document,
        PdfObjectIdentifier identifier)
    {
        if (!document.Objects.TryGet(
                identifier,
                out PdfObject? value) ||
            value is null)
        {
            throw new InvalidOperationException(
                $"Page-tree object '{identifier}' is not present in the object store.");
        }

        return value as PdfDictionary ??
            throw new InvalidOperationException(
                $"Page-tree object '{identifier}' must be a PDF dictionary.");
    }
}
