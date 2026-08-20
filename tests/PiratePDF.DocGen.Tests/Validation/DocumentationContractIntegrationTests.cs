using PiratePDF.DocGen.Repair;
using PiratePDF.DocGen.Tests.TestInfrastructure;
using PiratePDF.DocGen.Validation;

namespace PiratePDF.DocGen.Tests.Validation;

public sealed class DocumentationContractIntegrationTests
{
    [Fact]
    public void CanonicalNewMilestoneIsValidationAndRepairClean()
    {
        using var directory =
            new TemporaryDirectory();
        var library =
            DocumentationTestFactory.LoadLibrary(
                directory,
                new DocumentationTestFactory.TestDocument(
                    "BF-9999A-Canonical-Milestone.md",
                    DocumentationTestFactory.CreateDocument(
                        documentId: "BF-9999A",
                        category: "Milestone",
                        status: "Implemented",
                        relatedWorkItems: "BF-9999",
                        lastUpdated: "2026-08-16",
                        nextReview: "2026-11-16")));

        DocumentationValidationResult validation =
            DocumentationValidator.Validate(library);
        DocumentationRepairPlan repair =
            DocumentationRepairPlanner.Plan(library);

        Assert.Empty(validation.Issues);
        Assert.False(validation.HasErrors);
        Assert.False(repair.HasChanges);
        Assert.Empty(repair.Diagnostics);
    }

    [Fact]
    public void MissingRevisionHistoryIsVisibleToValidationAndRepair()
    {
        using var directory =
            new TemporaryDirectory();
        var library =
            DocumentationTestFactory.LoadLibrary(
                directory,
                new DocumentationTestFactory.TestDocument(
                    "Missing-History.md",
                    DocumentationTestFactory.CreateDocument(
                        includeRevisionHistory: false)));

        DocumentationValidationResult validation =
            DocumentationValidator.Validate(library);
        DocumentationRepairPlan repair =
            DocumentationRepairPlanner.Plan(library);

        Assert.Contains(
            validation.Issues,
            issue =>
                issue.Severity ==
                    ValidationSeverity.Error &&
                issue.Message.Contains(
                    "Revision History",
                    StringComparison.Ordinal));
        Assert.Contains(
            repair.ChangeSet.Changes,
            change => change.ReplacementText.Contains(
                "## Revision History",
                StringComparison.Ordinal));
    }
}
