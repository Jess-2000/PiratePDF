namespace BlackFlag.Engine.Editing.Preservation;

/// <summary>
/// Describes preservation guarantees that an editing session must maintain
/// while applying targeted logical edits.
/// </summary>
public sealed class PdfEditingPreservationContract
{
    private readonly bool preservesUntargetedStructure;
    private readonly bool preservesExistingObjectIdentity;
    private readonly bool avoidsImplicitNormalization;
    private readonly bool preservesHistoryObjectIdentity;
    private readonly bool supportsTargetedFusionProPreservation;
    private readonly bool guaranteesByteLevelEquivalence;

    internal PdfEditingPreservationContract(
        bool preservesUntargetedStructure = true,
        bool preservesExistingObjectIdentity = true,
        bool avoidsImplicitNormalization = true,
        bool preservesHistoryObjectIdentity = true,
        bool supportsTargetedFusionProPreservation = true,
        bool guaranteesByteLevelEquivalence = false)
    {
        this.preservesUntargetedStructure =
            preservesUntargetedStructure;

        this.preservesExistingObjectIdentity =
            preservesExistingObjectIdentity;

        this.avoidsImplicitNormalization =
            avoidsImplicitNormalization;

        this.preservesHistoryObjectIdentity =
            preservesHistoryObjectIdentity;

        this.supportsTargetedFusionProPreservation =
            supportsTargetedFusionProPreservation;

        this.guaranteesByteLevelEquivalence =
            guaranteesByteLevelEquivalence;
    }

    /// <summary>
    /// Gets the default preservation contract used by editing sessions.
    /// </summary>
    public static PdfEditingPreservationContract Default { get; } =
        new();

    /// <summary>
    /// Gets whether logical state outside the requested mutation scope must
    /// remain unchanged.
    /// </summary>
    public bool PreservesUntargetedStructure =>
        preservesUntargetedStructure;

    /// <summary>
    /// Gets whether existing object identity must be retained wherever the
    /// requested edit does not require replacement.
    /// </summary>
    public bool PreservesExistingObjectIdentity =>
        preservesExistingObjectIdentity;

    /// <summary>
    /// Gets whether editing must avoid implicit normalization or rebuilding
    /// of structures that are not directly targeted by the edit.
    /// </summary>
    public bool AvoidsImplicitNormalization =>
        avoidsImplicitNormalization;

    /// <summary>
    /// Gets whether rollback, undo, and redo must reuse accepted logical
    /// objects rather than cloning them.
    /// </summary>
    public bool PreservesHistoryObjectIdentity =>
        preservesHistoryObjectIdentity;

    /// <summary>
    /// Gets whether this contract carries the targeted preservation behavior
    /// required for FusionPro-oriented workflows.
    /// </summary>
    public bool SupportsTargetedFusionProPreservation =>
        supportsTargetedFusionProPreservation;

    /// <summary>
    /// Gets whether byte-for-byte output equivalence is promised by editing.
    /// </summary>
    public bool GuaranteesByteLevelEquivalence =>
        guaranteesByteLevelEquivalence;
}
