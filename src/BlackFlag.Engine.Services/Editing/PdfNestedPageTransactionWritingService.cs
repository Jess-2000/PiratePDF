using BlackFlag.Engine.Document.Model;
using BlackFlag.Engine.Document.Objects;
using BlackFlag.Engine.Document.Objects.Primitives;
using BlackFlag.Engine.Editing.Documents;
using BlackFlag.Engine.Editing.Operations;
using BlackFlag.Engine.Editing.Pages;
using BlackFlag.Engine.Editing.Transactions;
using BlackFlag.Engine.Writing;

namespace BlackFlag.Engine.Services.Editing;

/// <summary>
/// Combines nested page-tree structural mutations with page geometry and
/// rotation mutations in one atomic preservation-aware Writing transaction.
/// </summary>
public static class PdfNestedPageTransactionWritingService
{
    private static readonly PdfName[] PageStateNames =
    [
        PdfName.FromLatin1("MediaBox"),
        PdfName.FromLatin1("CropBox"),
        PdfName.FromLatin1("BleedBox"),
        PdfName.FromLatin1("TrimBox"),
        PdfName.FromLatin1("ArtBox"),
        PdfName.FromLatin1("Rotate")
    ];

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
                "Nested page incremental Writing requires an incremental base write plan.");
        }

        CombinedBatch batch =
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
                "Nested page full-rewrite Writing requires a full-rewrite base write plan.");
        }

        CombinedBatch batch =
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

    private static CombinedBatch CreateBatch(
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
                "Nested page Writing requires a non-empty committed change set.");
        }

        TransactionAnalysis analysis =
            Analyze(
                session,
                changeSet);

        PdfNestedPageTreeStructuralWritingService.ProjectionBatch?
            structuralBatch =
                analysis.StructuralChanges.Count == 0
                    ? null
                    : PdfNestedPageTreeStructuralWritingService
                        .CreateProjectionBatch(
                            session,
                            CreateFilteredChangeSet(
                                changeSet,
                                analysis.StructuralChanges),
                            writePlan);

        PdfObjectWriteProjectionSet? stateProjections =
            CreateStateProjections(
                document,
                session.Document.Pages,
                analysis.StatePageIds);

        PdfObjectWriteProjectionSet combined =
            Merge(
                structuralBatch?.Projections,
                stateProjections);

        return new CombinedBatch(
            combined,
            structuralBatch);
    }

    private static TransactionAnalysis Analyze(
        PdfEditingSession session,
        PdfEditChangeSet changeSet)
    {
        var structural =
            new List<IPdfEditOperation>();
        var statePageIds =
            new HashSet<Guid>();

        for (int index = 0; index < changeSet.Count; index++)
        {
            switch (changeSet[index])
            {
                case PdfPageGeometryEditOperation geometry:
                    session.EnsureOwnership(geometry);
                    _ = statePageIds.Add(
                        geometry.PageId);
                    break;

                case PdfPageEditOperation pageOperation:
                    session.EnsureOwnership(pageOperation);

                    switch (pageOperation.Kind)
                    {
                        case PdfPageMutationKind.SetRotation:
                            _ = statePageIds.Add(
                                pageOperation.PageId);
                            break;

                        case PdfPageMutationKind.Insert:
                        case PdfPageMutationKind.Remove:
                        case PdfPageMutationKind.Move:
                        case PdfPageMutationKind.Replace:
                            structural.Add(
                                pageOperation);
                            break;

                        default:
                            throw new NotSupportedException(
                                $"Page mutation '{pageOperation.Kind}' is not supported by nested page Writing.");
                    }

                    break;

                default:
                    throw new NotSupportedException(
                        "Nested page Writing accepts only page and page-geometry editing operations.");
            }
        }

        return new TransactionAnalysis(
            structural.AsReadOnly(),
            statePageIds);
    }

    private static PdfEditChangeSet CreateFilteredChangeSet(
        PdfEditChangeSet source,
        IReadOnlyList<IPdfEditOperation> operations)
    {
        PdfEditChange[] changes =
        [
            .. operations.Select(
                operation =>
                    new PdfEditChange(
                        operation,
                        static () => { },
                        static () => { }))
        ];

        return new PdfEditChangeSet(
            source.TransactionId,
            changes);
    }

    private static PdfObjectWriteProjectionSet? CreateStateProjections(
        PdfDocument document,
        IReadOnlyList<PdfEditablePage> pages,
        IReadOnlySet<Guid> statePageIds)
    {
        PdfObjectIdentifier[] targets =
        [
            .. pages
                .Where(
                    page =>
                        statePageIds.Contains(page.Id) &&
                        page.ObjectIdentifier.HasValue)
                .Select(
                    page =>
                        page.ObjectIdentifier!.Value)
                .Distinct()
                .OrderBy(
                    identifier =>
                        identifier.ObjectNumber)
                .ThenBy(
                    identifier =>
                        identifier.GenerationNumber)
        ];

        return targets.Length == 0
            ? null
            : PdfPageGeometryWriteProjector.Create(
                document,
                targets);
    }

    private static PdfObjectWriteProjectionSet Merge(
        PdfObjectWriteProjectionSet? structural,
        PdfObjectWriteProjectionSet? state)
    {
        if (structural is null &&
            state is null)
        {
            throw new InvalidOperationException(
                "The committed nested page transaction produced no writable final-state projection.");
        }

        if (structural is null)
        {
            return state!;
        }

        if (state is null)
        {
            return structural;
        }

        var projections =
            structural.Projections.ToDictionary(
                projection =>
                    projection.Identifier,
                projection =>
                    projection);

        foreach (PdfObjectWriteProjection stateProjection
                in state.Projections)
        {
            if (!projections.TryGetValue(
                    stateProjection.Identifier,
                    out PdfObjectWriteProjection? structuralProjection))
            {
                projections.Add(
                    stateProjection.Identifier,
                    stateProjection);
                continue;
            }

            PdfDictionary structuralDictionary =
                structuralProjection.Value as PdfDictionary ??
                throw new InvalidOperationException(
                    $"Structural projection '{structuralProjection.Identifier}' must be a dictionary when page state overlaps it.");

            PdfDictionary stateDictionary =
                stateProjection.Value as PdfDictionary ??
                throw new InvalidOperationException(
                    $"Page-state projection '{stateProjection.Identifier}' must be a dictionary.");

            PdfDictionary merged =
                structuralDictionary.CreateCopy();

            for (int index = 0;
                index < PageStateNames.Length;
                index++)
            {
                PdfName name =
                    PageStateNames[index];

                if (stateDictionary.TryGetValue(
                        name,
                        out PdfObject value))
                {
                    merged.Set(
                        name,
                        value);
                }
                else
                {
                    _ = merged.Remove(
                        name);
                }
            }

            projections[stateProjection.Identifier] =
                new PdfObjectWriteProjection(
                    stateProjection.Identifier,
                    merged);
        }

        return new PdfObjectWriteProjectionSet(
            projections.Values);
    }

    private sealed record TransactionAnalysis(
        IReadOnlyList<IPdfEditOperation> StructuralChanges,
        IReadOnlySet<Guid> StatePageIds);

    private sealed class CombinedBatch
    {
        private readonly
            PdfNestedPageTreeStructuralWritingService.ProjectionBatch?
                structuralBatch;

        public CombinedBatch(
            PdfObjectWriteProjectionSet projections,
            PdfNestedPageTreeStructuralWritingService.ProjectionBatch?
                structuralBatch)
        {
            Projections = projections;
            this.structuralBatch = structuralBatch;
        }

        public PdfObjectWriteProjectionSet Projections { get; }

        public void CommitBindings()
        {
            structuralBatch?.CommitBindings();
        }
    }
}
