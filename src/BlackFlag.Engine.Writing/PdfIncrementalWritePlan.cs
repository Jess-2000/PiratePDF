using BlackFlag.Engine.Document.Objects;

namespace BlackFlag.Engine.Writing;

/// <summary>Captures the preceding revision and exact indirect objects targeted by an incremental update.</summary>
public sealed class PdfIncrementalWritePlan
{
    private readonly IReadOnlyList<PdfObjectIdentifier> changedObjectIdentifiers;

    internal PdfIncrementalWritePlan(
        PdfWritePlan writePlan,
        IPdfOriginalSourceRevision sourceRevision,
        IEnumerable<PdfObjectIdentifier> changedObjectIdentifiers,
        PdfCrossReferenceWriteMode crossReferenceMode)
    {
        ArgumentNullException.ThrowIfNull(writePlan);
        ArgumentNullException.ThrowIfNull(sourceRevision);
        ArgumentNullException.ThrowIfNull(changedObjectIdentifiers);

        WritePlan = writePlan;
        SourceRevision = sourceRevision;
        this.changedObjectIdentifiers = Array.AsReadOnly(
            changedObjectIdentifiers.ToArray());
        CrossReferenceMode = crossReferenceMode;
    }

    /// <summary>Gets the preservation-aware logical write plan.</summary>
    public PdfWritePlan WritePlan { get; }

    /// <summary>Gets the immutable preceding physical revision.</summary>
    public IPdfOriginalSourceRevision SourceRevision { get; }

    /// <summary>Gets the concrete cross-reference representation selected for the appended revision.</summary>
    public PdfCrossReferenceWriteMode CrossReferenceMode { get; }

    /// <summary>Gets the sorted identifiers that may be superseded or introduced by the appended revision.</summary>
    public IReadOnlyList<PdfObjectIdentifier> ChangedObjectIdentifiers =>
        changedObjectIdentifiers;
}
