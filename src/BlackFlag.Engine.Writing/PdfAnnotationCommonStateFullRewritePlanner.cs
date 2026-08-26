using BlackFlag.Engine.Document.Objects;

namespace BlackFlag.Engine.Writing;

/// <summary>
/// Creates a full-rewrite plan for common-state changes to already-bound
/// annotations without rewriting the owning page dictionary.
/// </summary>
public static class PdfAnnotationCommonStateFullRewritePlanner
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
                "Annotation common-state full-rewrite planning requires a full-rewrite base write plan.");
        }

        PdfObjectIdentifier[] targets = [.. annotationIdentifiers];

        if (targets.Length == 0)
        {
            return writePlan;
        }

        PdfObjectWriteProjectionSet projections =
            PdfAnnotationWriteProjector.CreateAnnotationObjects(
                writePlan.Document,
                pageIdentifier,
                targets);

        return PdfProjectedFullRewritePlanner.Create(
            writePlan,
            projections);
    }
}
