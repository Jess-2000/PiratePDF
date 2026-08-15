using BlackFlag.Engine.Document.Model;
using BlackFlag.Engine.Document.Objects;

namespace BlackFlag.Engine.Writing;

/// <summary>
/// Represents an immutable, preservation-aware plan for a future PDF write.
/// </summary>
public sealed class PdfWritePlan
{
    private readonly IReadOnlyList<PdfObjectIdentifier> existingObjectIdentifiers;

    internal PdfWritePlan(
        PdfDocument document,
        PdfWriteOptions options,
        IEnumerable<PdfObjectIdentifier> existingObjectIdentifiers,
        int nextObjectNumber)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(existingObjectIdentifiers);

        Document = document;
        Mode = options.Mode;
        CrossReferenceMode = options.CrossReferenceMode;
        Preservation = new PdfWritingPreservationContract(options);
        this.existingObjectIdentifiers = Array.AsReadOnly(existingObjectIdentifiers.ToArray());
        NextObjectNumber = nextObjectNumber;
    }

    /// <summary>Gets the logical document whose current state is planned for output.</summary>
    public PdfDocument Document { get; }

    /// <summary>Gets the requested physical revision strategy.</summary>
    public PdfWriteMode Mode { get; }

    /// <summary>Gets the requested cross-reference representation policy.</summary>
    public PdfCrossReferenceWriteMode CrossReferenceMode { get; }

    /// <summary>Gets the preservation contract captured when the plan was created.</summary>
    public PdfWritingPreservationContract Preservation { get; }

    /// <summary>Gets the root catalog object identifier.</summary>
    public PdfObjectIdentifier RootObject => Document.Trailer.RootObject!.Value;

    /// <summary>Gets the optional document-information object identifier.</summary>
    public PdfObjectIdentifier? InformationObject => Document.Trailer.InformationObject;

    /// <summary>Gets the optional encryption dictionary object identifier.</summary>
    public PdfObjectIdentifier? EncryptionObject => Document.Trailer.EncryptionObject;

    /// <summary>Gets a stable snapshot of indirect-object identifiers present at planning time.</summary>
    public IReadOnlyList<PdfObjectIdentifier> ExistingObjectIdentifiers =>
        existingObjectIdentifiers;

    /// <summary>
    /// Gets the first object number available for newly allocated indirect objects.
    /// Existing gaps are deliberately not reused.
    /// </summary>
    public int NextObjectNumber { get; }

    /// <summary>
    /// Gets whether execution requires access to the original source revision.
    /// Incremental update always requires the preceding bytes.
    /// </summary>
    public bool RequiresOriginalSourceRevision => Mode == PdfWriteMode.IncrementalUpdate;
}
