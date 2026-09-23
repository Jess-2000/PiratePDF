using BlackFlag.Engine.Document.Model;
using BlackFlag.Engine.Document.Objects;
using BlackFlag.Engine.Document.Pages;
using BlackFlag.Engine.Editing.Documents;
using BlackFlag.Engine.Editing.Pages;
using BlackFlag.Engine.Editing.Transactions;
using BlackFlag.Engine.Writing;

namespace BlackFlag.Engine.Services.Editing;

/// <summary>
/// Translates committed structural page operations onto an existing nested
/// page-tree topology using the AE/AF projection primitives.
/// </summary>
public static class PdfNestedPageTreeStructuralWritingService
{
    public static PdfIncrementalWritePlan CreateIncrementalPlan(
        PdfEditingSession session,
        PdfEditChangeSet changeSet,
        PdfWritePlan writePlan,
        IPdfOriginalSourceRevision sourceRevision)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(changeSet);
        ArgumentNullException.ThrowIfNull(writePlan);
        ArgumentNullException.ThrowIfNull(sourceRevision);

        if (writePlan.Mode != PdfWriteMode.IncrementalUpdate)
        {
            throw new InvalidOperationException(
                "Nested structural page incremental Writing requires an incremental base write plan.");
        }

        ProjectionBatch batch =
            CreateProjectionBatch(session, changeSet, writePlan);

        PdfIncrementalWritePlan result =
            PdfProjectedIncrementalWritePlanner.Create(
                writePlan,
                sourceRevision,
                batch.Projections);

        batch.CommitBindings();
        return result;
    }

    public static PdfWritePlan CreateFullRewritePlan(
        PdfEditingSession session,
        PdfEditChangeSet changeSet,
        PdfWritePlan writePlan)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(changeSet);
        ArgumentNullException.ThrowIfNull(writePlan);

        if (writePlan.Mode != PdfWriteMode.FullRewrite)
        {
            throw new InvalidOperationException(
                "Nested structural page full-rewrite Writing requires a full-rewrite base write plan.");
        }

        ProjectionBatch batch =
            CreateProjectionBatch(session, changeSet, writePlan);

        PdfWritePlan result =
            PdfProjectedFullRewritePlanner.Create(
                writePlan,
                batch.Projections);

        batch.CommitBindings();
        return result;
    }

    internal static ProjectionBatch CreateProjectionBatch(
        PdfEditingSession session,
        PdfEditChangeSet changeSet,
        PdfWritePlan writePlan)
    {
        PdfDocument document =
            session.Document.GetSourceDocument();

        if (!ReferenceEquals(document, writePlan.Document))
        {
            throw new InvalidOperationException(
                "The Writing plan does not belong to the editing session document.");
        }

        if (changeSet.IsEmpty)
        {
            throw new InvalidOperationException(
                "Nested structural page Writing requires a non-empty committed change set.");
        }

        PdfPageTreeHierarchySnapshot hierarchy =
            PdfPageTreeHierarchySnapshot.CreateTopology(document);

        List<PdfNestedPageTreePlacement> placements =
            PdfNestedPageTreeOperationReplay.CreateInitialPlacements(
                hierarchy);

        PdfNestedPageTreeOperationReplay.Replay(
            session,
            changeSet,
            placements);

        PdfWriteObjectAllocator allocator =
            writePlan.CreateObjectAllocator();
        var pending =
            new Dictionary<PdfPage, PdfObjectIdentifier>();
        var bindings =
            new List<PendingBinding>();

        BindPendingFinalPages(
            session.Document.Pages,
            placements,
            allocator,
            pending,
            bindings);

        Dictionary<PdfObjectIdentifier, PdfObjectIdentifier[]> finalByParent =
            BuildFinalChildrenByParent(
                hierarchy,
                placements,
                pending);

        _ = DetermineAffectedParents(
            hierarchy,
            finalByParent);

        PdfObjectWriteProjectionSet projections =
            PdfNestedPageTreeTopologyProjector.Create(
                document,
                hierarchy,
                finalByParent,
                pending);

        return new ProjectionBatch(
            projections,
            bindings.AsReadOnly());
    }

    private static void BindPendingFinalPages(
        IReadOnlyList<PdfEditablePage> pages,
        IReadOnlyList<PdfNestedPageTreePlacement> placements,
        PdfWriteObjectAllocator allocator,
        Dictionary<PdfPage, PdfObjectIdentifier> pending,
        List<PendingBinding> bindings)
    {
        foreach (PdfNestedPageTreePlacement placement in placements)
        {
            if (placement.ObjectIdentifier.HasValue)
            {
                continue;
            }

            if (!placement.PageId.HasValue)
            {
                throw new InvalidOperationException(
                    "A pending nested page-tree placement does not identify a page.");
            }

            PdfEditablePage page =
                pages.Single(
                    candidate => candidate.Id == placement.PageId.Value);

            PdfObjectIdentifier identifier = allocator.ReserveNext();
            pending.Add(page.GetSourcePage(), identifier);
            bindings.Add(new PendingBinding(page, identifier));
        }
    }

    private static Dictionary<PdfObjectIdentifier, PdfObjectIdentifier[]>
        BuildFinalChildrenByParent(
        PdfPageTreeHierarchySnapshot hierarchy,
        IReadOnlyList<PdfNestedPageTreePlacement> placements,
        IReadOnlyDictionary<PdfPage, PdfObjectIdentifier> pending)
    {
        var pendingByPageId =
            pending.ToDictionary(
                pair => pair.Key.Id,
                pair => pair.Value);

        var result =
            hierarchy.Nodes.ToDictionary(
                node => node.Identifier,
                node => node.ChildIdentifiers.ToList());

        foreach (PdfPageTreeHierarchySnapshot.Node node
                in hierarchy.Nodes)
        {
            List<PdfObjectIdentifier> existingPageChildren =
                [.. node.ChildIdentifiers
                    .Where(
                        identifier =>
                            hierarchy.Leaves.Any(
                                leaf =>
                                    leaf.Identifier == identifier))];

            if (existingPageChildren.Count == 0)
            {
                continue;
            }

            List<PdfObjectIdentifier> finalLeaves =
                [.. placements
                    .Where(
                        placement =>
                            placement.ParentIdentifier ==
                            node.Identifier)
                    .Select(
                        placement =>
                            placement.ObjectIdentifier ??
                            pendingByPageId[
                                placement.PageId!.Value])];

            List<PdfObjectIdentifier> children =
                result[node.Identifier];

            int firstLeafIndex =
                children.FindIndex(
                    existingPageChildren.Contains);

            if (firstLeafIndex < 0)
            {
                throw new InvalidOperationException(
                    $"Leaf-bearing node '{node.Identifier}' no longer contains its expected page-leaf run.");
            }

            children.RemoveAll(
                existingPageChildren.Contains);

            children.InsertRange(
                firstLeafIndex,
                finalLeaves);
        }

        return result.ToDictionary(
            pair => pair.Key,
            pair => pair.Value.ToArray());
    }

    private static PdfObjectIdentifier[] DetermineAffectedParents(
        PdfPageTreeHierarchySnapshot hierarchy,
        Dictionary<PdfObjectIdentifier, PdfObjectIdentifier[]>
            finalByParent)
    {
        var affected =
            new List<PdfObjectIdentifier>();

        foreach (PdfPageTreeHierarchySnapshot.Node node
                in hierarchy.Nodes)
        {
            if (!node.ChildIdentifiers.SequenceEqual(
                    finalByParent[node.Identifier]))
            {
                affected.Add(
                    node.Identifier);
            }
        }

        if (affected.Count == 0)
        {
            throw new InvalidOperationException(
                "The committed nested structural transaction produced no page-tree change.");
        }

        return
        [
            .. affected
                .OrderBy(identifier => identifier.ObjectNumber)
                .ThenBy(identifier => identifier.GenerationNumber)
        ];
    }

    internal sealed class ProjectionBatch
    {
        private readonly IReadOnlyList<PendingBinding> bindings;

        internal ProjectionBatch(
            PdfObjectWriteProjectionSet projections,
            IReadOnlyList<PendingBinding> bindings)
        {
            Projections = projections;
            this.bindings = bindings;
        }

        public PdfObjectWriteProjectionSet Projections { get; }

        public void CommitBindings()
        {
            for (int index = 0; index < bindings.Count; index++)
            {
                if (bindings[index].Page.ObjectIdentifier.HasValue)
                {
                    throw new InvalidOperationException(
                        $"Page '{bindings[index].Page.Id}' was bound unexpectedly while nested structural Writing was in progress.");
                }
            }

            for (int index = 0; index < bindings.Count; index++)
            {
                bindings[index].Page.BindObjectIdentifier(
                    bindings[index].Identifier);
            }
        }
    }

    internal sealed record PendingBinding(
        PdfEditablePage Page,
        PdfObjectIdentifier Identifier);
}
