using BlackFlag.Engine.Document.Model;
using BlackFlag.Engine.Document.Objects;
using BlackFlag.Engine.Document.Objects.Primitives;
using BlackFlag.Engine.Document.Pages;

namespace BlackFlag.Engine.Writing;

/// <summary>
/// Projects coordinated page-leaf movement/replacement across two existing
/// `/Pages` nodes without flattening the surrounding hierarchy.
/// </summary>
public static class PdfNestedPageTreeCrossNodeProjector
{
    private static readonly PdfName KidsName =
        PdfName.FromLatin1("Kids");
    private static readonly PdfName CountName =
        PdfName.FromLatin1("Count");
    private static readonly PdfName ParentName =
        PdfName.FromLatin1("Parent");

    public static PdfObjectWriteProjectionSet Create(
        PdfDocument document,
        PdfObjectIdentifier sourceNodeIdentifier,
        IReadOnlyList<PdfObjectIdentifier> finalSourceChildren,
        PdfObjectIdentifier destinationNodeIdentifier,
        IReadOnlyList<PdfObjectIdentifier> finalDestinationChildren,
        IReadOnlyDictionary<PdfPage, PdfObjectIdentifier>
            pendingPageBindings)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(finalSourceChildren);
        ArgumentNullException.ThrowIfNull(finalDestinationChildren);
        ArgumentNullException.ThrowIfNull(pendingPageBindings);

        if (sourceNodeIdentifier == destinationNodeIdentifier)
        {
            throw new ArgumentException(
                "Cross-node projection requires distinct source and destination nodes.");
        }

        PdfPageTreeHierarchySnapshot hierarchy =
            PdfPageTreeHierarchySnapshot.Create(document);

        PdfPageTreeHierarchySnapshot.Node sourceNode =
            hierarchy.GetRequiredNode(sourceNodeIdentifier);
        PdfPageTreeHierarchySnapshot.Node destinationNode =
            hierarchy.GetRequiredNode(destinationNodeIdentifier);

        ValidateFinalChildren(
            hierarchy,
            sourceNode,
            destinationNode,
            finalSourceChildren,
            finalDestinationChildren,
            pendingPageBindings);

        var projections =
            new Dictionary<PdfObjectIdentifier, PdfDictionary>();

        int sourceCount =
            CountDescendantPages(
                hierarchy,
                finalSourceChildren,
                pendingPageBindings);
        int destinationCount =
            CountDescendantPages(
                hierarchy,
                finalDestinationChildren,
                pendingPageBindings);

        projections[sourceNodeIdentifier] =
            CreateNodeProjection(
                document,
                sourceNodeIdentifier,
                finalSourceChildren,
                sourceCount);

        projections[destinationNodeIdentifier] =
            CreateNodeProjection(
                document,
                destinationNodeIdentifier,
                finalDestinationChildren,
                destinationCount);

        ProjectDestinationLeafParents(
            document,
            hierarchy,
            destinationNodeIdentifier,
            finalDestinationChildren,
            pendingPageBindings,
            projections);

        Dictionary<PdfObjectIdentifier, int> countDeltas =
            CreateAncestorDeltas(
                hierarchy,
                sourceNode,
                sourceCount,
                destinationNode,
                destinationCount);

        foreach (KeyValuePair<PdfObjectIdentifier, int> delta
                in countDeltas)
        {
            if (delta.Value == 0)
            {
                continue;
            }

            PdfPageTreeHierarchySnapshot.Node node =
                hierarchy.GetRequiredNode(
                    delta.Key);

            PdfDictionary projected =
                projections.TryGetValue(
                    node.Identifier,
                    out PdfDictionary? existing)
                    ? existing
                    : GetDictionary(
                        document,
                        node.Identifier)
                    .CreateCopy();

            projected.Set(
                CountName,
                new PdfInteger(
                    node.DescendantPageCount +
                    delta.Value));

            projections[node.Identifier] =
                projected;
        }

        return new PdfObjectWriteProjectionSet(
            projections
                .OrderBy(pair => pair.Key.ObjectNumber)
                .ThenBy(pair => pair.Key.GenerationNumber)
                .Select(
                    pair =>
                        new PdfObjectWriteProjection(
                            pair.Key,
                            pair.Value)));
    }

    private static PdfDictionary CreateNodeProjection(
        PdfDocument document,
        PdfObjectIdentifier identifier,
        IReadOnlyList<PdfObjectIdentifier> finalChildren,
        int descendantCount)
    {
        PdfDictionary projected =
            GetDictionary(
                document,
                identifier)
            .CreateCopy();

        projected.Set(
            KidsName,
            new PdfArray(
                finalChildren.Select(
                    child =>
                        (PdfObject)new PdfIndirectReference(
                            child))));

        projected.Set(
            CountName,
            new PdfInteger(
                descendantCount));

        return projected;
    }

    private static void ProjectDestinationLeafParents(
        PdfDocument document,
        PdfPageTreeHierarchySnapshot hierarchy,
        PdfObjectIdentifier destinationNodeIdentifier,
        IReadOnlyList<PdfObjectIdentifier> finalDestinationChildren,
        IReadOnlyDictionary<PdfPage, PdfObjectIdentifier>
            pendingPageBindings,
        Dictionary<PdfObjectIdentifier, PdfDictionary> projections)
    {
        foreach (PdfObjectIdentifier childIdentifier
                in finalDestinationChildren)
        {
            if (TryGetPendingPage(
                    pendingPageBindings,
                    childIdentifier,
                    out PdfPage? pendingPage) &&
                pendingPage is not null)
            {
                PdfDictionary newPage =
                    pendingPage.CreateDictionarySnapshot();

                newPage.Set(
                    ParentName,
                    new PdfIndirectReference(
                        destinationNodeIdentifier));

                projections[childIdentifier] =
                    newPage;

                continue;
            }

            if (!hierarchy.Leaves.Any(
                    leaf =>
                        leaf.Identifier == childIdentifier))
            {
                continue;
            }

            PdfDictionary page =
                GetDictionary(
                    document,
                    childIdentifier)
                .CreateCopy();

            page.Set(
                ParentName,
                new PdfIndirectReference(
                    destinationNodeIdentifier));

            projections[childIdentifier] =
                page;
        }
    }

    private static Dictionary<PdfObjectIdentifier, int>
        CreateAncestorDeltas(
        PdfPageTreeHierarchySnapshot hierarchy,
        PdfPageTreeHierarchySnapshot.Node sourceNode,
        int newSourceCount,
        PdfPageTreeHierarchySnapshot.Node destinationNode,
        int newDestinationCount)
    {
        var deltas =
            new Dictionary<PdfObjectIdentifier, int>();

        AddAncestorDelta(
            hierarchy,
            sourceNode,
            newSourceCount -
            sourceNode.DescendantPageCount,
            deltas);

        AddAncestorDelta(
            hierarchy,
            destinationNode,
            newDestinationCount -
            destinationNode.DescendantPageCount,
            deltas);

        return deltas;
    }

    private static void AddAncestorDelta(
        PdfPageTreeHierarchySnapshot hierarchy,
        PdfPageTreeHierarchySnapshot.Node node,
        int delta,
        Dictionary<PdfObjectIdentifier, int> deltas)
    {
        PdfObjectIdentifier? parentIdentifier =
            node.ParentIdentifier;

        while (parentIdentifier.HasValue)
        {
            if (deltas.TryGetValue(
                    parentIdentifier.Value,
                    out int current))
            {
                deltas[parentIdentifier.Value] =
                    current + delta;
            }
            else
            {
                deltas.Add(
                    parentIdentifier.Value,
                    delta);
            }

            parentIdentifier =
                hierarchy.GetRequiredNode(
                    parentIdentifier.Value)
                .ParentIdentifier;
        }
    }

    private static int CountDescendantPages(
        PdfPageTreeHierarchySnapshot hierarchy,
        IReadOnlyList<PdfObjectIdentifier> children,
        IReadOnlyDictionary<PdfPage, PdfObjectIdentifier>
            pendingPageBindings)
    {
        int count = 0;

        foreach (PdfObjectIdentifier identifier
                in children)
        {
            if (hierarchy.Leaves.Any(
                    leaf =>
                        leaf.Identifier == identifier) ||
                TryGetPendingPage(
                    pendingPageBindings,
                    identifier,
                    out _))
            {
                count++;
                continue;
            }

            count +=
                hierarchy.GetRequiredNode(
                    identifier)
                .DescendantPageCount;
        }

        return count;
    }

    private static void ValidateFinalChildren(
        PdfPageTreeHierarchySnapshot hierarchy,
        PdfPageTreeHierarchySnapshot.Node sourceNode,
        PdfPageTreeHierarchySnapshot.Node destinationNode,
        IReadOnlyList<PdfObjectIdentifier> finalSourceChildren,
        IReadOnlyList<PdfObjectIdentifier> finalDestinationChildren,
        IReadOnlyDictionary<PdfPage, PdfObjectIdentifier>
            pendingPageBindings)
    {
        ValidateNoDuplicates(
            finalSourceChildren,
            "source");
        ValidateNoDuplicates(
            finalDestinationChildren,
            "destination");

        var sourceOriginal =
            sourceNode.ChildIdentifiers.ToHashSet();
        var destinationOriginal =
            destinationNode.ChildIdentifiers.ToHashSet();

        foreach (PdfObjectIdentifier identifier
                in finalSourceChildren)
        {
            if (!sourceOriginal.Contains(identifier))
            {
                throw new InvalidOperationException(
                    $"Source child '{identifier}' was not originally attached to the source node.");
            }
        }

        foreach (PdfObjectIdentifier identifier
                in finalDestinationChildren)
        {
            if (destinationOriginal.Contains(identifier) ||
                pendingPageBindings.Values.Contains(identifier))
            {
                continue;
            }

            if (!sourceOriginal.Contains(identifier))
            {
                throw new InvalidOperationException(
                    $"Destination child '{identifier}' is neither an existing destination child, a source child, nor a pending page.");
            }

            if (!hierarchy.Leaves.Any(
                    leaf =>
                        leaf.Identifier == identifier))
            {
                throw new InvalidOperationException(
                    $"Cross-node movement of `/Pages` child '{identifier}' is not supported in this slice.");
            }
        }

        foreach (PdfObjectIdentifier identifier
                in finalSourceChildren)
        {
            if (finalDestinationChildren.Contains(identifier))
            {
                throw new InvalidOperationException(
                    $"Child '{identifier}' cannot remain in source and destination simultaneously.");
            }
        }

        foreach (PdfObjectIdentifier pendingIdentifier
                in pendingPageBindings.Values)
        {
            if (!finalDestinationChildren.Contains(
                    pendingIdentifier))
            {
                continue;
            }

            if (finalSourceChildren.Contains(
                    pendingIdentifier))
            {
                throw new InvalidOperationException(
                    $"Pending page '{pendingIdentifier}' cannot occur in both nodes.");
            }
        }
    }

    private static void ValidateNoDuplicates(
        IReadOnlyList<PdfObjectIdentifier> identifiers,
        string side)
    {
        var seen =
            new HashSet<PdfObjectIdentifier>();

        foreach (PdfObjectIdentifier identifier
                in identifiers)
        {
            if (!seen.Add(identifier))
            {
                throw new InvalidOperationException(
                    $"Child '{identifier}' occurs more than once in the final {side} `/Kids` array.");
            }
        }
    }

    private static bool TryGetPendingPage(
        IReadOnlyDictionary<PdfPage, PdfObjectIdentifier>
            pendingPageBindings,
        PdfObjectIdentifier identifier,
        out PdfPage? page)
    {
        foreach (KeyValuePair<PdfPage, PdfObjectIdentifier> pair
                in pendingPageBindings)
        {
            if (pair.Value == identifier)
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
