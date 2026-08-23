using BlackFlag.Engine.Document.Objects;

namespace BlackFlag.Engine.Writing;

/// <summary>
/// Creates an incremental plan for explicitly targeted bound annotations on
/// one page while always refreshing that page's authoritative `/Annots` array.
/// </summary>
public static class PdfAnnotationIncrementalWritePlanner
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
                "Annotation incremental planning requires an incremental base write plan.");
        }

        PdfObjectWriteProjectionSet projections =
            PdfAnnotationWriteProjector.Create(
                writePlan.Document,
                pageIdentifier,
                annotationIdentifiers);

        return PdfProjectedIncrementalWritePlanner.Create(
            writePlan,
            sourceRevision,
            projections);
    }
}
