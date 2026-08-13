namespace BlackFlag.Engine.Security.Formats.Rtf;

/// <summary>Configures bounded Rich Text Format security inspection.</summary>
public sealed record RtfSecurityAnalysisOptions
{
    public static RtfSecurityAnalysisOptions Default { get; } = new();

    public long MaximumDocumentBytes { get; init; } = 64L * 1024 * 1024;

    public int MaximumEmbeddedObjectBytes { get; init; } = 32 * 1024 * 1024;

    public void Validate()
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(MaximumDocumentBytes);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(MaximumEmbeddedObjectBytes);
    }
}
