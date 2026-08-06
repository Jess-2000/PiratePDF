namespace PiratePDF.DocGen.Library.Statistics;

internal sealed class DocumentationStatistics
{
    private readonly DocumentationDocument[] documents;

    public DocumentationStatistics(
        DocumentationDocument[] documents)
    {
        ArgumentNullException.ThrowIfNull(documents);

        this.documents = documents;
    }

    // =========================================================================
    // General Statistics
    // =========================================================================

    public int DocumentCount =>
        documents.Length;

    public int SectionCount =>
        documents.Sum(
            document => document.Sections.Count);

    public int RevisionEntryCount =>
        documents.Sum(
            document => document.RevisionHistory.Count);

    public int RelatedDocumentReferenceCount =>
        documents.Sum(
            document => document.RelatedDocumentIds.Count);

    public int RelatedWorkItemReferenceCount =>
        documents.Sum(
            document => document.RelatedWorkItems.Count);

    // =========================================================================
    // Grouped Statistics
    // =========================================================================

    public IReadOnlyDictionary<string, int> GetCategoryCounts()
    {
        return CreateCounts(
            documents.Select(
                document => document.Category));
    }

    public IReadOnlyDictionary<string, int> GetStatusCounts()
    {
        return CreateCounts(
            documents.Select(
                document => document.Status));
    }

    public IReadOnlyDictionary<string, int> GetOwnerCounts()
    {
        return CreateCounts(
            documents.Select(
                document => document.Owner));
    }

    public IReadOnlyDictionary<string, int> GetWorkItemCounts()
    {
        return CreateCounts(
            documents.SelectMany(
                document => document.RelatedWorkItems));
    }

    // =========================================================================
    // Helpers
    // =========================================================================

    private static Dictionary<string, int> CreateCounts(
        IEnumerable<string> values)
    {
        ArgumentNullException.ThrowIfNull(values);

        return values
            .Select(value =>
                NormalizeGroupingValue(
                    value,
                    "(missing)"))
            .GroupBy(
                value => value,
                StringComparer.OrdinalIgnoreCase)
            .OrderBy(
                group => group.Key,
                StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                group => group.Key,
                group => group.Count(),
                StringComparer.OrdinalIgnoreCase);
    }

    private static string NormalizeGroupingValue(
        string value,
        string fallback)
    {
        return string.IsNullOrWhiteSpace(value)
            ? fallback
            : value.Trim();
    }
}