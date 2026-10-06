using System.Collections.ObjectModel;
using BlackFlag.Engine.Security.Model;

namespace BlackFlag.Engine.Security.Formats.Markdown;

/// <summary>Contains normalized observations discovered in Markdown.</summary>
public sealed class MarkdownSecurityAnalysisResult
{
    private readonly ReadOnlyCollection<DocumentSecurityObservation> observations;

    public MarkdownSecurityAnalysisResult(
        IEnumerable<DocumentSecurityObservation> observations)
    {
        ArgumentNullException.ThrowIfNull(observations);
        this.observations = Array.AsReadOnly(observations.ToArray());
    }

    public IReadOnlyList<DocumentSecurityObservation> Observations => observations;
}
