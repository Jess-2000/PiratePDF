using PiratePDF.DocGen.Library.Coverage;

namespace PiratePDF.DocGen.Health;

internal sealed record DocumentationHealthSnapshot(
    int DocumentCount,
    int SectionCount,
    int RevisionEntryCount,
    int RelatedDocumentReferenceCount,
    int RelatedWorkItemReferenceCount,
    IReadOnlyDictionary<string, int> CategoryCounts,
    IReadOnlyDictionary<string, int> StatusCounts,
    IReadOnlyDictionary<string, int> OwnerCounts,
    DocumentationCoverageSnapshot Coverage,
    IReadOnlyList<string> MissingRelatedDocumentIds,
    int ValidationErrorCount,
    int ValidationWarningCount,
    int ValidationInformationCount,
    double OverallScore,
    string Grade);
