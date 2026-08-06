namespace PiratePDF.DocGen.Library.Queries;

internal sealed class DocumentationQueries
{
    private readonly DocumentationDocument[] documents;

    public DocumentationQueries(
        DocumentationDocument[] documents)
    {
        ArgumentNullException.ThrowIfNull(documents);

        this.documents = documents;
    }

    // =========================================================================
    // Single-Document Lookup
    // =========================================================================

    public DocumentationDocument? FindByDocumentId(
        string documentId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(documentId);

        return documents.FirstOrDefault(
            document => string.Equals(
                document.DocumentId,
                documentId,
                StringComparison.OrdinalIgnoreCase));
    }

    public DocumentationDocument? FindByPath(
        string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        var fullPath = Path.GetFullPath(filePath);

        return documents.FirstOrDefault(
            document => string.Equals(
                Path.GetFullPath(document.FilePath),
                fullPath,
                StringComparison.OrdinalIgnoreCase));
    }

    // =========================================================================
    // Multi-Document Lookup
    // =========================================================================

    public DocumentationDocument[] FindByCategory(
        string category)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(category);

        return documents
            .Where(document => string.Equals(
                document.Category,
                category,
                StringComparison.OrdinalIgnoreCase))
            .ToArray();
    }

    public DocumentationDocument[] FindByStatus(
        string status)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(status);

        return documents
            .Where(document => string.Equals(
                document.Status,
                status,
                StringComparison.OrdinalIgnoreCase))
            .ToArray();
    }

    public DocumentationDocument[] FindByOwner(
        string owner)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(owner);

        return documents
            .Where(document => string.Equals(
                document.Owner,
                owner,
                StringComparison.OrdinalIgnoreCase))
            .ToArray();
    }

    public DocumentationDocument[] FindByTitle(
        string titleFragment)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(titleFragment);

        return documents
            .Where(document => document.Title.Contains(
                titleFragment,
                StringComparison.OrdinalIgnoreCase))
            .ToArray();
    }

    public DocumentationDocument[] FindByWorkItem(
        string workItem)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(workItem);

        return documents
            .Where(document =>
                document.ReferencesWorkItem(workItem))
            .ToArray();
    }

    public DocumentationDocument[] FindReferencingDocument(
        string documentId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(documentId);

        return documents
            .Where(document =>
                document.ReferencesDocument(documentId))
            .ToArray();
    }
}
