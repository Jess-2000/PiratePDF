using BlackFlag.Engine.Security.Model;

namespace BlackFlag.Engine.Security.Formats.Ooxml;

/// <summary>Contains normalized observations discovered in a DOCX or DOCM package.</summary>
public sealed class OoxmlWordSecurityAnalysisResult
{
    private readonly DocumentSecurityObservation[] observations;

    public OoxmlWordSecurityAnalysisResult(
        IEnumerable<DocumentSecurityObservation> observations)
    {
        ArgumentNullException.ThrowIfNull(observations);
        this.observations = observations.ToArray();
    }

    public IReadOnlyList<DocumentSecurityObservation> Observations => observations;
}
