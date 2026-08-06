namespace PiratePDF.DocGen.Library.Coverage;

internal sealed record DocumentationCoverageSnapshot(
    int TotalDocuments,
    int DocumentsWithDocumentId,
    int DocumentsWithVersion,
    int DocumentsWithStatus,
    int DocumentsWithOwner,
    int DocumentsWithLastUpdated,
    int DocumentsWithNextReview,
    int DocumentsWithRevisionHistory,
    int DocumentsWithRelatedDocuments,
    int DocumentsWithRelatedWorkItems)
{
    public double DocumentIdPercentage =>
        CalculatePercentage(DocumentsWithDocumentId);

    public double VersionPercentage =>
        CalculatePercentage(DocumentsWithVersion);

    public double StatusPercentage =>
        CalculatePercentage(DocumentsWithStatus);

    public double OwnerPercentage =>
        CalculatePercentage(DocumentsWithOwner);

    public double LastUpdatedPercentage =>
        CalculatePercentage(DocumentsWithLastUpdated);

    public double NextReviewPercentage =>
        CalculatePercentage(DocumentsWithNextReview);

    public double RevisionHistoryPercentage =>
        CalculatePercentage(DocumentsWithRevisionHistory);

    public double RelatedDocumentsPercentage =>
        CalculatePercentage(DocumentsWithRelatedDocuments);

    public double RelatedWorkItemsPercentage =>
        CalculatePercentage(DocumentsWithRelatedWorkItems);

    private double CalculatePercentage(
        int coveredDocuments)
    {
        if (TotalDocuments == 0)
        {
            return 100.0;
        }

        return coveredDocuments * 100.0 / TotalDocuments;
    }
}