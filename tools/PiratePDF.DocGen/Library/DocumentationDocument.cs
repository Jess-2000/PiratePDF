using PiratePDF.DocGen.Models;

namespace PiratePDF.DocGen.Library;

internal sealed class DocumentationDocument
{
    private readonly DocumentationSection[] sections;
    private readonly string[] relatedDocumentIds;
    private readonly string[] relatedWorkItems;
    private readonly RevisionHistoryEntry[] revisionHistory;

    public DocumentationDocument(
        DocumentMetadata metadata,
        string rawMarkdown,
        IEnumerable<DocumentationSection> sections,
        IEnumerable<string> relatedDocumentIds,
        IEnumerable<string> relatedWorkItems,
        IEnumerable<RevisionHistoryEntry> revisionHistory)
    {
        ArgumentNullException.ThrowIfNull(metadata);
        ArgumentNullException.ThrowIfNull(rawMarkdown);
        ArgumentNullException.ThrowIfNull(sections);
        ArgumentNullException.ThrowIfNull(relatedDocumentIds);
        ArgumentNullException.ThrowIfNull(relatedWorkItems);
        ArgumentNullException.ThrowIfNull(revisionHistory);

        Metadata = metadata;
        RawMarkdown = rawMarkdown;

        this.sections = sections
            .OrderBy(section => section.StartLine)
            .ToArray();

        this.relatedDocumentIds = relatedDocumentIds
            .Where(value =>
                !string.IsNullOrWhiteSpace(value))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(
                value => value,
                StringComparer.OrdinalIgnoreCase)
            .ToArray();

        this.relatedWorkItems = relatedWorkItems
            .Where(value =>
                !string.IsNullOrWhiteSpace(value))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(
                value => value,
                StringComparer.OrdinalIgnoreCase)
            .ToArray();

        this.revisionHistory = revisionHistory
            .ToArray();
    }

    // =========================================================================
    // Metadata
    // =========================================================================

    public DocumentMetadata Metadata { get; }

    // Set only after the approved original/policy hashes and complete registration validate.
    internal string? MetadataSupplementPath { get; init; }

    public string FilePath =>
        Metadata.FilePath;

    public string Title =>
        Metadata.Title;

    public string DocumentId =>
        Metadata.DocumentId;

    public string Category =>
        Metadata.Category;

    public string Version =>
        Metadata.Version;

    public string Status =>
        Metadata.Status;

    public string Owner =>
        Metadata.Owner;

    public string LastUpdated =>
        Metadata.LastUpdated;

    public string NextReview =>
        Metadata.NextReview;

    // =========================================================================
    // Parsed Document Content
    // =========================================================================

    public string RawMarkdown { get; }

    public IReadOnlyList<DocumentationSection> Sections =>
        sections;

    public IReadOnlyList<string> RelatedDocumentIds =>
        relatedDocumentIds;

    public IReadOnlyList<string> RelatedWorkItems =>
        relatedWorkItems;

    public IReadOnlyList<RevisionHistoryEntry> RevisionHistory =>
        revisionHistory;

    // =========================================================================
    // Queries
    // =========================================================================

    public DocumentationSection? FindSection(
        string heading)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(heading);

        return sections.FirstOrDefault(
            section => string.Equals(
                section.Heading,
                heading,
                StringComparison.OrdinalIgnoreCase));
    }

    public bool HasSection(
        string heading)
    {
        return FindSection(heading) is not null;
    }

    public bool ReferencesDocument(
        string documentId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(documentId);

        return relatedDocumentIds.Contains(
            documentId,
            StringComparer.OrdinalIgnoreCase);
    }

    public bool ReferencesWorkItem(
        string workItem)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(workItem);

        return relatedWorkItems.Contains(
            workItem,
            StringComparer.OrdinalIgnoreCase);
    }
}
