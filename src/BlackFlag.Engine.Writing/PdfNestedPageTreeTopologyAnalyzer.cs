using BlackFlag.Engine.Document.Objects;
using BlackFlag.Engine.Document.Pages;

namespace BlackFlag.Engine.Writing;

internal static class PdfNestedPageTreeTopologyAnalyzer
{
    public static PdfNestedPageTreeTopologyAnalysis Analyze(
        PdfPageTreeHierarchySnapshot hierarchy,
        IReadOnlyDictionary<
            PdfObjectIdentifier,
            PdfObjectIdentifier[]> finalChildrenByNode,
        IReadOnlyDictionary<PdfPage, PdfObjectIdentifier>
            pendingPageBindings)
    {
        ArgumentNullException.ThrowIfNull(hierarchy);
        ArgumentNullException.ThrowIfNull(finalChildrenByNode);
        ArgumentNullException.ThrowIfNull(pendingPageBindings);

        ValidateNodeMap(
            hierarchy,
            finalChildrenByNode);

        Dictionary<PdfObjectIdentifier, PdfObjectIdentifier>
            finalLeafParents =
                ValidateAndMapFinalLeafParents(
                    hierarchy,
                    finalChildrenByNode,
                    pendingPageBindings);

        Dictionary<PdfObjectIdentifier, int> finalCounts =
            ComputeFinalCounts(
                hierarchy,
                finalChildrenByNode,
                pendingPageBindings);

        return new PdfNestedPageTreeTopologyAnalysis(
            finalLeafParents,
            finalCounts);
    }

    private static void ValidateNodeMap(
        PdfPageTreeHierarchySnapshot hierarchy,
        IReadOnlyDictionary<
            PdfObjectIdentifier,
            PdfObjectIdentifier[]> finalChildrenByNode)
    {
        if (finalChildrenByNode.Count !=
            hierarchy.Nodes.Count)
        {
            throw new InvalidOperationException(
                "The final page-tree child map must contain every existing `/Pages` node exactly once.");
        }

        foreach (PdfPageTreeHierarchySnapshot.Node node
                in hierarchy.Nodes)
        {
            if (!finalChildrenByNode.ContainsKey(
                    node.Identifier))
            {
                throw new InvalidOperationException(
                    $"The final page-tree child map is missing node '{node.Identifier}'.");
            }
        }
    }

    private static Dictionary<
        PdfObjectIdentifier,
        PdfObjectIdentifier>
        ValidateAndMapFinalLeafParents(
        PdfPageTreeHierarchySnapshot hierarchy,
        IReadOnlyDictionary<
            PdfObjectIdentifier,
            PdfObjectIdentifier[]> finalChildrenByNode,
        IReadOnlyDictionary<PdfPage, PdfObjectIdentifier>
            pendingPageBindings)
    {
        var finalParents =
            new Dictionary<
                PdfObjectIdentifier,
                PdfObjectIdentifier>();
        HashSet<PdfObjectIdentifier> allKnownNodes =
            hierarchy.Nodes
                .Select(node => node.Identifier)
                .ToHashSet();
        HashSet<PdfObjectIdentifier> allKnownLeaves =
            hierarchy.Leaves
                .Select(leaf => leaf.Identifier)
                .ToHashSet();
        HashSet<PdfObjectIdentifier> pendingIdentifiers =
            pendingPageBindings.Values.ToHashSet();

        foreach (KeyValuePair<
                    PdfObjectIdentifier,
                    PdfObjectIdentifier[]> entry
                in finalChildrenByNode)
        {
            var localSeen =
                new HashSet<PdfObjectIdentifier>();

            foreach (PdfObjectIdentifier child
                    in entry.Value)
            {
                if (!localSeen.Add(child))
                {
                    throw new InvalidOperationException(
                        $"Child '{child}' occurs more than once in node '{entry.Key}'.");
                }

                if (allKnownNodes.Contains(child))
                {
                    ValidateNodeParent(
                        hierarchy,
                        child,
                        entry.Key);
                    continue;
                }

                if (!allKnownLeaves.Contains(child) &&
                    !pendingIdentifiers.Contains(child))
                {
                    throw new InvalidOperationException(
                        $"Child '{child}' is neither an existing page-tree object nor a pending page.");
                }

                if (!finalParents.TryAdd(
                        child,
                        entry.Key))
                {
                    throw new InvalidOperationException(
                        $"Page leaf '{child}' occurs in more than one final `/Kids` array.");
                }
            }
        }

        foreach (PdfObjectIdentifier pending
                in pendingIdentifiers)
        {
            if (!finalParents.ContainsKey(pending))
            {
                throw new InvalidOperationException(
                    $"Pending page '{pending}' is not attached to a final `/Pages` node.");
            }
        }

        return finalParents;
    }

    private static void ValidateNodeParent(
        PdfPageTreeHierarchySnapshot hierarchy,
        PdfObjectIdentifier child,
        PdfObjectIdentifier parent)
    {
        PdfPageTreeHierarchySnapshot.Node childNode =
            hierarchy.GetRequiredNode(child);

        if (childNode.ParentIdentifier != parent)
        {
            throw new NotSupportedException(
                $"Reparenting `/Pages` node '{child}' is not supported by topology preservation.");
        }
    }

    private static Dictionary<PdfObjectIdentifier, int>
        ComputeFinalCounts(
        PdfPageTreeHierarchySnapshot hierarchy,
        IReadOnlyDictionary<
            PdfObjectIdentifier,
            PdfObjectIdentifier[]> finalChildrenByNode,
        IReadOnlyDictionary<PdfPage, PdfObjectIdentifier>
            pendingPageBindings)
    {
        var counts =
            new Dictionary<PdfObjectIdentifier, int>();
        var active =
            new HashSet<PdfObjectIdentifier>();
        HashSet<PdfObjectIdentifier> existingLeaves =
            hierarchy.Leaves
                .Select(leaf => leaf.Identifier)
                .ToHashSet();
        HashSet<PdfObjectIdentifier> pendingLeaves =
            pendingPageBindings.Values.ToHashSet();

        foreach (PdfPageTreeHierarchySnapshot.Node node
                in hierarchy.Nodes)
        {
            _ = ComputeNodeCount(
                node.Identifier,
                finalChildrenByNode,
                existingLeaves,
                pendingLeaves,
                counts,
                active);
        }

        return counts;
    }

    private static int ComputeNodeCount(
        PdfObjectIdentifier nodeIdentifier,
        IReadOnlyDictionary<
            PdfObjectIdentifier,
            PdfObjectIdentifier[]> finalChildrenByNode,
        IReadOnlySet<PdfObjectIdentifier> existingLeaves,
        IReadOnlySet<PdfObjectIdentifier> pendingLeaves,
        Dictionary<PdfObjectIdentifier, int> counts,
        HashSet<PdfObjectIdentifier> active)
    {
        if (counts.TryGetValue(
                nodeIdentifier,
                out int cached))
        {
            return cached;
        }

        if (!active.Add(nodeIdentifier))
        {
            throw new InvalidOperationException(
                $"The final page-tree child map contains a cycle at '{nodeIdentifier}'.");
        }

        int count = 0;

        try
        {
            foreach (PdfObjectIdentifier child
                    in finalChildrenByNode[nodeIdentifier])
            {
                if (existingLeaves.Contains(child) ||
                    pendingLeaves.Contains(child))
                {
                    count++;
                    continue;
                }

                if (!finalChildrenByNode.ContainsKey(child))
                {
                    throw new InvalidOperationException(
                        $"Final page-tree child '{child}' is not a page leaf or `/Pages` node.");
                }

                count +=
                    ComputeNodeCount(
                        child,
                        finalChildrenByNode,
                        existingLeaves,
                        pendingLeaves,
                        counts,
                        active);
            }
        }
        finally
        {
            _ = active.Remove(nodeIdentifier);
        }

        counts.Add(nodeIdentifier, count);
        return count;
    }
}

internal sealed record PdfNestedPageTreeTopologyAnalysis(
    IReadOnlyDictionary<PdfObjectIdentifier, PdfObjectIdentifier>
        FinalLeafParents,
    IReadOnlyDictionary<PdfObjectIdentifier, int>
        FinalCounts);
