using System.Collections.ObjectModel;
using BlackFlag.Engine.Security.Model;

namespace BlackFlag.Engine.Security.Containers;

/// <summary>
/// Contains format-specific security observations and embedded children
/// discovered while safely inspecting one container.
/// </summary>
public sealed class DocumentContainerSecurityInspection
{
    private readonly ReadOnlyCollection<DocumentSecurityObservation> observations;
    private readonly ReadOnlyCollection<DocumentContainerEntry> embeddedObjects;

    public DocumentContainerSecurityInspection(
        IEnumerable<DocumentSecurityObservation>? observations = null,
        IEnumerable<DocumentContainerEntry>? embeddedObjects = null)
    {
        this.observations = Array.AsReadOnly(
            observations?.ToArray() ?? Array.Empty<DocumentSecurityObservation>());
        this.embeddedObjects = Array.AsReadOnly(
            embeddedObjects?.ToArray() ?? Array.Empty<DocumentContainerEntry>());
    }

    public IReadOnlyList<DocumentSecurityObservation> Observations => observations;

    public IReadOnlyList<DocumentContainerEntry> EmbeddedObjects => embeddedObjects;
}
