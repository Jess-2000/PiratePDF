namespace BlackFlag.Engine.Security.Formats.OpenDocument;

/// <summary>Configures bounded OpenDocument Text security inspection.</summary>
public sealed record OpenDocumentTextSecurityAnalysisOptions
{
    public static OpenDocumentTextSecurityAnalysisOptions Default { get; } = new();

    public int MaximumArchiveEntries { get; init; } = 100_000;

    public long MaximumXmlPartBytes { get; init; } = 32L * 1024 * 1024;

    public void Validate()
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(MaximumArchiveEntries);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(MaximumXmlPartBytes);
    }
}
