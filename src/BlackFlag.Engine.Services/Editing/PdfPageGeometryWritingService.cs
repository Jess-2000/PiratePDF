using BlackFlag.Engine.Document.Model;
using BlackFlag.Engine.Document.Objects;
using BlackFlag.Engine.Editing.Documents;
using BlackFlag.Engine.Editing.Operations;
using BlackFlag.Engine.Editing.Pages;
using BlackFlag.Engine.Editing.Transactions;
using BlackFlag.Engine.Writing;

namespace BlackFlag.Engine.Services.Editing;

/// <summary>
/// Translates committed page-geometry editing operations into minimal
/// preservation-aware Writing plans.
/// </summary>
public static class PdfPageGeometryWritingService
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
        PdfObjectWriteProjectionSet projections =
            PdfPageGeometryWriteProjector.Create(writePlan.Document, targets);

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
            ResolveTargets(session, changeSet, writePlan.Document);
        PdfObjectWriteProjectionSet projections =
            PdfPageGeometryWriteProjector.Create(writePlan.Document, targets);

        return PdfProjectedFullRewritePlanner.Create(
            writePlan,
            projections);
    }

    private static PdfObjectIdentifier[] ResolveTargets(
        PdfEditingSession session,
        PdfEditChangeSet changeSet,
        PdfDocument writeDocument)
    {
        PdfDocument sessionDocument =
            session.Document.GetSourceDocument();

        if (!ReferenceEquals(sessionDocument, writeDocument))
        {
            throw new InvalidOperationException(
                "The Writing plan does not belong to the editing session document.");
        }

        if (changeSet.IsEmpty)
        {
            throw new InvalidOperationException(
                "Page-geometry Writing requires a non-empty committed change set.");
        }

        var pageIds = new HashSet<Guid>();

        for (int index = 0; index < changeSet.Count; index++)
        {
            if (changeSet[index] is not PdfPageGeometryEditOperation operation)
            {
                throw new NotSupportedException(
                    "Page-geometry Writing accepts only page-geometry editing operations.");
            }

            session.EnsureOwnership(operation);
            _ = pageIds.Add(operation.PageId);
        }

        var identifiers =
            new List<PdfObjectIdentifier>(pageIds.Count);

        foreach (Guid pageId in pageIds)
        {
            PdfEditablePage page =
                FindPage(session.Document.Pages, pageId);
            PdfObjectIdentifier identifier =
                page.ObjectIdentifier ??
                throw new InvalidOperationException(
                    $"Page '{page.Id}' is not bound to an indirect object.");

            identifiers.Add(identifier);
        }

        return
        [
            .. identifiers
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
            $"Geometry-edited page '{pageId}' is no longer attached to the session document.");
    }
}
