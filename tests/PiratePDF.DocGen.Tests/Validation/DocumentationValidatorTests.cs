using PiratePDF.DocGen.Models;
using PiratePDF.DocGen.Tests.TestInfrastructure;
using PiratePDF.DocGen.Validation;

namespace PiratePDF.DocGen.Tests.Validation;

public sealed class DocumentationValidatorTests
{
    [Fact]
    public void Validate_WithHealthyLibrary_ReturnsNoIssues()
    {
        using var directory = new TemporaryDirectory();

        var library = DocumentationTestFactory.LoadLibrary(
            directory,
            new DocumentationTestFactory.TestDocument(
                "Healthy.md",
                DocumentationTestFactory.CreateDocument()));

        var result = DocumentationValidator.Validate(
            library);

        Assert.False(result.HasErrors);
        Assert.Equal(0, result.ErrorCount);
        Assert.Equal(0, result.WarningCount);
        Assert.Equal(0, result.InformationCount);
        Assert.Empty(result.Issues);
    }

    [Theory]
    [MemberData(nameof(GetCanonicalStatuses))]
    public void Validate_WithCanonicalStatus_ReturnsNoStatusIssue(
        string status)
    {
        using var directory = new TemporaryDirectory();

        var library = DocumentationTestFactory.LoadLibrary(
            directory,
            new DocumentationTestFactory.TestDocument(
                "Canonical-Status.md",
                DocumentationTestFactory.CreateDocument(
                    status: status)));

        var result = DocumentationValidator.Validate(
            library);

        Assert.DoesNotContain(
            result.Issues,
            issue => issue.Message.Contains(
                "Unknown document status",
                StringComparison.Ordinal));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("Release Candidate")]
    [InlineData("released")]
    public void IsValid_WithNonCanonicalValue_ReturnsFalse(
        string? status)
    {
        Assert.False(
            DocumentStatuses.IsValid(status));
    }

    [Fact]
    public void GetAll_ReturnsTheCanonicalStatusCollection()
    {
        Assert.Equal(
            DocumentStatuses.All,
            DocumentStatuses.GetAll());
    }

    [Fact]
    public void Validate_WithDuplicateDocumentIds_ReportsBothFiles()
    {
        using var directory = new TemporaryDirectory();

        var library = DocumentationTestFactory.LoadLibrary(
            directory,
            new DocumentationTestFactory.TestDocument(
                "First.md",
                DocumentationTestFactory.CreateDocument(
                    documentId: "PP-TEST-777",
                    title: "First Document")),

            new DocumentationTestFactory.TestDocument(
                "Second.md",
                DocumentationTestFactory.CreateDocument(
                    documentId: "PP-TEST-777",
                    title: "Second Document")));

        var result = DocumentationValidator.Validate(
            library);

        var duplicateIssues = result.Issues
            .Where(issue => issue.Message.Contains(
                "Duplicate Document ID",
                StringComparison.Ordinal))
            .ToArray();

        Assert.Equal(
            2,
            duplicateIssues.Length);

        Assert.All(
            duplicateIssues,
            issue => Assert.Equal(
                ValidationSeverity.Error,
                issue.Severity));
    }

    [Fact]
    public void Validate_WithInvalidMetadata_ReportsExpectedErrors()
    {
        using var directory = new TemporaryDirectory();

        var library = DocumentationTestFactory.LoadLibrary(
            directory,
            new DocumentationTestFactory.TestDocument(
                "Invalid.md",
                DocumentationTestFactory.CreateDocument(
                    status: "Unknown",
                    relatedWorkItems: "INVALID-1030",
                    lastUpdated: "07/25/2026",
                    nextReview: "October 25, 2026")));

        var result = DocumentationValidator.Validate(
            library);

        Assert.Contains(
            result.Issues,
            issue => issue.Message.Contains(
                "Unknown document status",
                StringComparison.Ordinal));

        Assert.Contains(
            result.Issues,
            issue => issue.Message.Contains(
                "Invalid BF work-item reference",
                StringComparison.Ordinal));

        Assert.Contains(
            result.Issues,
            issue => issue.Message.Contains(
                "Last Updated must use",
                StringComparison.Ordinal));

        Assert.Contains(
            result.Issues,
            issue => issue.Message.Contains(
                "Next Review must use",
                StringComparison.Ordinal));
    }

    [Fact]
    public void Validate_WithUnclosedFence_ReportsStructuralError()
    {
        using var directory = new TemporaryDirectory();

        var library = DocumentationTestFactory.LoadLibrary(
            directory,
            new DocumentationTestFactory.TestDocument(
                "Unclosed.md",
                DocumentationTestFactory.CreateDocument(
                    body:
                        """
                        ## Example

                        ```text
                        Unclosed example.
                        """,
                    includeRevisionHistory: false)));

        var result = DocumentationValidator.Validate(
            library);

        Assert.Contains(
            result.Issues,
            issue =>
                issue.Severity == ValidationSeverity.Error &&
                issue.Message.Contains(
                    "Unclosed fenced code block",
                    StringComparison.Ordinal));
    }

    public static TheoryData<string> GetCanonicalStatuses()
    {
        var statuses = new TheoryData<string>();

        foreach (var status in DocumentStatuses.GetAll())
        {
            statuses.Add(status);
        }

        return statuses;
    }
}
