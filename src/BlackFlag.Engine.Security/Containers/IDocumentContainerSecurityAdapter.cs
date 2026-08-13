using BlackFlag.Engine.Security.Inspection;

namespace BlackFlag.Engine.Security.Containers;

/// <summary>
/// Supplies format-specific, passive security inspection for a container while
/// delegating recursive child traversal to the shared BlackFlag engine.
/// </summary>
public interface IDocumentContainerSecurityAdapter
{
    bool CanInspect(DocumentEmbeddedObjectClassification classification);

    DocumentContainerSecurityInspection Inspect(
        DocumentEmbeddedObject embeddedObject,
        DocumentSecurityInspectionBudget budget,
        CancellationToken cancellationToken = default);
}
