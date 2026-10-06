using BlackFlag.Engine.Document.Annotations;
using BlackFlag.Engine.Document.Model;
using BlackFlag.Engine.Document.Objects;
using BlackFlag.Engine.Editing.Annotations;
using BlackFlag.Engine.Editing.Documents;
using BlackFlag.Engine.Editing.Operations;
using BlackFlag.Engine.Editing.Pages;
using BlackFlag.Engine.Editing.Transactions;
using BlackFlag.Engine.Writing;

namespace BlackFlag.Engine.Services.Editing;

/// <summary>
/// Translates one committed annotation transaction containing structural
/// and/or common-state mutations into one minimal Writing transaction.
/// </summary>
public static class PdfAnnotationTransactionWritingService
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
                "Unified annotation incremental Writing requires an incremental base write plan.");
        }

        AnnotationProjectionBatch batch =
            CreateBatch(
                session,
                changeSet,
                writePlan);

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
                "Unified annotation full-rewrite Writing requires a full-rewrite base write plan.");
        }

        AnnotationProjectionBatch batch =
            CreateBatch(
                session,
                changeSet,
                writePlan);

        PdfWritePlan result =
            PdfProjectedFullRewritePlanner.Create(
                writePlan,
                batch.Projections);

        batch.CommitBindings();

        return result;
    }

    private static AnnotationProjectionBatch CreateBatch(
        PdfEditingSession session,
        PdfEditChangeSet changeSet,
        PdfWritePlan writePlan)
    {
        PdfDocument document =
            session.Document.GetSourceDocument();

        if (!ReferenceEquals(
                document,
                writePlan.Document))
        {
            throw new InvalidOperationException(
                "The Writing plan does not belong to the editing session document.");
        }

        if (changeSet.IsEmpty)
        {
            throw new InvalidOperationException(
                "Unified annotation Writing requires a non-empty committed change set.");
        }

        Dictionary<Guid, PageMutationState> pageStates =
            AnalyzeChangeSet(
                session,
                changeSet);

        PdfWriteObjectAllocator allocator =
            writePlan.CreateObjectAllocator();
        var projections =
            new List<PdfObjectWriteProjection>();
        var bindings =
            new List<PendingBinding>();

        foreach (KeyValuePair<Guid, PageMutationState> pageState
                in pageStates)
        {
            PdfEditablePage page =
                FindPage(
                    session.Document.Pages,
                    pageState.Key);
            PdfObjectIdentifier pageIdentifier =
                page.ObjectIdentifier ??
                throw new InvalidOperationException(
                    $"Page '{page.Id}' is not bound to an indirect object.");

            Dictionary<PdfAnnotation, PdfObjectIdentifier> pending =
                CreatePendingBindings(
                    page,
                    pageState.Value.AddedAnnotationIds,
                    allocator,
                    bindings);

            PdfObjectIdentifier[] commonTargets =
                ResolveBoundCommonTargets(
                    page,
                    pageState.Value.CommonStateAnnotationIds);

            PdfObjectWriteProjectionSet pageProjections =
                PdfAnnotationWriteProjector.Create(
                    document,
                    pageIdentifier,
                    pending,
                    commonTargets,
                    pageState.Value.StructureChanged);

            projections.AddRange(
                pageProjections.Projections);
        }

        return new AnnotationProjectionBatch(
            new PdfObjectWriteProjectionSet(projections),
            bindings.AsReadOnly());
    }

    private static Dictionary<Guid, PageMutationState> AnalyzeChangeSet(
        PdfEditingSession session,
        PdfEditChangeSet changeSet)
    {
        var pageStates =
            new Dictionary<Guid, PageMutationState>();

        for (int index = 0; index < changeSet.Count; index++)
        {
            if (changeSet[index] is not PdfAnnotationEditOperation operation)
            {
                throw new NotSupportedException(
                    "Unified annotation Writing accepts only annotation editing operations.");
            }

            session.EnsureOwnership(operation);

            if (!pageStates.TryGetValue(
                    operation.PageId,
                    out PageMutationState? state))
            {
                state = new PageMutationState();
                pageStates.Add(
                    operation.PageId,
                    state);
            }

            switch (operation.Kind)
            {
                case PdfAnnotationMutationKind.Add:
                    state.StructureChanged = true;
                    _ = state.AddedAnnotationIds.Add(
                        operation.AnnotationId);
                    break;

                case PdfAnnotationMutationKind.Remove:
                    state.StructureChanged = true;
                    _ = state.RemovedAnnotationIds.Add(
                        operation.AnnotationId);
                    break;

                case PdfAnnotationMutationKind.SetRectangle:
                case PdfAnnotationMutationKind.SetContents:
                case PdfAnnotationMutationKind.ClearContents:
                case PdfAnnotationMutationKind.SetFlags:
                    _ = state.CommonStateAnnotationIds.Add(
                        operation.AnnotationId);
                    break;

                default:
                    throw new NotSupportedException(
                        $"Annotation mutation '{operation.Kind}' is not supported by unified annotation Writing.");
            }
        }

        return pageStates;
    }

    private static Dictionary<PdfAnnotation, PdfObjectIdentifier>
        CreatePendingBindings(
            PdfEditablePage page,
            HashSet<Guid> addedAnnotationIds,
            PdfWriteObjectAllocator allocator,
            List<PendingBinding> bindings)
    {
        var pending =
            new Dictionary<PdfAnnotation, PdfObjectIdentifier>();

        for (int index = 0; index < page.Annotations.Count; index++)
        {
            PdfEditableAnnotation annotation =
                page.Annotations[index];

            if (annotation.ObjectIdentifier.HasValue)
            {
                continue;
            }

            if (!addedAnnotationIds.Contains(annotation.Id))
            {
                throw new InvalidOperationException(
                    $"Unbound annotation '{annotation.Id}' on page '{page.Id}' is not represented by the committed annotation transaction.");
            }

            PdfObjectIdentifier identifier =
                allocator.ReserveNext();

            pending.Add(
                annotation.GetSourceAnnotation(),
                identifier);
            bindings.Add(
                new PendingBinding(
                    annotation,
                    identifier));
        }

        return pending;
    }

    private static PdfObjectIdentifier[] ResolveBoundCommonTargets(
        PdfEditablePage page,
        HashSet<Guid> commonStateAnnotationIds)
    {
        var targets =
            new HashSet<PdfObjectIdentifier>();

        for (int index = 0; index < page.Annotations.Count; index++)
        {
            PdfEditableAnnotation annotation =
                page.Annotations[index];

            if (!commonStateAnnotationIds.Contains(annotation.Id) ||
                !annotation.ObjectIdentifier.HasValue)
            {
                continue;
            }

            _ = targets.Add(
                annotation.ObjectIdentifier.Value);
        }

        return
        [
            .. targets
                .OrderBy(identifier => identifier.ObjectNumber)
                .ThenBy(identifier => identifier.GenerationNumber)
        ];
    }

    private static PdfEditablePage FindPage(
        PdfEditablePageCollection pages,
        Guid pageId)
    {
        for (int index = 0; index < pages.Count; index++)
        {
            if (pages[index].Id == pageId)
            {
                return pages[index];
            }
        }

        throw new InvalidOperationException(
            $"Edited page '{pageId}' is no longer attached to the session document.");
    }

    private sealed class PageMutationState
    {
        public bool StructureChanged { get; set; }
        public HashSet<Guid> AddedAnnotationIds { get; } = [];
        public HashSet<Guid> RemovedAnnotationIds { get; } = [];
        public HashSet<Guid> CommonStateAnnotationIds { get; } = [];
    }

    private sealed class AnnotationProjectionBatch
    {
        private readonly IReadOnlyList<PendingBinding> bindings;

        public AnnotationProjectionBatch(
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
                PendingBinding binding =
                    bindings[index];

                if (binding.Annotation.ObjectIdentifier.HasValue)
                {
                    throw new InvalidOperationException(
                        $"Annotation '{binding.Annotation.Id}' was bound unexpectedly while unified Writing was in progress.");
                }
            }

            for (int index = 0; index < bindings.Count; index++)
            {
                PendingBinding binding =
                    bindings[index];

                binding.Annotation.BindObjectIdentifier(
                    binding.Identifier);
            }
        }
    }

    private sealed record PendingBinding(
        PdfEditableAnnotation Annotation,
        PdfObjectIdentifier Identifier);
}
