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
/// Translates committed annotation add/remove transactions into one
/// reservation-aware preservation transaction.
/// </summary>
public static class PdfAnnotationStructuralWritingService
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
                "Structural annotation incremental Writing requires an incremental base write plan.");
        }

        StructuralProjectionBatch batch =
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
                "Structural annotation full-rewrite Writing requires a full-rewrite base write plan.");
        }

        StructuralProjectionBatch batch =
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

    private static StructuralProjectionBatch CreateBatch(
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
                "Structural annotation Writing requires a non-empty committed change set.");
        }

        Dictionary<Guid, HashSet<Guid>> addedAnnotationIds =
            ValidateAndCollectAffectedPages(
                session,
                changeSet);

        PdfWriteObjectAllocator allocator =
            writePlan.CreateObjectAllocator();
        var projections =
            new List<PdfObjectWriteProjection>();
        var bindings =
            new List<PendingBinding>();

        foreach (KeyValuePair<Guid, HashSet<Guid>> affectedPage
                in addedAnnotationIds)
        {
            PdfEditablePage page =
                FindPage(
                    session.Document.Pages,
                    affectedPage.Key);
            PdfObjectIdentifier pageIdentifier =
                page.ObjectIdentifier ??
                throw new InvalidOperationException(
                    $"Page '{page.Id}' is not bound to an indirect object.");

            Dictionary<PdfAnnotation, PdfObjectIdentifier> pending =
                CreatePendingBindings(
                    page,
                    affectedPage.Value,
                    allocator,
                    bindings);

            PdfObjectWriteProjectionSet pageProjections =
                PdfAnnotationWriteProjector.Create(
                    document,
                    pageIdentifier,
                    pending);

            projections.AddRange(
                pageProjections.Projections);
        }

        return new StructuralProjectionBatch(
            new PdfObjectWriteProjectionSet(projections),
            bindings.AsReadOnly());
    }

    private static Dictionary<Guid, HashSet<Guid>>
        ValidateAndCollectAffectedPages(
            PdfEditingSession session,
            PdfEditChangeSet changeSet)
    {
        var affectedPages =
            new Dictionary<Guid, HashSet<Guid>>();

        for (int index = 0; index < changeSet.Count; index++)
        {
            if (changeSet[index] is not PdfAnnotationEditOperation operation)
            {
                throw new NotSupportedException(
                    "Structural annotation Writing accepts only annotation editing operations.");
            }

            session.EnsureOwnership(operation);

            if (operation.Kind is not
                (PdfAnnotationMutationKind.Add or
                PdfAnnotationMutationKind.Remove))
            {
                throw new NotSupportedException(
                    $"Annotation mutation '{operation.Kind}' requires common-state annotation Writing.");
            }

            if (!affectedPages.TryGetValue(
                    operation.PageId,
                    out HashSet<Guid>? additions))
            {
                additions = [];
                affectedPages.Add(
                    operation.PageId,
                    additions);
            }

            if (operation.Kind == PdfAnnotationMutationKind.Add)
            {
                _ = additions.Add(
                    operation.AnnotationId);
            }
        }

        return affectedPages;
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
                    $"Unbound annotation '{annotation.Id}' on page '{page.Id}' is not represented by the committed structural change set.");
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
            $"Structurally edited page '{pageId}' is no longer attached to the session document.");
    }

    private sealed class StructuralProjectionBatch
    {
        private readonly IReadOnlyList<PendingBinding> bindings;

        public StructuralProjectionBatch(
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
                        $"Annotation '{binding.Annotation.Id}' was bound unexpectedly while structural Writing was in progress.");
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
