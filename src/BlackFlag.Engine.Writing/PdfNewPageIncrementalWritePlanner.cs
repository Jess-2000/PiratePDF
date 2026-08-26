namespace BlackFlag.Engine.Writing;

/// <summary>
/// Allocates and projects newly created pages into an incremental write plan.
/// </summary>
public static class PdfNewPageIncrementalWritePlanner
{
    /// <summary>
    /// Reserves generation-zero identifiers for every currently unbound page,
    /// projects those page objects plus the page-tree update, and binds the
    /// pages only after projected incremental planning succeeds.
    /// </summary>
    public static PdfIncrementalWritePlan Create(
        PdfWritePlan writePlan,
        IPdfOriginalSourceRevision sourceRevision)
    {
        ArgumentNullException.ThrowIfNull(writePlan);
        ArgumentNullException.ThrowIfNull(sourceRevision);

        if (writePlan.Mode != PdfWriteMode.IncrementalUpdate)
        {
            throw new InvalidOperationException(
                "New-page incremental planning requires an incremental base write plan.");
        }

        PdfNewPageProjectionBatch batch =
            PdfNewPageProjectionBatch.Create(writePlan);

        PdfIncrementalWritePlan result =
            PdfProjectedIncrementalWritePlanner.Create(
                writePlan,
                sourceRevision,
                batch.Projections);

        batch.CommitBindings();

        return result;
    }
}
