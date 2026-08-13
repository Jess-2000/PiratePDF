using BlackFlag.Engine.Security.Model;

namespace BlackFlag.Engine.Security.Formats.Markdown;

/// <summary>Contains normalized observations discovered in Markdown.</summary>
public sealed class MarkdownSecurityAnalysisResult
{
    private readonly DocumentSecurityObservation[] observations;

    public MarkdownSecurityAnalysisResult(
        IEnumerable<DocumentSecurityObservation> observations)
    {
        ArgumentNullException.ThrowIfNull(observations);
        this.observations = observations.ToArray();
    }

    public IReadOnlyList<DocumentSecurityObservation> Observations => observations;
}
