using BlackFlag.Engine.Security.Model;

namespace BlackFlag.Engine.Security.Formats.OpenDocument;

/// <summary>Contains normalized observations discovered in an ODT package.</summary>
public sealed class OpenDocumentTextSecurityAnalysisResult
{
    private readonly DocumentSecurityObservation[] observations;

    public OpenDocumentTextSecurityAnalysisResult(
        IEnumerable<DocumentSecurityObservation> observations)
    {
        ArgumentNullException.ThrowIfNull(observations);
        this.observations = observations.ToArray();
    }

    public IReadOnlyList<DocumentSecurityObservation> Observations => observations;
}
