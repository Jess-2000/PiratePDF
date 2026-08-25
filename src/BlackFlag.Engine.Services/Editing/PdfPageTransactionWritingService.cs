using BlackFlag.Engine.Document.Model;
using BlackFlag.Engine.Document.Objects;
using BlackFlag.Engine.Document.Pages;
using BlackFlag.Engine.Editing.Documents;
using BlackFlag.Engine.Editing.Operations;
using BlackFlag.Engine.Editing.Pages;
using BlackFlag.Engine.Editing.Transactions;
using BlackFlag.Engine.Writing;

namespace BlackFlag.Engine.Services.Editing;

/// <summary>
/// Translates one committed page transaction containing structural and/or
/// non-structural page-state edits into one atomic Writing transaction.
/// </summary>
public static class PdfPageTransactionWritingService
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
                "Unified page incremental Writing requires an incremental base write plan.");
        }

        PageProjectionBatch batch =
            CreateBatch(session, changeSet, writePlan);

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
                "Unified page full-rewrite Writing requires a full-rewrite base write plan.");
        }

        PageProjectionBatch batch =
            CreateBatch(session, changeSet, writePlan);

        PdfWritePlan result =
            PdfProjectedFullRewritePlanner.Create(
                writePlan,
                batch.Projections);

        batch.CommitBindings();
        return result;
    }

    private static PageProjectionBatch CreateBatch(
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
                "Unified page Writing requires a non-empty committed change set.");
        }

        PageTransactionState state =
            AnalyzeChangeSet(session, changeSet);

        PdfWriteObjectAllocator allocator =
            writePlan.CreateObjectAllocator();

        var pending =
            new Dictionary<PdfPage, PdfObjectIdentifier>();
        var bindings =
            new List<PendingBinding>();
        var projections =
            new List<PdfObjectWriteProjection>();

        for (int index = 0; index < session.Document.Pages.Count; index++)
        {
            PdfEditablePage editablePage =
                session.Document.Pages[index];

            if (editablePage.ObjectIdentifier.HasValue)
            {
                continue;
            }

            if (editablePage.Origin != PdfEditablePageOrigin.SessionCreated)
            {
                throw new InvalidOperationException(
                    $"Unbound page '{editablePage.Id}' is not a session-created page.");
            }

            PdfObjectIdentifier identifier =
                allocator.ReserveNext();
            PdfPage source =
                editablePage.GetSourcePage();

            pending.Add(source, identifier);
            bindings.Add(
                new PendingBinding(
                    editablePage,
                    identifier));

            PdfObject projectedValue =
                editablePage.CreateDictionarySnapshot();

            projections.Add(
                new PdfObjectWriteProjection(
                    identifier,
                    projectedValue));
        }

        if (state.StructureChanged)
        {
            PdfObjectWriteProjectionSet tree =
                PdfPageTreeWriteProjector.Create(
                    document,
                    pending);

            projections.AddRange(tree.Projections);
        }

        PdfObjectIdentifier[] boundStateTargets =
            ResolveBoundStateTargets(
                session.Document.Pages,
                state.PageStateIds);

        if (boundStateTargets.Length != 0)
        {
            PdfObjectWriteProjectionSet pageState =
                PdfPageGeometryWriteProjector.Create(
                    document,
                    boundStateTargets);

            projections.AddRange(
                pageState.Projections);
        }

        return new PageProjectionBatch(
            new PdfObjectWriteProjectionSet(projections),
            bindings.AsReadOnly());
    }

    private static PageTransactionState AnalyzeChangeSet(
        PdfEditingSession session,
        PdfEditChangeSet changeSet)
    {
        var state =
            new PageTransactionState();

        for (int index = 0; index < changeSet.Count; index++)
        {
            switch (changeSet[index])
            {
                case PdfPageGeometryEditOperation geometry:
                    session.EnsureOwnership(geometry);
                    _ = state.PageStateIds.Add(
                        geometry.PageId);
                    break;

                case PdfPageEditOperation pageOperation:
                    session.EnsureOwnership(pageOperation);

                    switch (pageOperation.Kind)
                    {
                        case PdfPageMutationKind.SetRotation:
                            _ = state.PageStateIds.Add(
                                pageOperation.PageId);
                            break;

                        case PdfPageMutationKind.Insert:
                        case PdfPageMutationKind.Remove:
                        case PdfPageMutationKind.Move:
                        case PdfPageMutationKind.Replace:
                            state.StructureChanged = true;
                            break;

                        default:
                            throw new NotSupportedException(
                                $"Page mutation '{pageOperation.Kind}' is not supported by unified page Writing.");
                    }

                    break;

                default:
                    throw new NotSupportedException(
                        "Unified page Writing accepts only page editing and page-geometry operations.");
            }
        }

        return state;
    }

    private static PdfObjectIdentifier[] ResolveBoundStateTargets(
        PdfEditablePageCollection pages,
        HashSet<Guid> pageIds)
    {
        var targets =
            new HashSet<PdfObjectIdentifier>();

        for (int index = 0; index < pages.Count; index++)
        {
            PdfEditablePage page =
                pages[index];

            if (!pageIds.Contains(page.Id) ||
                !page.ObjectIdentifier.HasValue)
            {
                continue;
            }

            _ = targets.Add(
                page.ObjectIdentifier.Value);
        }

        return
        [
            .. targets
                .OrderBy(identifier => identifier.ObjectNumber)
                .ThenBy(identifier => identifier.GenerationNumber)
        ];
    }

    private sealed class PageTransactionState
    {
        public bool StructureChanged { get; set; }

        public HashSet<Guid> PageStateIds { get; } = [];
    }

    private sealed class PageProjectionBatch
    {
        private readonly IReadOnlyList<PendingBinding> bindings;

        public PageProjectionBatch(
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

                if (binding.Page.ObjectIdentifier.HasValue)
                {
                    throw new InvalidOperationException(
                        $"Page '{binding.Page.Id}' was bound unexpectedly while unified page Writing was in progress.");
                }
            }

            for (int index = 0; index < bindings.Count; index++)
            {
                PendingBinding binding =
                    bindings[index];

                binding.Page.BindObjectIdentifier(
                    binding.Identifier);
            }
        }
    }

    private sealed record PendingBinding(
        PdfEditablePage Page,
        PdfObjectIdentifier Identifier);
}
