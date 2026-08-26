namespace BlackFlag.Engine.Writing;

/// <summary>
/// Defines preservation-oriented policy for a PDF write operation.
/// </summary>
public sealed class PdfWriteOptions
{
    /// <summary>Gets or initializes the requested revision strategy.</summary>
    public PdfWriteMode Mode { get; init; } = PdfWriteMode.FullRewrite;

    /// <summary>Gets or initializes the requested cross-reference representation.</summary>
    public PdfCrossReferenceWriteMode CrossReferenceMode { get; init; } =
        PdfCrossReferenceWriteMode.PreserveSource;

    /// <summary>
    /// Gets or initializes whether existing indirect-object numbers and generations
    /// must be retained for objects that remain present in the logical document.
    /// </summary>
    public bool PreserveExistingObjectIdentifiers { get; init; } = true;

    /// <summary>
    /// Gets or initializes whether objects outside the targeted logical edit scope
    /// must be preserved rather than rebuilt merely for normalization.
    /// </summary>
    public bool PreserveUntargetedObjects { get; init; } = true;

    /// <summary>
    /// Gets or initializes whether the writer may normalize untargeted objects.
    /// The default is <see langword="false"/>.
    /// </summary>
    public bool NormalizeUntargetedObjects { get; init; }

    internal void Validate()
    {
        if (!Enum.IsDefined(Mode))
        {
            throw new ArgumentOutOfRangeException(
                nameof(Mode),
                Mode,
                "The PDF write mode is not supported.");
        }

        if (!Enum.IsDefined(CrossReferenceMode))
        {
            throw new ArgumentOutOfRangeException(
                nameof(CrossReferenceMode),
                CrossReferenceMode,
                "The cross-reference write mode is not supported.");
        }

        if (NormalizeUntargetedObjects && PreserveUntargetedObjects)
        {
            throw new InvalidOperationException(
                "Untargeted objects cannot be both preserved and normalized.");
        }
    }
}
