
using System.Collections.ObjectModel;
using BlackFlag.Engine.Security.Model;

namespace BlackFlag.Engine.Parsing.Security;

/// <summary>Contains normalized observations discovered in one PDF object graph.</summary>
public sealed class PdfSecurityAnalysisResult
{
    private readonly ReadOnlyCollection<DocumentSecurityObservation> observations;

    /// <summary>Initializes a PDF security analysis result.</summary>
    public PdfSecurityAnalysisResult(
        IEnumerable<DocumentSecurityObservation> observations)
    {
        ArgumentNullException.ThrowIfNull(observations);
        this.observations = Array.AsReadOnly(observations.ToArray());
    }

    /// <summary>Gets normalized security observations.</summary>
    public IReadOnlyList<DocumentSecurityObservation> Observations => observations;
}
