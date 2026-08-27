using System.Collections.ObjectModel;
using BlackFlag.Engine.Security.Model;

namespace BlackFlag.Engine.Security.Containers;

/// <summary>Contains normalized observations from recursive embedded-object inspection.</summary>
public sealed class DocumentEmbeddedInspectionResult
{
    private readonly ReadOnlyCollection<DocumentSecurityObservation> observations;

    public DocumentEmbeddedInspectionResult(IEnumerable<DocumentSecurityObservation> observations)
    {
        ArgumentNullException.ThrowIfNull(observations);
        this.observations = Array.AsReadOnly(observations.ToArray());
    }

    public IReadOnlyList<DocumentSecurityObservation> Observations => observations;
}
