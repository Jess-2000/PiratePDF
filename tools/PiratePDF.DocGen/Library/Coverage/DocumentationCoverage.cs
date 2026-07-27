namespace PiratePDF.DocGen.Library.Coverage;

internal sealed class DocumentationCoverage
{
    private readonly DocumentationDocument[] documents;

    public DocumentationCoverage(
        DocumentationDocument[] documents)
    {
        ArgumentNullException.ThrowIfNull(documents);

        this.documents = documents;
    }

    // =========================================================================
    // Coverage Snapshot
    // =========================================================================

    public DocumentationCoverageSnapshot CreateSnapshot()
    {
        return new DocumentationCoverageSnapshot(
            documents.Length,
            CountDocumentsWithDocumentId(),
            CountDocumentsWithVersion(),
            CountDocumentsWithStatus(),
            CountDocumentsWithOwner(),
            CountDocumentsWithLastUpdated(),
            CountDocumentsWithNextReview(),
            CountDocumentsWithRevisionHistory(),
            CountDocumentsWithRelatedDocuments(),
            CountDocumentsWithRelatedWorkItems());
    }

    // =========================================================================
    // Metadata Coverage
    // =========================================================================

    public int CountDocumentsWithDocumentId()
    {
        return documents.Count(
            document =>
                !string.IsNullOrWhiteSpace(
                    document.DocumentId));
    }

    public int CountDocumentsWithVersion()
    {
        return documents.Count(
            document =>
                !string.IsNullOrWhiteSpace(
                    document.Version));
    }

    public int CountDocumentsWithStatus()
    {
        return documents.Count(
            document =>
                !string.IsNullOrWhiteSpace(
                    document.Status));
    }

    public int CountDocumentsWithOwner()
    {
        return documents.Count(
            document =>
                !string.IsNullOrWhiteSpace(
                    document.Owner));
    }

    public int CountDocumentsWithLastUpdated()
    {
        return documents.Count(
            document =>
                !string.IsNullOrWhiteSpace(
                    document.LastUpdated));
    }

    public int CountDocumentsWithNextReview()
    {
        return documents.Count(
            document =>
                !string.IsNullOrWhiteSpace(
                    document.NextReview));
    }

    // =========================================================================
    // Structural Coverage
    // =========================================================================

    public int CountDocumentsWithRevisionHistory()
    {
        return documents.Count(
            document =>
                document.RevisionHistory.Count > 0);
    }

    public int CountDocumentsWithRelatedDocuments()
    {
        return documents.Count(
            document =>
                document.RelatedDocumentIds.Count > 0);
    }

    public int CountDocumentsWithRelatedWorkItems()
    {
        return documents.Count(
            document =>
                document.RelatedWorkItems.Count > 0);
    }

    public int CountDocumentsWithSection(
        string heading)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(heading);

        return documents.Count(
            document =>
                document.HasSection(heading));
    }
}