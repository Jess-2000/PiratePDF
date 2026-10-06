namespace PiratePDF.DocGen.Models;

internal static class DocumentationContract
{
    public const string DocumentId = "Document ID";
    public const string Category = "Category";
    public const string Version = "Version";
    public const string Status = "Status";
    public const string Owner = "Owner";
    public const string RelatedWorkItems =
        "Related BF Work Items";
    public const string LastUpdated = "Last Updated";
    public const string NextReview = "Next Review";
    public const string RevisionHistoryHeading =
        "Revision History";

    public static DateOnly CalculateNextReview(
        DateOnly lastUpdated)
    {
        return lastUpdated.AddMonths(3);
    }
}
