using System.Globalization;
using System.Text.RegularExpressions;
using PiratePDF.DocGen.Library;
using PiratePDF.DocGen.Models;

namespace PiratePDF.DocGen.Validation;

internal static partial class DocumentationValidator
{
    // =========================================================================
    // Public API
    // =========================================================================

    public static DocumentationValidationResult Validate(
        IReadOnlyCollection<DocumentMetadata> documents)
    {
        ArgumentNullException.ThrowIfNull(documents);

        var issues = new List<ValidationIssue>();

        // Validate each document independently before performing checks that
        // compare documents with one another.
        foreach (var document in documents)
        {
            ValidateDocument(document, issues);
        }

        ValidateDuplicateDocumentIds(documents, issues);

        return new DocumentationValidationResult(issues);
    }

    public static DocumentationValidationResult Validate(
        DocumentationLibrary library)
    {
        ArgumentNullException.ThrowIfNull(library);

        var issues = new List<ValidationIssue>();

        // Metadata rules validate the standardized document header.
        foreach (var document in library.AllDocuments)
        {
            ValidateDocument(document.Metadata, issues);
        }

        ValidateDuplicateDocumentIds(
            library.AllDocuments
                .Select(document => document.Metadata)
                .ToArray(),
            issues);

        // Structural rules inspect complete Markdown content and report malformed
        // constructs before they cause misleading section-level findings.
        foreach (var document in library.AllDocuments)
        {
            issues.AddRange(
                MarkdownStructureValidator.Validate(
                    document));

            if (document.MetadataSupplementPath is null && !document.HasSection(
                    DocumentationContract.RevisionHistoryHeading))
            {
                issues.Add(
                    new ValidationIssue(
                        ValidationSeverity.Error,
                        string.IsNullOrWhiteSpace(document.DocumentId)
                            ? "(missing)"
                            : document.DocumentId,
                        document.FilePath,
                        "Revision History section is missing."));
            }
        }

        return new DocumentationValidationResult(
            issues);
    }

    // =========================================================================
    // Document Validation
    // =========================================================================

    private static void ValidateDocument(
        DocumentMetadata document,
        List<ValidationIssue> issues)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(issues);

        ValidateRequiredMetadata(document, issues);
        ValidateStatus(document, issues);
        ValidateWorkItemReferences(document, issues);
        ValidateMetadataDates(document, issues);
    }

    private static void ValidateRequiredMetadata(
        DocumentMetadata document,
        List<ValidationIssue> issues)
    {
        ValidateRequiredField(
            document.Title,
            "Title",
            document,
            issues);

        ValidateRequiredField(
            document.DocumentId,
            DocumentationContract.DocumentId,
            document,
            issues);

        ValidateRequiredField(
            document.Category,
            DocumentationContract.Category,
            document,
            issues);

        ValidateRequiredField(
            document.Version,
            DocumentationContract.Version,
            document,
            issues);

        ValidateRequiredField(
            document.Status,
            DocumentationContract.Status,
            document,
            issues);

        ValidateRequiredField(
            document.Owner,
            DocumentationContract.Owner,
            document,
            issues);

        ValidateRequiredField(
            document.RelatedWorkItems,
            DocumentationContract.RelatedWorkItems,
            document,
            issues);

        ValidateRequiredField(
            document.LastUpdated,
            DocumentationContract.LastUpdated,
            document,
            issues);

        ValidateRequiredField(
            document.NextReview,
            DocumentationContract.NextReview,
            document,
            issues);
    }

    // =========================================================================
    // Field Validation
    // =========================================================================

    private static void ValidateRequiredField(
        string value,
        string fieldName,
        DocumentMetadata document,
        List<ValidationIssue> issues)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        issues.Add(
            new ValidationIssue(
                ValidationSeverity.Error,
                GetDisplayDocumentId(document),
                document.FilePath,
                $"{fieldName} is missing."));
    }

    private static void ValidateStatus(
        DocumentMetadata document,
        List<ValidationIssue> issues)
    {
        if (string.IsNullOrWhiteSpace(document.Status))
        {
            return;
        }

        if (DocumentStatuses.IsValid(document.Status))
        {
            return;
        }

        issues.Add(
            new ValidationIssue(
                ValidationSeverity.Error,
                GetDisplayDocumentId(document),
                document.FilePath,
                $"Unknown document status '{document.Status}'."));
    }

    private static void ValidateWorkItemReferences(
        DocumentMetadata document,
        List<ValidationIssue> issues)
    {
        if (string.IsNullOrWhiteSpace(document.RelatedWorkItems))
        {
            return;
        }

        var references = document.RelatedWorkItems.Split(
            [',', ';'],
            StringSplitOptions.RemoveEmptyEntries |
            StringSplitOptions.TrimEntries);

        if (references.Length == 0)
        {
            issues.Add(
                new ValidationIssue(
                    ValidationSeverity.Error,
                    GetDisplayDocumentId(document),
                    document.FilePath,
                    "Related BF Work Items does not contain a valid reference."));

            return;
        }

        foreach (var reference in references)
        {
            if (WorkItemPattern().IsMatch(reference))
            {
                continue;
            }

            issues.Add(
                new ValidationIssue(
                    ValidationSeverity.Error,
                    GetDisplayDocumentId(document),
                    document.FilePath,
                    $"Invalid BF work-item reference '{reference}'."));
        }
    }

    private static void ValidateMetadataDates(
        DocumentMetadata document,
        List<ValidationIssue> issues)
    {
        ValidateDate(
            document.LastUpdated,
            "Last Updated",
            document,
            issues);

        ValidateDate(
            document.NextReview,
            "Next Review",
            document,
            issues);
    }

    private static void ValidateDate(
        string value,
        string fieldName,
        DocumentMetadata document,
        List<ValidationIssue> issues)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        if (DateOnly.TryParseExact(
                value,
                "yyyy-MM-dd",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out _))
        {
            return;
        }

        issues.Add(
            new ValidationIssue(
                ValidationSeverity.Error,
                GetDisplayDocumentId(document),
                document.FilePath,
                $"{fieldName} must use the YYYY-MM-DD format."));
    }

    // =========================================================================
    // Cross-Document Validation
    // =========================================================================

    private static void ValidateDuplicateDocumentIds(
        IReadOnlyCollection<DocumentMetadata> documents,
        List<ValidationIssue> issues)
    {
        // Missing identifiers are reported by required-field validation and
        // should not be treated as one shared duplicate identifier.
        var duplicateGroups = documents
            .Where(document =>
                !string.IsNullOrWhiteSpace(document.DocumentId))
            .GroupBy(
                document => document.DocumentId,
                StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Count() > 1);

        foreach (var duplicateGroup in duplicateGroups)
        {
            foreach (var document in duplicateGroup)
            {
                issues.Add(
                    new ValidationIssue(
                        ValidationSeverity.Error,
                        document.DocumentId,
                        document.FilePath,
                        $"Duplicate Document ID '{document.DocumentId}'."));
            }
        }
    }

    // =========================================================================
    // Helpers
    // =========================================================================

    private static string GetDisplayDocumentId(
        DocumentMetadata document)
    {
        return string.IsNullOrWhiteSpace(document.DocumentId)
            ? "(missing)"
            : document.DocumentId;
    }

    [GeneratedRegex(
        @"^BF-\d{4,}$",
        RegexOptions.CultureInvariant)]
    private static partial Regex WorkItemPattern();
}
