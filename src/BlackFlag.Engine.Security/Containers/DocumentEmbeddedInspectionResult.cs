using BlackFlag.Engine.Security.Model;

namespace BlackFlag.Engine.Security.Containers;

/// <summary>Contains normalized observations from recursive embedded-object inspection.</summary>
public sealed class DocumentEmbeddedInspectionResult
{
    private readonly DocumentSecurityObservation[] observations;

    public DocumentEmbeddedInspectionResult(IEnumerable<DocumentSecurityObservation> observations)
    {
        ArgumentNullException.ThrowIfNull(observations);
        this.observations = observations.ToArray();
    }

    public IReadOnlyList<DocumentSecurityObservation> Observations => observations;
}
