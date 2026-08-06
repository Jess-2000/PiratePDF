using PiratePDF.DocGen.Library.Coverage;
using PiratePDF.DocGen.Library.Queries;
using PiratePDF.DocGen.Library.Relationships;
using PiratePDF.DocGen.Library.Statistics;

namespace PiratePDF.DocGen.Library;

internal sealed class DocumentationLibrary
{
    private readonly DocumentationDocument[] allDocuments;

    public DocumentationLibrary(
        IEnumerable<DocumentationDocument> documents)
    {
        ArgumentNullException.ThrowIfNull(documents);

        allDocuments = documents
            .OrderBy(
                document => document.DocumentId,
                StringComparer.OrdinalIgnoreCase)
            .ThenBy(
                document => document.Title,
                StringComparer.OrdinalIgnoreCase)
            .ThenBy(
                document => document.FilePath,
                StringComparer.OrdinalIgnoreCase)
            .ToArray();

        Documents = new DocumentationQueries(
            allDocuments);

        Statistics = new DocumentationStatistics(
            allDocuments);

        Relationships = new DocumentationRelationships(
            allDocuments,
            Documents);

        Coverage = new DocumentationCoverage(
            allDocuments);
    }

    // =========================================================================
    // Document Collection
    // =========================================================================

    public IReadOnlyList<DocumentationDocument> AllDocuments =>
        allDocuments;

    public int Count =>
        allDocuments.Length;

    public bool IsEmpty =>
        allDocuments.Length == 0;

    // =========================================================================
    // Library Views
    // =========================================================================

    public DocumentationQueries Documents { get; }

    public DocumentationStatistics Statistics { get; }

    public DocumentationRelationships Relationships { get; }

    public DocumentationCoverage Coverage { get; }
}
