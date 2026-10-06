namespace BlackFlag.Engine.Writing;

/// <summary>
/// Allocates and projects newly created pages into a full-rewrite plan.
/// </summary>
public static class PdfNewPageFullRewritePlanner
{
    /// <summary>
    /// Reserves generation-zero identifiers for every currently unbound page,
    /// projects those page objects plus the page-tree update, and binds the
    /// pages only after projected full-rewrite planning succeeds.
    /// </summary>
    public static PdfWritePlan Create(
        PdfWritePlan writePlan)
    {
        ArgumentNullException.ThrowIfNull(writePlan);

        if (writePlan.Mode != PdfWriteMode.FullRewrite)
        {
            throw new InvalidOperationException(
                "New-page full-rewrite planning requires a full-rewrite base write plan.");
        }

        PdfNewPageProjectionBatch batch =
            PdfNewPageProjectionBatch.Create(writePlan);

        PdfWritePlan result =
            PdfProjectedFullRewritePlanner.Create(
                writePlan,
                batch.Projections);

        batch.CommitBindings();

        return result;
    }
}
