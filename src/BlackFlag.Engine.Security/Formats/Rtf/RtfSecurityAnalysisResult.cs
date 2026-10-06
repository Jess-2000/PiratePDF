using System.Collections.ObjectModel;
using BlackFlag.Engine.Security.Model;

namespace BlackFlag.Engine.Security.Formats.Rtf;

/// <summary>Contains normalized observations discovered in an RTF document.</summary>
public sealed class RtfSecurityAnalysisResult
{
    private readonly ReadOnlyCollection<DocumentSecurityObservation> observations;

    public RtfSecurityAnalysisResult(IEnumerable<DocumentSecurityObservation> observations)
    {
        ArgumentNullException.ThrowIfNull(observations);
        this.observations = Array.AsReadOnly(observations.ToArray());
    }

    public IReadOnlyList<DocumentSecurityObservation> Observations => observations;
}
