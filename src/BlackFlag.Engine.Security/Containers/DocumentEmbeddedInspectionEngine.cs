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
    private readonly IDocumentContainerSecurityAdapter[] securityAdapters;

    public DocumentEmbeddedInspectionEngine(
        IDocumentEmbeddedObjectClassifier? classifier = null,
        IEnumerable<IDocumentContainerAdapter>? adapters = null,
        IEnumerable<IDocumentContainerSecurityAdapter>? securityAdapters = null)
    {
        this.classifier = classifier ?? new DocumentEmbeddedObjectClassifier();
        this.adapters = adapters?.ToArray() ?? [];
        this.securityAdapters = securityAdapters?.ToArray() ?? [];
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
            InspectOne(
                embeddedObject,
                depth: 1,
                isEmbeddedObject: true,
                budgetAccounted: false,
                budget,
                observations,
                cancellationToken);
        }

        return new DocumentEmbeddedInspectionResult(observations);
    }

    /// <summary>
    /// Inspects a root container without incorrectly reporting the root itself
    /// as embedded content. All recursively discovered children still use the
    /// same shared inspection budget.
    /// </summary>
    public DocumentEmbeddedInspectionResult InspectRoot(
        DocumentEmbeddedObject rootObject,
        DocumentSecurityInspectionBudget? budget = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(rootObject);

        budget ??= new DocumentSecurityInspectionBudget();
        var observations = new List<DocumentSecurityObservation>();

        InspectOne(
            rootObject,
            depth: 0,
            isEmbeddedObject: false,
            budgetAccounted: true,
            budget,
            observations,
            cancellationToken);

        return new DocumentEmbeddedInspectionResult(observations);
    }

    private void InspectOne(
        DocumentEmbeddedObject embeddedObject,
        int depth,
        bool isEmbeddedObject,
        bool budgetAccounted,
        DocumentSecurityInspectionBudget budget,
        List<DocumentSecurityObservation> observations,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        budget.ValidateContainerDepth(depth);
        budget.ConsumeWork();

        if (isEmbeddedObject && !budgetAccounted)
        {
            if (embeddedObject.CompressedLength is long compressedLength)
            {
                budget.ValidateCompressionRatio(
                    compressedLength,
                    embeddedObject.Content.Length);
            }

            budget.ConsumeEmbeddedObject(embeddedObject.Content.Length);
        }

        DocumentEmbeddedObjectClassification classification =
            classifier.Classify(embeddedObject);

        if (isEmbeddedObject)
        {
            AddClassificationObservation(
                observations,
                budget,
                classification,
                embeddedObject);
        }

        if (!classification.IsContainer)
        {
            return;
        }

        IDocumentContainerSecurityAdapter? securityAdapter =
            securityAdapters.FirstOrDefault(
                item => item.CanInspect(classification));

        if (securityAdapter is not null)
        {
            DocumentContainerSecurityInspection inspection =
                securityAdapter.Inspect(
                    embeddedObject,
                    budget,
                    cancellationToken);

            foreach (DocumentSecurityObservation observation in inspection.Observations)
            {
                AddObservation(
                    observations,
                    budget,
                    observation);
            }

            foreach (DocumentContainerEntry child in inspection.EmbeddedObjects)
            {
                InspectChild(
                    child,
                    depth,
                    budget,
                    observations,
                    cancellationToken);
            }

            return;
        }

        IDocumentContainerAdapter? adapter =
            adapters.FirstOrDefault(
                item => item.CanExpand(classification));

        if (adapter is null)
        {
            return;
        }

        foreach (DocumentContainerEntry child in adapter.Expand(
                    embeddedObject,
                    cancellationToken))
        {
            InspectChild(
                child,
                depth,
                budget,
                observations,
                cancellationToken);
        }
    }

    private void InspectChild(
        DocumentContainerEntry child,
        int parentDepth,
        DocumentSecurityInspectionBudget budget,
        List<DocumentSecurityObservation> observations,
        CancellationToken cancellationToken)
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
                entry.CompressedLength,
                entry.BudgetAccounted);
        }

        InspectOne(
            entry.EmbeddedObject,
            checked(parentDepth + 1),
            isEmbeddedObject: true,
            entry.BudgetAccounted,
            budget,
            observations,
            cancellationToken);
    }

    private static void AddClassificationObservation(
        List<DocumentSecurityObservation> observations,
        DocumentSecurityInspectionBudget budget,
        DocumentEmbeddedObjectClassification classification,
        DocumentEmbeddedObject embeddedObject)
    {
        AddObservation(
            observations,
            budget,
            new DocumentSecurityObservation(
                classification.IsExecutable
                    ? DocumentSecurityObservationCategory.ExecutableContent
                    : DocumentSecurityObservationCategory.EmbeddedContent,
                classification.IsExecutable
                    ? "EmbeddedExecutableContent"
                    : "EmbeddedObject",
                classification.IsExecutable
                    ? $"Embedded content at '{embeddedObject.LogicalPath}' is executable or script content."
                    : $"Embedded content at '{embeddedObject.LogicalPath}' was classified as {classification.FormatIdentifier}.",
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

    private static void AddObservation(
        List<DocumentSecurityObservation> observations,
        DocumentSecurityInspectionBudget budget,
        DocumentSecurityObservation observation)
    {
        ArgumentNullException.ThrowIfNull(observation);
        budget.ConsumeObservation();
        observations.Add(observation);
    }
}
