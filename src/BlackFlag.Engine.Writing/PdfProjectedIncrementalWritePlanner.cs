using BlackFlag.Engine.Document.Objects;

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

        ValidateProjections(writePlan, projections);

        PdfObjectStore objects = writePlan.Document.Objects;
        IReadOnlyDictionary<PdfObjectIdentifier, PdfObject> snapshot =
            objects.CreateSnapshot();

        try
        {
            projections.ApplyTo(objects);
            return PdfIncrementalWritePlanner.Create(
                writePlan,
                sourceRevision,
                projections.Identifiers);
        }
        catch
        {
            RestoreProjectedObjects(objects, snapshot, projections);
            throw;
        }
    }

    private static void ValidateProjections(
        PdfWritePlan writePlan,
        PdfObjectWriteProjectionSet projections)
    {
        HashSet<int> existingObjectNumbers =
            writePlan.Document.Objects.Identifiers
                .Select(identifier => identifier.ObjectNumber)
                .ToHashSet();

        foreach (PdfObjectWriteProjection projection in projections.Projections)
        {
            if (writePlan.Document.Objects.Contains(projection.Identifier))
            {
                continue;
            }

            if (existingObjectNumbers.Contains(projection.Identifier.ObjectNumber))
            {
                throw new InvalidOperationException(
                    $"Projected object '{projection.Identifier}' conflicts with an existing active generation.");
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
    }

    private static void RestoreProjectedObjects(
        PdfObjectStore objects,
        IReadOnlyDictionary<PdfObjectIdentifier, PdfObject> snapshot,
        PdfObjectWriteProjectionSet projections)
    {
        foreach (PdfObjectWriteProjection projection in projections.Projections)
        {
            if (snapshot.TryGetValue(projection.Identifier, out PdfObject? original))
            {
                objects.Set(projection.Identifier, original);
            }
            else
            {
                objects.Remove(projection.Identifier);
            }
        }
    }
}
