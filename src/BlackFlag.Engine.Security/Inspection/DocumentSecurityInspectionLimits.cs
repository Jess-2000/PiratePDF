namespace BlackFlag.Engine.Security.Inspection;

/// <summary>Defines bounded shared limits for document security inspection.</summary>
public sealed record DocumentSecurityInspectionLimits
{
    /// <summary>Gets production defaults.</summary>
    public static DocumentSecurityInspectionLimits Default { get; } = new();

    /// <summary>Gets the maximum recursive embedded-container depth.</summary>
    public int MaximumContainerDepth { get; init; } = 16;

    /// <summary>Gets the maximum embedded objects inspected per document.</summary>
    public int MaximumEmbeddedObjects { get; init; } = 10_000;

    /// <summary>Gets the maximum total expanded bytes inspected.</summary>
    public long MaximumExpandedBytes { get; init; } = 2L * 1024 * 1024 * 1024;

    /// <summary>Gets the maximum expanded size of one embedded object.</summary>
    public long MaximumSingleEmbeddedObjectBytes { get; init; } = 512L * 1024 * 1024;

    /// <summary>Gets the maximum accepted decompression ratio.</summary>
    public double MaximumCompressionRatio { get; init; } = 1_000;

    /// <summary>Gets the maximum number of normalized observations.</summary>
    public int MaximumObservations { get; init; } = 100_000;

    /// <summary>Gets the maximum shared inspection work units.</summary>
    public long MaximumInspectionWorkUnits { get; init; } = 5_000_000;

    /// <summary>Validates the configured limits.</summary>
    public void Validate()
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(MaximumContainerDepth);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(MaximumEmbeddedObjects);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(MaximumExpandedBytes);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(MaximumSingleEmbeddedObjectBytes);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(MaximumCompressionRatio);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(MaximumObservations);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(MaximumInspectionWorkUnits);
    }
}
