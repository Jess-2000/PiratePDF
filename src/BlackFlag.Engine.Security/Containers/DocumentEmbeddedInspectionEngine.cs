using BlackFlag.Engine.Security.Inspection;
using BlackFlag.Engine.Security.Model;

namespace BlackFlag.Engine.Security.Containers;

/// <summary>
/// Coordinates bounded recursive inspection of embedded payloads through one shared budget.
/// </summary>
public sealed class DocumentEmbeddedInspectionEngine
{
    private readonly IDocumentEmbeddedObjectClassifier classifier;
    private readonly IDocumentContainerAdapter[] adapters;

    public DocumentEmbeddedInspectionEngine(
        IDocumentEmbeddedObjectClassifier? classifier = null,
        IEnumerable<IDocumentContainerAdapter>? adapters = null)
    {
        this.classifier = classifier ?? new DocumentEmbeddedObjectClassifier();
        this.adapters = adapters?.ToArray() ?? [];
    }

    public DocumentEmbeddedInspectionResult Inspect(
        IEnumerable<DocumentEmbeddedObject> embeddedObjects,
        DocumentSecurityInspectionBudget? budget = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(embeddedObjects);

        budget ??= new DocumentSecurityInspectionBudget();
        var observations = new List<DocumentSecurityObservation>();

        foreach (DocumentEmbeddedObject embeddedObject in embeddedObjects)
        {
            InspectOne(embeddedObject, depth: 1, budget, observations, cancellationToken);
        }

        return new DocumentEmbeddedInspectionResult(observations);
    }

    private void InspectOne(
        DocumentEmbeddedObject embeddedObject,
        int depth,
        DocumentSecurityInspectionBudget budget,
        List<DocumentSecurityObservation> observations,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        budget.ValidateContainerDepth(depth);
        budget.ConsumeWork();
        budget.ConsumeEmbeddedObject(embeddedObject.Content.Length);

        if (embeddedObject.CompressedLength is long compressedLength)
        {
            budget.ValidateCompressionRatio(compressedLength, embeddedObject.Content.Length);
        }

        DocumentEmbeddedObjectClassification classification = classifier.Classify(embeddedObject);

        AddObservation(
            observations,
            budget,
            classification.IsExecutable
                ? DocumentSecurityObservationCategory.ExecutableContent
                : DocumentSecurityObservationCategory.EmbeddedContent,
            classification.IsExecutable ? "EmbeddedExecutableContent" : "EmbeddedObject",
            classification.IsExecutable
                ? $"Embedded content at '{embeddedObject.LogicalPath}' is executable or script content."
                : $"Embedded content at '{embeddedObject.LogicalPath}' was classified as {classification.FormatIdentifier}.",
            classification,
            embeddedObject);

        if (!classification.IsContainer)
            return;

        IDocumentContainerAdapter? adapter =
            adapters.FirstOrDefault(item => item.CanExpand(classification));

        if (adapter is null)
            return;

        foreach (DocumentContainerEntry child in adapter.Expand(embeddedObject, cancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();

            DocumentContainerEntry entry = child;

            if (entry.EmbeddedObject.CompressedLength is null)
            {
                entry = new DocumentContainerEntry(
                    new DocumentEmbeddedObject(
                        entry.EmbeddedObject.Content,
                        entry.EmbeddedObject.LogicalPath,
                        entry.CompressedLength,
                        entry.EmbeddedObject.DeclaredMediaType,
                        entry.EmbeddedObject.DeclaredFileName),
                    entry.CompressedLength);
            }

            InspectOne(
                entry.EmbeddedObject,
                checked(depth + 1),
                budget,
                observations,
                cancellationToken);
        }
    }

    private static void AddObservation(
        List<DocumentSecurityObservation> observations,
        DocumentSecurityInspectionBudget budget,
        DocumentSecurityObservationCategory category,
        string kind,
        string description,
        DocumentEmbeddedObjectClassification classification,
        DocumentEmbeddedObject embeddedObject)
    {
        budget.ConsumeObservation();

        observations.Add(
            new DocumentSecurityObservation(
                category,
                kind,
                description,
                classification.FormatIdentifier,
                new DocumentSecuritySourceLocation
                {
                    LogicalPath = embeddedObject.LogicalPath
                },
                new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    ["Classification"] = classification.Kind.ToString(),
                    ["Format"] = classification.FormatIdentifier,
                    ["Container"] = classification.IsContainer.ToString(),
                    ["Executable"] = classification.IsExecutable.ToString(),
                    ["DeclaredFileName"] = embeddedObject.DeclaredFileName ?? string.Empty,
                    ["DeclaredMediaType"] = embeddedObject.DeclaredMediaType ?? string.Empty
                }));
    }
}
