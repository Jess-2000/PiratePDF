using System.Collections.ObjectModel;
using BlackFlag.Engine.Security.Model;

namespace BlackFlag.Engine.Security.Formats.Ole;

/// <summary>Contains normalized observations discovered in a legacy DOC file.</summary>
public sealed class LegacyWordBinarySecurityAnalysisResult
{
    private readonly ReadOnlyCollection<DocumentSecurityObservation> observations;

    public LegacyWordBinarySecurityAnalysisResult(
        IEnumerable<DocumentSecurityObservation> observations)
    {
        ArgumentNullException.ThrowIfNull(observations);
        this.observations = Array.AsReadOnly(observations.ToArray());
    }

    public IReadOnlyList<DocumentSecurityObservation> Observations => observations;
}
