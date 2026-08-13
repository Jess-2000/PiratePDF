using BlackFlag.Engine.Security.Model;

namespace BlackFlag.Engine.Security.Formats.Ole;

/// <summary>Contains normalized observations discovered in a legacy DOC file.</summary>
public sealed class LegacyWordBinarySecurityAnalysisResult
{
    private readonly DocumentSecurityObservation[] observations;

    public LegacyWordBinarySecurityAnalysisResult(
        IEnumerable<DocumentSecurityObservation> observations)
    {
        ArgumentNullException.ThrowIfNull(observations);
        this.observations = observations.ToArray();
    }

    public IReadOnlyList<DocumentSecurityObservation> Observations => observations;
}
