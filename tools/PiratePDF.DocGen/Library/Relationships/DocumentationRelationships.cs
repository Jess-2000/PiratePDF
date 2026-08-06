using PiratePDF.DocGen.Library.Queries;

namespace PiratePDF.DocGen.Library.Relationships;

internal sealed class DocumentationRelationships
{
    private readonly DocumentationDocument[] documents;
    private readonly DocumentationQueries queries;

    public DocumentationRelationships(
        DocumentationDocument[] documents,
        DocumentationQueries queries)
    {
        ArgumentNullException.ThrowIfNull(documents);
        ArgumentNullException.ThrowIfNull(queries);

        this.documents = documents;
        this.queries = queries;
    }

    // =========================================================================
    // Related Documents
    // =========================================================================

    public DocumentationDocument[] GetRelatedDocuments(
        DocumentationDocument sourceDocument)
    {
        ArgumentNullException.ThrowIfNull(sourceDocument);

        var relatedDocuments =
            new List<DocumentationDocument>();

        foreach (var documentId in sourceDocument.RelatedDocumentIds)
        {
            var relatedDocument =
                queries.FindByDocumentId(documentId);

            if (relatedDocument is not null)
            {
                relatedDocuments.Add(relatedDocument);
            }
        }

        return relatedDocuments
            .DistinctBy(
                document => document.DocumentId,
                StringComparer.OrdinalIgnoreCase)
            .OrderBy(
                document => document.DocumentId,
                StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public DocumentationDocument[] FindDocumentsReferencing(
        string documentId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(documentId);

        return queries.FindReferencingDocument(documentId);
    }

    // =========================================================================
    // Missing Relationships
    // =========================================================================

    public string[] FindMissingRelatedDocumentIds()
    {
        var knownDocumentIds = documents
            .Select(document => document.DocumentId)
            .Where(documentId =>
                !string.IsNullOrWhiteSpace(documentId))
            .ToHashSet(
                StringComparer.OrdinalIgnoreCase);

        return documents
            .SelectMany(document =>
                document.RelatedDocumentIds)
            .Where(documentId =>
                !knownDocumentIds.Contains(documentId))
            .Distinct(
                StringComparer.OrdinalIgnoreCase)
            .OrderBy(
                documentId => documentId,
                StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public DocumentationDocument[] FindDocumentsWithMissingReferences()
    {
        var knownDocumentIds = documents
            .Select(document => document.DocumentId)
            .Where(documentId =>
                !string.IsNullOrWhiteSpace(documentId))
            .ToHashSet(
                StringComparer.OrdinalIgnoreCase);

        return documents
            .Where(document =>
                document.RelatedDocumentIds.Any(
                    documentId =>
                        !knownDocumentIds.Contains(documentId)))
            .ToArray();
    }

    // =========================================================================
    // Relationship Checks
    // =========================================================================

    public bool HasRelationship(
        string sourceDocumentId,
        string targetDocumentId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            sourceDocumentId);

        ArgumentException.ThrowIfNullOrWhiteSpace(
            targetDocumentId);

        var sourceDocument =
            queries.FindByDocumentId(sourceDocumentId);

        return sourceDocument?.ReferencesDocument(
            targetDocumentId)
            ?? false;
    }
}
