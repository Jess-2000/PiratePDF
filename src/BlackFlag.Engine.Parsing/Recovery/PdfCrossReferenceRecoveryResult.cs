
using BlackFlag.Engine.Objects;
using BlackFlag.Engine.Parsing.CrossReferences;
using BlackFlag.Engine.Parsing.Loading;

namespace BlackFlag.Engine.Parsing.Recovery;

/// <summary>Contains the synthetic object index and recovery metadata.</summary>
public sealed class PdfCrossReferenceRecoveryResult
{
    private readonly PdfRecoveryDiagnostic[] diagnostics;
    private readonly PdfRecoveredObjectCandidate[] objects;

    /// <summary>Initializes an immutable recovery result.</summary>
    public PdfCrossReferenceRecoveryResult(
        PdfObjectLocationTable locationTable,
        PdfDictionary? trailer,
        PdfObjectId? rootObjectId,
        PdfRecoveredEncryptionState encryptionState,
        PdfRecoveryConfidence confidence,
        IEnumerable<PdfRecoveredObjectCandidate> objects,
        IEnumerable<PdfRecoveryDiagnostic> diagnostics)
    {
        ArgumentNullException.ThrowIfNull(locationTable);
        ArgumentNullException.ThrowIfNull(objects);
        ArgumentNullException.ThrowIfNull(diagnostics);

        LocationTable = locationTable;
        Trailer = trailer;
        RootObjectId = rootObjectId;
        EncryptionState = encryptionState;
        Confidence = confidence;
        this.objects = objects.ToArray();
        this.diagnostics = diagnostics.ToArray();
    }

    /// <summary>Gets the synthetic newest-wins object-location table.</summary>
    public PdfObjectLocationTable LocationTable { get; }

    /// <summary>Gets the recovered trailer when one was established.</summary>
    public PdfDictionary? Trailer { get; }

    /// <summary>Gets the recovered catalog object identifier.</summary>
    public PdfObjectId? RootObjectId { get; }

    /// <summary>Gets encryption state discovered during recovery.</summary>
    public PdfRecoveredEncryptionState EncryptionState { get; }

    /// <summary>Gets recovery confidence.</summary>
    public PdfRecoveryConfidence Confidence { get; }

    /// <summary>Gets parser-validated recovered objects in physical order.</summary>
    public IReadOnlyList<PdfRecoveredObjectCandidate> Objects => objects;

    /// <summary>Gets recovery diagnostics in encounter order.</summary>
    public IReadOnlyList<PdfRecoveryDiagnostic> Diagnostics => diagnostics;
}
