using BlackFlag.Engine.Document.Model;
using BlackFlag.Engine.Document.Objects;
using BlackFlag.Engine.Editing.Documents;
using BlackFlag.Engine.Editing.Operations;
using BlackFlag.Engine.Editing.Pages;
using BlackFlag.Engine.Editing.Transactions;
using BlackFlag.Engine.Writing;

namespace BlackFlag.Engine.Services.Editing;

/// <summary>
/// Translates one committed page-state transaction containing geometry and/or
/// rotation mutations into one minimal preservation-aware Writing plan.
/// </summary>
public static class PdfPageStateWritingService
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

        PdfObjectIdentifier[] targets =
            ResolveTargets(
                session,
                changeSet,
                writePlan.Document);

        PdfObjectWriteProjectionSet projections =
            PdfPageGeometryWriteProjector.Create(
                writePlan.Document,
                targets);

        return PdfProjectedIncrementalWritePlanner.Create(
            writePlan,
            sourceRevision,
            projections);
    }

    public static PdfWritePlan CreateFullRewritePlan(
        PdfEditingSession session,
        PdfEditChangeSet changeSet,
        PdfWritePlan writePlan)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(changeSet);
        ArgumentNullException.ThrowIfNull(writePlan);

        PdfObjectIdentifier[] targets =
            ResolveTargets(
                session,
                changeSet,
                writePlan.Document);

        PdfObjectWriteProjectionSet projections =
            PdfPageGeometryWriteProjector.Create(
                writePlan.Document,
                targets);

        return PdfProjectedFullRewritePlanner.Create(
            writePlan,
            projections);
    }

    private static PdfObjectIdentifier[] ResolveTargets(
        PdfEditingSession session,
        PdfEditChangeSet changeSet,
        PdfDocument writeDocument)
    {
        if (!ReferenceEquals(
                session.Document.GetSourceDocument(),
                writeDocument))
        {
            throw new InvalidOperationException(
                "The Writing plan does not belong to the editing session document.");
        }

        if (changeSet.IsEmpty)
        {
            throw new InvalidOperationException(
                "Unified page-state Writing requires a non-empty committed change set.");
        }

        var pageIds = new HashSet<Guid>();

        for (int index = 0; index < changeSet.Count; index++)
        {
            switch (changeSet[index])
            {
                case PdfPageGeometryEditOperation geometry:
                    session.EnsureOwnership(geometry);
                    _ = pageIds.Add(geometry.PageId);
                    break;

                case PdfPageEditOperation pageOperation
                    when pageOperation.Kind == PdfPageMutationKind.SetRotation:
                    session.EnsureOwnership(pageOperation);
                    _ = pageIds.Add(pageOperation.PageId);
                    break;

                case PdfPageEditOperation pageOperation:
                    session.EnsureOwnership(pageOperation);
                    throw new NotSupportedException(
                        $"Page mutation '{pageOperation.Kind}' requires structural page-tree Writing.");

                default:
                    throw new NotSupportedException(
                        "Unified page-state Writing accepts only page geometry and SetRotation operations.");
            }
        }

        var targets =
            new List<PdfObjectIdentifier>(pageIds.Count);

        foreach (Guid pageId in pageIds)
        {
            PdfEditablePage page =
                FindPage(
                    session.Document.Pages,
                    pageId);

            targets.Add(
                page.ObjectIdentifier ??
                throw new InvalidOperationException(
                    $"Page '{page.Id}' is not bound to an indirect object."));
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
}
