using BlackFlag.Engine.Document.Objects;

namespace BlackFlag.Engine.Writing;

/// <summary>
/// Creates an incremental plan for common-state changes to already-bound
/// annotations without rewriting the owning page dictionary.
/// </summary>
public static class PdfAnnotationCommonStateIncrementalWritePlanner
{
    public static PdfIncrementalWritePlan Create(
        PdfWritePlan writePlan,
        IPdfOriginalSourceRevision sourceRevision,
        PdfObjectIdentifier pageIdentifier,
        IEnumerable<PdfObjectIdentifier> annotationIdentifiers)
    {
        ArgumentNullException.ThrowIfNull(writePlan);
        ArgumentNullException.ThrowIfNull(sourceRevision);
        ArgumentNullException.ThrowIfNull(annotationIdentifiers);

        if (writePlan.Mode != PdfWriteMode.IncrementalUpdate)
        {
            throw new InvalidOperationException(
                "Annotation common-state incremental planning requires an incremental base write plan.");
        }

        PdfObjectIdentifier[] targets = [.. annotationIdentifiers];

        if (targets.Length == 0)
        {
            throw new InvalidOperationException(
                "Annotation common-state incremental planning requires at least one targeted annotation.");
        }

        PdfObjectWriteProjectionSet projections =
            PdfAnnotationWriteProjector.CreateAnnotationObjects(
                writePlan.Document,
                pageIdentifier,
                targets);

        return PdfProjectedIncrementalWritePlanner.Create(
            writePlan,
            sourceRevision,
            projections);
    }
}
