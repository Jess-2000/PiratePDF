using BlackFlag.Engine.Document.Model;
using BlackFlag.Engine.Document.Objects;
using BlackFlag.Engine.Editing.Documents;
using BlackFlag.Engine.Editing.Operations;
using BlackFlag.Engine.Editing.Pages;
using BlackFlag.Engine.Editing.Transactions;
using BlackFlag.Engine.Writing;

namespace BlackFlag.Engine.Services.Editing;

/// <summary>
/// Translates committed page-rotation editing operations into minimal
/// preservation-aware Writing plans.
/// </summary>
public static class PdfPageRotationWritingService
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
            ResolveTargets(session, changeSet, writePlan.Document);

        return PdfProjectedIncrementalWritePlanner.Create(
            writePlan,
            sourceRevision,
            PdfPageGeometryWriteProjector.Create(
                writePlan.Document,
                targets));
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
            ResolveTargets(session, changeSet, writePlan.Document);

        return PdfProjectedFullRewritePlanner.Create(
            writePlan,
            PdfPageGeometryWriteProjector.Create(
                writePlan.Document,
                targets));
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
                "Page-rotation Writing requires a non-empty committed change set.");
        }

        var pageIds = new HashSet<Guid>();

        for (int index = 0; index < changeSet.Count; index++)
        {
            if (changeSet[index] is not PdfPageEditOperation operation ||
                operation.Kind != PdfPageMutationKind.SetRotation)
            {
                throw new NotSupportedException(
                    "Page-rotation Writing accepts only SetRotation page operations.");
            }

            session.EnsureOwnership(operation);
            _ = pageIds.Add(operation.PageId);
        }

        var targets =
            new List<PdfObjectIdentifier>(pageIds.Count);

        foreach (Guid pageId in pageIds)
        {
            PdfEditablePage page =
                FindPage(session.Document.Pages, pageId);

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
            $"Rotation-edited page '{pageId}' is no longer attached to the session document.");
    }
}
