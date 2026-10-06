using System.Collections.ObjectModel;
using BlackFlag.Engine.Security.Model;

namespace BlackFlag.Engine.Security.Formats.OpenDocument;

/// <summary>Contains normalized observations discovered in an ODT package.</summary>
public sealed class OpenDocumentTextSecurityAnalysisResult
{
    private readonly ReadOnlyCollection<DocumentSecurityObservation> observations;

    public OpenDocumentTextSecurityAnalysisResult(
        IEnumerable<DocumentSecurityObservation> observations)
    {
        ArgumentNullException.ThrowIfNull(observations);
        this.observations = Array.AsReadOnly(observations.ToArray());
    }

    public IReadOnlyList<DocumentSecurityObservation> Observations => observations;
}
