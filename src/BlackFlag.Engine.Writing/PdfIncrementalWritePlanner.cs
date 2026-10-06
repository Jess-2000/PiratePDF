using BlackFlag.Engine.Document.Objects;

namespace BlackFlag.Engine.Writing;

/// <summary>Creates a bounded incremental-update plan without serializing a new revision.</summary>
public static class PdfIncrementalWritePlanner
{
    public static PdfIncrementalWritePlan Create(
        PdfWritePlan writePlan,
        IPdfOriginalSourceRevision sourceRevision,
        IEnumerable<PdfObjectIdentifier> changedObjectIdentifiers)
    {
        ArgumentNullException.ThrowIfNull(writePlan);
        ArgumentNullException.ThrowIfNull(sourceRevision);
        ArgumentNullException.ThrowIfNull(changedObjectIdentifiers);

        if (writePlan.Mode != PdfWriteMode.IncrementalUpdate)
        {
            throw new InvalidOperationException(
                "An incremental write plan requires PdfWriteMode.IncrementalUpdate.");
        }

        if (writePlan.EncryptionObject.HasValue)
        {
            throw new NotSupportedException(
                "Encrypted incremental output is deferred until encryption-aware Writing is implemented.");
        }

        PdfObjectIdentifier[] changed = changedObjectIdentifiers
            .Distinct()
            .OrderBy(identifier => identifier.ObjectNumber)
            .ThenBy(identifier => identifier.GenerationNumber)
            .ToArray();

        if (changed.Length == 0)
        {
            throw new ArgumentException(
                "An incremental update must identify at least one changed indirect object.",
                nameof(changedObjectIdentifiers));
        }

        foreach (PdfObjectIdentifier identifier in changed)
        {
            if (!writePlan.Document.Objects.Contains(identifier))
            {
                throw new InvalidOperationException(
                    $"Changed object '{identifier}' is not present in the current document object store.");
            }
        }

        PdfCrossReferenceWriteMode crossReferenceMode =
            ResolveCrossReferenceMode(writePlan, sourceRevision);

        if (crossReferenceMode == PdfCrossReferenceWriteMode.CrossReferenceStream &&
            writePlan.Document.Version < new Document.Model.PdfVersion(1, 5))
        {
            throw new NotSupportedException(
                "Cross-reference streams require PDF 1.5 or later.");
        }

        return new PdfIncrementalWritePlan(
            writePlan,
            sourceRevision,
            changed,
            crossReferenceMode);
    }

    private static PdfCrossReferenceWriteMode ResolveCrossReferenceMode(
        PdfWritePlan writePlan,
        IPdfOriginalSourceRevision sourceRevision)
    {
        return writePlan.CrossReferenceMode switch
        {
            PdfCrossReferenceWriteMode.PreserveSource =>
                sourceRevision.CrossReferenceMode,
            PdfCrossReferenceWriteMode.TraditionalTable =>
                PdfCrossReferenceWriteMode.TraditionalTable,
            PdfCrossReferenceWriteMode.CrossReferenceStream =>
                PdfCrossReferenceWriteMode.CrossReferenceStream,
            _ => throw new InvalidOperationException(
                "The requested cross-reference representation is not supported.")
        };
    }
}
