namespace BlackFlag.Engine.Security.Formats.Markdown;

/// <summary>Configures bounded Markdown security inspection.</summary>
public sealed record MarkdownSecurityAnalysisOptions
{
    public static MarkdownSecurityAnalysisOptions Default { get; } = new();

    public long MaximumDocumentBytes { get; init; } = 32L * 1024 * 1024;

    public void Validate() =>
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(MaximumDocumentBytes);
}
