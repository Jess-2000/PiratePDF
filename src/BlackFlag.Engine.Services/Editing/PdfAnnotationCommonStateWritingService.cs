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
/// Translates committed annotation common-state editing changes into minimal
/// preservation-aware Writing plans.
/// </summary>
public static class PdfAnnotationCommonStateWritingService
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

        IReadOnlyDictionary<PdfObjectIdentifier, PdfObjectIdentifier[]>
            targets = ResolveTargets(
                session,
                changeSet,
                writePlan.Document);

        var projections = new List<PdfObjectWriteProjection>();

        foreach (KeyValuePair<PdfObjectIdentifier, PdfObjectIdentifier[]> target in targets)
        {
            PdfObjectWriteProjectionSet set =
                PdfAnnotationWriteProjector.CreateAnnotationObjects(
                    writePlan.Document,
                    target.Key,
                    target.Value);

            projections.AddRange(set.Projections);
        }

        return PdfProjectedIncrementalWritePlanner.Create(
            writePlan,
            sourceRevision,
            new PdfObjectWriteProjectionSet(projections));
    }

    public static PdfWritePlan CreateFullRewritePlan(
        PdfEditingSession session,
        PdfEditChangeSet changeSet,
        PdfWritePlan writePlan)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(changeSet);
        ArgumentNullException.ThrowIfNull(writePlan);

        IReadOnlyDictionary<PdfObjectIdentifier, PdfObjectIdentifier[]>
            targets = ResolveTargets(
                session,
                changeSet,
                writePlan.Document);

        var projections = new List<PdfObjectWriteProjection>();

        foreach (KeyValuePair<PdfObjectIdentifier, PdfObjectIdentifier[]> target in targets)
        {
            PdfObjectWriteProjectionSet set =
                PdfAnnotationWriteProjector.CreateAnnotationObjects(
                    writePlan.Document,
                    target.Key,
                    target.Value);

            projections.AddRange(set.Projections);
        }

        return PdfProjectedFullRewritePlanner.Create(
            writePlan,
            new PdfObjectWriteProjectionSet(projections));
    }

    private static Dictionary<
        PdfObjectIdentifier,
        PdfObjectIdentifier[]> ResolveTargets(
        PdfEditingSession session,
        PdfEditChangeSet changeSet,
        PdfDocument writeDocument)
    {
        PdfDocument sessionDocument =
            session.Document.GetSourceDocument();

        if (!ReferenceEquals(
                sessionDocument,
                writeDocument))
        {
            throw new InvalidOperationException(
                "The Writing plan does not belong to the editing session document.");
        }

        if (changeSet.IsEmpty)
        {
            throw new InvalidOperationException(
                "Annotation common-state Writing requires a non-empty committed change set.");
        }

        var targets =
            new Dictionary<PdfObjectIdentifier, HashSet<PdfObjectIdentifier>>();

        for (int index = 0; index < changeSet.Count; index++)
        {
            if (changeSet[index] is not PdfAnnotationEditOperation operation)
            {
                throw new NotSupportedException(
                    "Annotation common-state Writing accepts only annotation editing operations.");
            }

            session.EnsureOwnership(operation);

            if (!IsCommonStateMutation(operation.Kind))
            {
                throw new NotSupportedException(
                    $"Annotation mutation '{operation.Kind}' requires structural annotation Writing.");
            }

            PdfEditablePage page =
                FindPage(
                    session.Document.Pages,
                    operation.PageId);
            PdfObjectIdentifier pageIdentifier =
                page.ObjectIdentifier ??
                throw new InvalidOperationException(
                    $"Page '{page.Id}' is not bound to an indirect object.");

            PdfEditableAnnotation annotation =
                FindAnnotation(
                    page,
                    operation.AnnotationId);
            PdfObjectIdentifier annotationIdentifier =
                annotation.ObjectIdentifier ??
                throw new InvalidOperationException(
                    $"Annotation '{annotation.Id}' is not bound to an indirect object.");

            if (!targets.TryGetValue(
                    pageIdentifier,
                    out HashSet<PdfObjectIdentifier>? pageTargets))
            {
                pageTargets = [];
                targets.Add(
                    pageIdentifier,
                    pageTargets);
            }

            _ = pageTargets.Add(annotationIdentifier);
        }

        return targets.ToDictionary(
            pair => pair.Key,
            pair => pair.Value
                .OrderBy(identifier => identifier.ObjectNumber)
                .ThenBy(identifier => identifier.GenerationNumber)
                .ToArray());
    }

    private static bool IsCommonStateMutation(
        PdfAnnotationMutationKind kind)
    {
        return kind switch
        {
            PdfAnnotationMutationKind.SetRectangle => true,
            PdfAnnotationMutationKind.SetContents => true,
            PdfAnnotationMutationKind.ClearContents => true,
            PdfAnnotationMutationKind.SetFlags => true,
            _ => false
        };
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

    private static PdfEditableAnnotation FindAnnotation(
        PdfEditablePage page,
        Guid annotationId)
    {
        for (int index = 0; index < page.Annotations.Count; index++)
        {
            if (page.Annotations[index].Id == annotationId)
            {
                return page.Annotations[index];
            }
        }

        throw new InvalidOperationException(
            $"Edited annotation '{annotationId}' is no longer attached to page '{page.Id}'.");
    }
}
