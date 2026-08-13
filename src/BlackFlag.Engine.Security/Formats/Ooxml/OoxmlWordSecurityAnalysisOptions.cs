namespace BlackFlag.Engine.Security.Formats.Ooxml;

/// <summary>Configures bounded OOXML Word security inspection.</summary>
public sealed record OoxmlWordSecurityAnalysisOptions
{
    public static OoxmlWordSecurityAnalysisOptions Default { get; } = new();

    /// <summary>Gets the maximum number of ZIP entries inspected in one OOXML package.</summary>
    public int MaximumArchiveEntries { get; init; } = 100_000;

    /// <summary>Gets the maximum size of one XML part inspected for security metadata.</summary>
    public long MaximumXmlPartBytes { get; init; } = 32L * 1024 * 1024;

    public void Validate()
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(MaximumArchiveEntries);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(MaximumXmlPartBytes);
    }
}
