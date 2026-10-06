using BlackFlag.Engine.Document.Objects;

namespace BlackFlag.Engine.Writing;

/// <summary>
/// Allocates and projects newly created annotations into an incremental plan.
/// </summary>
public static class PdfNewAnnotationIncrementalWritePlanner
{
    /// <summary>
    /// Reserves generation-zero identifiers for every currently unbound
    /// annotation on the target page and binds them only after projected
    /// incremental planning succeeds.
    /// </summary>
    public static PdfIncrementalWritePlan Create(
        PdfWritePlan writePlan,
        IPdfOriginalSourceRevision sourceRevision,
        PdfObjectIdentifier pageIdentifier)
    {
        ArgumentNullException.ThrowIfNull(writePlan);
        ArgumentNullException.ThrowIfNull(sourceRevision);

        if (writePlan.Mode != PdfWriteMode.IncrementalUpdate)
        {
            throw new InvalidOperationException(
                "New-annotation incremental planning requires an incremental base write plan.");
        }

        PdfNewAnnotationProjectionBatch batch =
            PdfNewAnnotationProjectionBatch.Create(
                writePlan,
                pageIdentifier);

        PdfIncrementalWritePlan result =
            PdfProjectedIncrementalWritePlanner.Create(
                writePlan,
                sourceRevision,
                batch.Projections);

        batch.CommitBindings();

        return result;
    }
}
