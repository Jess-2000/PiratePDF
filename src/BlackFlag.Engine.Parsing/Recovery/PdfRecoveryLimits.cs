
namespace BlackFlag.Engine.Parsing.Recovery;

/// <summary>Defines bounded work and allocation limits for damaged-PDF recovery.</summary>
public sealed record PdfRecoveryLimits
{
    /// <summary>Gets production defaults.</summary>
    public static PdfRecoveryLimits Default { get; } = new();

    /// <summary>Gets the maximum source size scanned in one recovery operation.</summary>
    public long MaximumRecoveryScanBytes { get; init; } = 512L * 1024 * 1024;

    /// <summary>Gets the maximum number of accepted indirect-object candidates.</summary>
    public int MaximumRecoveredObjects { get; init; } = 1_000_000;

    /// <summary>Gets the maximum candidate object number.</summary>
    public int MaximumObjectNumber { get; init; } = 10_000_000;

    /// <summary>Gets the maximum trailer candidates retained for analysis.</summary>
    public int MaximumTrailerCandidates { get; init; } = 4096;

    /// <summary>Validates configured limits.</summary>
    public void Validate()
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(MaximumRecoveryScanBytes);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(MaximumRecoveredObjects);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(MaximumObjectNumber);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(MaximumTrailerCandidates);
    }
}
