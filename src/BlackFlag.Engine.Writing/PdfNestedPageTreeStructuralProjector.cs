using BlackFlag.Engine.Document.Model;
using BlackFlag.Engine.Document.Objects;
using BlackFlag.Engine.Document.Objects.Primitives;
using BlackFlag.Engine.Document.Pages;

namespace BlackFlag.Engine.Writing;

/// <summary>
/// Projects insert/remove mutations inside one existing nested `/Pages` node
/// while preserving the surrounding hierarchy.
/// </summary>
public static class PdfNestedPageTreeStructuralProjector
{
    private static readonly PdfName KidsName =
        PdfName.FromLatin1("Kids");
    private static readonly PdfName CountName =
        PdfName.FromLatin1("Count");
    private static readonly PdfName ParentName =
        PdfName.FromLatin1("Parent");

    public static PdfObjectWriteProjectionSet Create(
        PdfDocument document,
        PdfObjectIdentifier targetNodeIdentifier,
        IReadOnlyList<PdfObjectIdentifier> finalChildIdentifiers,
        IReadOnlyDictionary<PdfPage, PdfObjectIdentifier>
            pendingPageBindings)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(finalChildIdentifiers);
        ArgumentNullException.ThrowIfNull(pendingPageBindings);

        PdfPageTreeHierarchySnapshot hierarchy =
            PdfPageTreeHierarchySnapshot.Create(document);

        PdfPageTreeHierarchySnapshot.Node targetNode =
            hierarchy.GetRequiredNode(targetNodeIdentifier);

        ValidateFinalChildren(
            hierarchy,
            targetNode,
            finalChildIdentifiers,
            pendingPageBindings);

        var projections =
            new List<PdfObjectWriteProjection>();

        PdfDictionary targetDictionary =
            GetDictionary(document, targetNodeIdentifier)
                .CreateCopy();

        targetDictionary.Set(
            KidsName,
            new PdfArray(
                finalChildIdentifiers.Select(
                    identifier =>
                        (PdfObject)new PdfIndirectReference(
                            identifier))));

        int newDescendantCount =
            CountDescendantPages(
                hierarchy,
                finalChildIdentifiers,
                pendingPageBindings);

        targetDictionary.Set(
            CountName,
            new PdfInteger(newDescendantCount));

        projections.Add(
            new PdfObjectWriteProjection(
                targetNodeIdentifier,
                targetDictionary));

        foreach (PdfObjectIdentifier childIdentifier
                in finalChildIdentifiers)
        {
            if (TryGetPendingPage(
                    pendingPageBindings,
                    childIdentifier,
                    out PdfPage? pendingPage)
                && pendingPage is not null)
            {
                PdfDictionary newPage =
                    pendingPage.CreateDictionarySnapshot();

                newPage.Set(
                    ParentName,
                    new PdfIndirectReference(
                        targetNodeIdentifier));

                projections.Add(
                    new PdfObjectWriteProjection(
                        childIdentifier,
                        newPage));

                continue;
            }

            if (hierarchy.Leaves.Any(
                    leaf =>
                        leaf.Identifier == childIdentifier))
            {
                PdfDictionary childPage =
                    GetDictionary(
                        document,
                        childIdentifier)
                    .CreateCopy();

                childPage.Set(
                    ParentName,
                    new PdfIndirectReference(
                        targetNodeIdentifier));

                projections.Add(
                    new PdfObjectWriteProjection(
                        childIdentifier,
                        childPage));
            }
        }

        AddAncestorCountProjections(
            document,
            hierarchy,
            targetNode,
            newDescendantCount,
            projections);

        return new PdfObjectWriteProjectionSet(
            projections);
    }

    private static void AddAncestorCountProjections(
        PdfDocument document,
        PdfPageTreeHierarchySnapshot hierarchy,
        PdfPageTreeHierarchySnapshot.Node targetNode,
        int newTargetCount,
        List<PdfObjectWriteProjection> projections)
    {
        int delta =
            newTargetCount -
            targetNode.DescendantPageCount;

        PdfObjectIdentifier? parentIdentifier =
            targetNode.ParentIdentifier;

        while (parentIdentifier.HasValue)
        {
            PdfPageTreeHierarchySnapshot.Node parent =
                hierarchy.GetRequiredNode(
                    parentIdentifier.Value);

            PdfDictionary projected =
                GetDictionary(
                    document,
                    parent.Identifier)
                .CreateCopy();

            projected.Set(
                CountName,
                new PdfInteger(
                    parent.DescendantPageCount + delta));

            projections.Add(
                new PdfObjectWriteProjection(
                    parent.Identifier,
                    projected));

            parentIdentifier =
                parent.ParentIdentifier;
        }
    }

    private static int CountDescendantPages(
        PdfPageTreeHierarchySnapshot hierarchy,
        IReadOnlyList<PdfObjectIdentifier> childIdentifiers,
        IReadOnlyDictionary<PdfPage, PdfObjectIdentifier>
            pendingPageBindings)
    {
        int count = 0;

        foreach (PdfObjectIdentifier identifier
                in childIdentifiers)
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
        PdfPageTreeHierarchySnapshot.Node targetNode,
        IReadOnlyList<PdfObjectIdentifier> finalChildIdentifiers,
        IReadOnlyDictionary<PdfPage, PdfObjectIdentifier>
            pendingPageBindings)
    {
        var seen =
            new HashSet<PdfObjectIdentifier>();

        foreach (PdfObjectIdentifier identifier
                in finalChildIdentifiers)
        {
            if (!seen.Add(identifier))
            {
                throw new InvalidOperationException(
                    $"Child object '{identifier}' occurs more than once in the projected `/Kids` array.");
            }

            if (TryGetPendingPage(
                    pendingPageBindings,
                    identifier,
                    out _))
            {
                continue;
            }

            bool existingLeaf =
                hierarchy.Leaves.Any(
                    leaf =>
                        leaf.Identifier == identifier);
            bool existingNode =
                hierarchy.Nodes.Any(
                    node =>
                        node.Identifier == identifier);

            if (!existingLeaf &&
                !existingNode)
            {
                throw new InvalidOperationException(
                    $"Child object '{identifier}' is not present in the validated hierarchy or pending page bindings.");
            }

            if (existingLeaf)
            {
                PdfPageTreeHierarchySnapshot.Leaf leaf =
                    hierarchy.GetRequiredLeaf(
                        identifier);

                if (leaf.ParentIdentifier !=
                        targetNode.Identifier)
                {
                    throw new InvalidOperationException(
                        $"Existing page '{identifier}' belongs to a different `/Pages` node.");
                }
            }

            if (existingNode)
            {
                PdfPageTreeHierarchySnapshot.Node node =
                    hierarchy.GetRequiredNode(
                        identifier);

                if (node.ParentIdentifier !=
                        targetNode.Identifier)
                {
                    throw new InvalidOperationException(
                        $"Existing page-tree node '{identifier}' belongs to a different parent.");
                }
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
