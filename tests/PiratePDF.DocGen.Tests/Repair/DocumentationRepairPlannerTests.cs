using PiratePDF.DocGen.Infrastructure;
using PiratePDF.DocGen.Repair;
using PiratePDF.DocGen.Tests.TestInfrastructure;

namespace PiratePDF.DocGen.Tests.Repair;

public sealed class DocumentationRepairPlannerTests
{
    [Fact]
    public void Plan_WithHealthyDocument_ReturnsEmptyChangeSet()
    {
        using var directory = new TemporaryDirectory();

        directory.CreateFile(
            "Healthy.md",
            CreateDocument(
                status: "Approved",
                lastUpdated: "2026-07-25",
                nextReview: "2026-10-25",
                includeRevisionHistory: true,
                includeTrailingNewline: true));

        var library = DocumentationLibraryLoader.Load(
            directory.Path);

        var repairPlan =
            DocumentationRepairPlanner.Plan(library);

        Assert.True(
            repairPlan.ChangeSet.IsEmpty);

        Assert.Empty(
            repairPlan.Diagnostics);
    }

    [Fact]
    public void Plan_WithNonCanonicalStatusCasing_ProposesReplacement()
    {
        using var directory = new TemporaryDirectory();

        directory.CreateFile(
            "Status.md",
            CreateDocument(
                status: "approved",
                lastUpdated: "2026-07-25",
                nextReview: "2026-10-25",
                includeRevisionHistory: true,
                includeTrailingNewline: true));

        var library = DocumentationLibraryLoader.Load(
            directory.Path);

        var repairPlan =
            DocumentationRepairPlanner.Plan(library);

        var change = Assert.Single(
            repairPlan.ChangeSet.Changes);

        Assert.Equal(
            "Approved",
            change.ReplacementText);

        Assert.Contains(
            "canonical casing",
            change.Reason,
            StringComparison.Ordinal);

        Assert.Empty(
            repairPlan.Diagnostics);
    }

    [Fact]
    public void Plan_WithUnambiguousDate_ProposesIsoReplacement()
    {
        using var directory = new TemporaryDirectory();

        directory.CreateFile(
            "Date.md",
            CreateDocument(
                status: "Approved",
                lastUpdated: "July 25, 2026",
                nextReview: "2026-10-25",
                includeRevisionHistory: true,
                includeTrailingNewline: true));

        var library = DocumentationLibraryLoader.Load(
            directory.Path);

        var repairPlan =
            DocumentationRepairPlanner.Plan(library);

        var change = Assert.Single(
            repairPlan.ChangeSet.Changes);

        Assert.Equal(
            "2026-07-25",
            change.ReplacementText);

        Assert.Empty(
            repairPlan.Diagnostics);
    }

    [Fact]
    public void Plan_WithMissingRevisionHistory_ProposesInsertion()
    {
        using var directory = new TemporaryDirectory();

        directory.CreateFile(
            "Revision.md",
            CreateDocument(
                status: "Approved",
                lastUpdated: "2026-07-25",
                nextReview: "2026-10-25",
                includeRevisionHistory: false,
                includeTrailingNewline: true));

        var library = DocumentationLibraryLoader.Load(
            directory.Path);

        var repairPlan =
            DocumentationRepairPlanner.Plan(library);

        var change = Assert.Single(
            repairPlan.ChangeSet.Changes);

        Assert.Contains(
            "## Revision History",
            change.ReplacementText,
            StringComparison.Ordinal);

        Assert.Empty(
            repairPlan.Diagnostics);
    }

    [Fact]
    public void Plan_WithMissingTrailingNewline_ProposesInsertion()
    {
        using var directory = new TemporaryDirectory();

        directory.CreateFile(
            "Newline.md",
            CreateDocument(
                status: "Approved",
                lastUpdated: "2026-07-25",
                nextReview: "2026-10-25",
                includeRevisionHistory: true,
                includeTrailingNewline: false));

        var library = DocumentationLibraryLoader.Load(
            directory.Path);

        var repairPlan =
            DocumentationRepairPlanner.Plan(library);

        var change = Assert.Single(
            repairPlan.ChangeSet.Changes);

        Assert.Equal(
            0,
            change.Length);

        Assert.Contains(
            "trailing newline",
            change.Reason,
            StringComparison.Ordinal);

        Assert.Empty(
            repairPlan.Diagnostics);
    }

    private static string CreateDocument(
        string status,
        string lastUpdated,
        string nextReview,
        bool includeRevisionHistory,
        bool includeTrailingNewline)
    {
        var content =
            $"""
             # Test Document

             | Field | Value |
             |---|---|
             | **Document ID** | PP-TEST-901 |
             | **Category** | Testing Documentation |
             | **Version** | 1.0 |
             | **Status** | {status} |
             | **Owner** | PiratePDF Project |
             | **Related BF Work Items** | BF-1030 |
             | **Last Updated** | {lastUpdated} |
             | **Next Review** | {nextReview} |

             ---

             ## Purpose

             Test document.
             """;

        if (includeRevisionHistory)
        {
            content +=
                """

                ## Revision History

                | Version | Date | Description |
                |---|---|---|
                | 1.0 | 2026-07-25 | Initial version. |
                """;
        }

        content = content.Replace(
            "\r\n",
            "\n",
            StringComparison.Ordinal);

        return includeTrailingNewline
            ? content.TrimEnd('\n') + "\n"
            : content.TrimEnd('\n');
    }
}
