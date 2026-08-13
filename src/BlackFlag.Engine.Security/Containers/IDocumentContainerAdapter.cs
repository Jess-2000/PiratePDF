namespace BlackFlag.Engine.Security.Containers;

/// <summary>
/// Safely expands one supported container format into passive child payloads.
/// Implementations must never execute document-provided behavior.
/// </summary>
public interface IDocumentContainerAdapter
{
    bool CanExpand(DocumentEmbeddedObjectClassification classification);

    IEnumerable<DocumentContainerEntry> Expand(
        DocumentEmbeddedObject embeddedObject,
        CancellationToken cancellationToken = default);
}
