namespace BlackFlag.Engine.Writing;

/// <summary>Applies explicit indirect-object projections and creates their incremental write plan.</summary>
public static class PdfProjectedIncrementalWritePlanner
{
    public static PdfIncrementalWritePlan Create(
        PdfWritePlan writePlan,
        IPdfOriginalSourceRevision sourceRevision,
        PdfObjectWriteProjectionSet projections)
    {
        ArgumentNullException.ThrowIfNull(writePlan);
        ArgumentNullException.ThrowIfNull(sourceRevision);
        ArgumentNullException.ThrowIfNull(projections);

        if (writePlan.Mode != PdfWriteMode.IncrementalUpdate)
        {
            throw new InvalidOperationException(
                "Projected incremental writing requires an incremental base write plan.");
        }

        foreach (PdfObjectWriteProjection projection in projections.Projections)
        {
            if (writePlan.Document.Objects.Contains(projection.Identifier))
            {
                continue;
            }

            if (projection.Identifier.GenerationNumber != 0)
            {
                throw new InvalidOperationException(
                    $"New projected object '{projection.Identifier}' must use generation zero.");
            }

            if (projection.Identifier.ObjectNumber < writePlan.NextObjectNumber)
            {
                throw new InvalidOperationException(
                    $"New projected object '{projection.Identifier}' is below the write plan allocation boundary.");
            }
        }

        projections.ApplyTo(writePlan.Document.Objects);
        return PdfIncrementalWritePlanner.Create(
            writePlan,
            sourceRevision,
            projections.Identifiers);
    }
}
