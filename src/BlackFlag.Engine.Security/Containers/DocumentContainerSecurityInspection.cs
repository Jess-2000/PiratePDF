using BlackFlag.Engine.Security.Model;

namespace BlackFlag.Engine.Security.Containers;

/// <summary>
/// Contains format-specific security observations and embedded children
/// discovered while safely inspecting one container.
/// </summary>
public sealed class DocumentContainerSecurityInspection
{
    private readonly DocumentSecurityObservation[] observations;
    private readonly DocumentContainerEntry[] embeddedObjects;

    public DocumentContainerSecurityInspection(
        IEnumerable<DocumentSecurityObservation>? observations = null,
        IEnumerable<DocumentContainerEntry>? embeddedObjects = null)
    {
        this.observations = observations?.ToArray() ?? [];
        this.embeddedObjects = embeddedObjects?.ToArray() ?? [];
    }

    public IReadOnlyList<DocumentSecurityObservation> Observations => observations;

    public IReadOnlyList<DocumentContainerEntry> EmbeddedObjects => embeddedObjects;
}
