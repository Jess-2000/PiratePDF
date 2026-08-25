using BlackFlag.Engine.Document.Model;
using BlackFlag.Engine.Document.Objects;
using BlackFlag.Engine.Document.Pages;
using BlackFlag.Engine.Editing.Documents;
using BlackFlag.Engine.Editing.Operations;
using BlackFlag.Engine.Editing.Pages;
using BlackFlag.Engine.Editing.Transactions;
using BlackFlag.Engine.Writing;

namespace BlackFlag.Engine.Services.Editing;

public static class PdfPageStructuralWritingService
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
                "Structural page incremental Writing requires an incremental base write plan.");
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
                "Structural page full-rewrite Writing requires a full-rewrite base write plan.");
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
                "Structural page Writing requires a non-empty committed change set.");
        }

        ValidateChangeSet(session, changeSet);

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
            PdfPage page =
                editablePage.GetSourcePage();

            pending.Add(page, identifier);
            bindings.Add(
                new PendingBinding(
                    editablePage,
                    identifier));
            projections.Add(
                new PdfObjectWriteProjection(
                    identifier,
                    editablePage.CreateDictionarySnapshot()));
        }

        PdfObjectWriteProjectionSet tree =
            PdfPageTreeWriteProjector.Create(
                document,
                pending);

        projections.AddRange(tree.Projections);

        return new PageProjectionBatch(
            new PdfObjectWriteProjectionSet(projections),
            bindings.AsReadOnly());
    }

    private static void ValidateChangeSet(
        PdfEditingSession session,
        PdfEditChangeSet changeSet)
    {
        for (int index = 0; index < changeSet.Count; index++)
        {
            if (changeSet[index] is not PdfPageEditOperation operation)
            {
                throw new NotSupportedException(
                    "Structural page Writing accepts only page editing operations.");
            }

            session.EnsureOwnership(operation);

            switch (operation.Kind)
            {
                case PdfPageMutationKind.Insert:
                case PdfPageMutationKind.Remove:
                case PdfPageMutationKind.Move:
                case PdfPageMutationKind.Replace:
                    break;

                case PdfPageMutationKind.SetRotation:
                    throw new NotSupportedException(
                        "Page rotation requires page-state Writing.");

                default:
                    throw new NotSupportedException(
                        $"Page mutation '{operation.Kind}' is not supported by structural page Writing.");
            }
        }
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
                PendingBinding binding = bindings[index];

                if (binding.Page.ObjectIdentifier.HasValue)
                {
                    throw new InvalidOperationException(
                        $"Page '{binding.Page.Id}' was bound unexpectedly while structural Writing was in progress.");
                }
            }

            for (int index = 0; index < bindings.Count; index++)
            {
                PendingBinding binding = bindings[index];
                binding.Page.BindObjectIdentifier(binding.Identifier);
            }
        }
    }

    private sealed record PendingBinding(
        PdfEditablePage Page,
        PdfObjectIdentifier Identifier);
}
