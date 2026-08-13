namespace BlackFlag.Engine.Security.Formats.Ole;

/// <summary>Configures bounded Compound File Binary security inspection.</summary>
public sealed record OleCompoundFileSecurityAnalysisOptions
{
    public static OleCompoundFileSecurityAnalysisOptions Default { get; } = new();

    public int MaximumDirectoryEntries { get; init; } = 100_000;

    public long MaximumStreamBytes { get; init; } = 64L * 1024 * 1024;

    public long MaximumTextScanBytes { get; init; } = 16L * 1024 * 1024;

    public void Validate()
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(MaximumDirectoryEntries);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(MaximumStreamBytes);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(MaximumTextScanBytes);
    }
}
