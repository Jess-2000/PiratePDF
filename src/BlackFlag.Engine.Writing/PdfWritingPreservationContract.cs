namespace BlackFlag.Engine.Writing;

/// <summary>
/// Describes the preservation guarantees carried by a prepared PDF write plan.
/// </summary>
public sealed class PdfWritingPreservationContract
{
    internal PdfWritingPreservationContract(PdfWriteOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        PreservesExistingObjectIdentifiers = options.PreserveExistingObjectIdentifiers;
        PreservesUntargetedObjects = options.PreserveUntargetedObjects;
        AvoidsUntargetedNormalization = !options.NormalizeUntargetedObjects;
    }

    /// <summary>Gets whether existing indirect-object identifiers must be retained.</summary>
    public bool PreservesExistingObjectIdentifiers { get; }

    /// <summary>Gets whether untargeted objects must be preserved.</summary>
    public bool PreservesUntargetedObjects { get; }

    /// <summary>Gets whether untargeted normalization is prohibited.</summary>
    public bool AvoidsUntargetedNormalization { get; }

    /// <summary>
    /// Gets whether the contract promises byte-for-byte equivalence. Writing does
    /// not make that promise because offsets, xref representation, and targeted
    /// object bytes may legitimately change.
    /// </summary>
    public static bool GuaranteesByteLevelEquivalence => false;
}
