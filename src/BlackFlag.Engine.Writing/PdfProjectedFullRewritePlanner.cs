using BlackFlag.Engine.Document.Objects;

namespace BlackFlag.Engine.Writing;

/// <summary>
/// Applies explicit indirect-object projections and refreshes a full-rewrite
/// plan so newly projected objects participate in complete serialization.
/// </summary>
public static class PdfProjectedFullRewritePlanner
{
    public static PdfWritePlan Create(
        PdfWritePlan writePlan,
        PdfObjectWriteProjectionSet projections)
    {
        ArgumentNullException.ThrowIfNull(writePlan);
        ArgumentNullException.ThrowIfNull(projections);

        if (writePlan.Mode != PdfWriteMode.FullRewrite)
        {
            throw new InvalidOperationException(
                "Projected full rewriting requires a full-rewrite base write plan.");
        }

        ValidateProjections(writePlan, projections);

        PdfObjectStore objects = writePlan.Document.Objects;
        IReadOnlyDictionary<PdfObjectIdentifier, PdfObject> snapshot =
            objects.CreateSnapshot();

        try
        {
            projections.ApplyTo(objects);
            return PdfWritePlanner.Create(
                writePlan.Document,
                CreateEquivalentOptions(writePlan));
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
            [.. writePlan.Document.Objects.Identifiers.Select(identifier => identifier.ObjectNumber)];

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

    private static PdfWriteOptions CreateEquivalentOptions(PdfWritePlan plan) =>
        new()
        {
            Mode = plan.Mode,
            CrossReferenceMode = plan.CrossReferenceMode,
            PreserveExistingObjectIdentifiers =
                plan.Preservation.PreservesExistingObjectIdentifiers,
            PreserveUntargetedObjects =
                plan.Preservation.PreservesUntargetedObjects,
            NormalizeUntargetedObjects =
                !plan.Preservation.AvoidsUntargetedNormalization
        };

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
