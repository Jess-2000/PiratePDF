using BlackFlag.Engine.Document.Objects;

namespace BlackFlag.Engine.Writing;

/// <summary>
/// Creates a full-rewrite plan for explicitly targeted bound annotations on
/// one page while always refreshing that page's authoritative `/Annots` array.
/// </summary>
public static class PdfAnnotationFullRewritePlanner
{
    public static PdfWritePlan Create(
        PdfWritePlan writePlan,
        PdfObjectIdentifier pageIdentifier,
        IEnumerable<PdfObjectIdentifier> annotationIdentifiers)
    {
        ArgumentNullException.ThrowIfNull(writePlan);
        ArgumentNullException.ThrowIfNull(annotationIdentifiers);

        if (writePlan.Mode != PdfWriteMode.FullRewrite)
        {
            throw new InvalidOperationException(
                "Annotation full-rewrite planning requires a full-rewrite base write plan.");
        }

        PdfObjectWriteProjectionSet projections =
            PdfAnnotationWriteProjector.Create(
                writePlan.Document,
                pageIdentifier,
                annotationIdentifiers);

        return PdfProjectedFullRewritePlanner.Create(
            writePlan,
            projections);
    }
}
