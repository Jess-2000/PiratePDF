using BlackFlag.Engine.Document.Objects;

namespace BlackFlag.Engine.Writing;

/// <summary>
/// Allocates and projects newly created annotations into a full-rewrite plan.
/// </summary>
public static class PdfNewAnnotationFullRewritePlanner
{
    /// <summary>
    /// Reserves generation-zero identifiers for every currently unbound
    /// annotation on the target page and binds them only after projected
    /// full-rewrite planning succeeds.
    /// </summary>
    public static PdfWritePlan Create(
        PdfWritePlan writePlan,
        PdfObjectIdentifier pageIdentifier)
    {
        ArgumentNullException.ThrowIfNull(writePlan);

        if (writePlan.Mode != PdfWriteMode.FullRewrite)
        {
            throw new InvalidOperationException(
                "New-annotation full-rewrite planning requires a full-rewrite base write plan.");
        }

        PdfNewAnnotationProjectionBatch batch =
            PdfNewAnnotationProjectionBatch.Create(
                writePlan,
                pageIdentifier);

        PdfWritePlan result =
            PdfProjectedFullRewritePlanner.Create(
                writePlan,
                batch.Projections);

        batch.CommitBindings();

        return result;
    }
}
