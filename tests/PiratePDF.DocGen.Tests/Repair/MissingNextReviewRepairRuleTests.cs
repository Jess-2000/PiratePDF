using PiratePDF.DocGen.Changes;
using PiratePDF.DocGen.Repair.Diagnostics;
using PiratePDF.DocGen.Repair.Rules;
using PiratePDF.DocGen.Tests.TestInfrastructure;

namespace PiratePDF.DocGen.Tests.Repair;

public sealed class MissingNextReviewRepairRuleTests
{
    [Fact]
    public void AnalyzeWithMissingNextReviewInsertsCanonicalDate()
    {
        using var directory =
            new TemporaryDirectory();

        string content = DocumentationTestFactory
            .CreateDocument(
                lastUpdated: "2026-08-16",
                nextReview: "2026-11-16")
            .Replace(
                "| **Next Review** | 2026-11-16 |\n",
                string.Empty,
                StringComparison.Ordinal);

        var library =
            DocumentationTestFactory.LoadLibrary(
                directory,
                new DocumentationTestFactory.TestDocument(
                    "Missing-Next-Review.md",
                    content));
        var document =
            Assert.Single(library.AllDocuments);
        var changes =
            new List<DocumentChange>();
        var diagnostics =
            new List<RepairDiagnostic>();

        new MissingNextReviewRepairRule().Analyze(
            new RepairRuleContext(
                library,
                document),
            changes,
            diagnostics);

        DocumentChange change =
            Assert.Single(changes);

        Assert.Equal(
            DocumentChangeType.Insert,
            change.ChangeType);
        Assert.Equal(
            "| **Next Review** | 2026-11-16 |\n",
            change.ReplacementText);
        Assert.Empty(diagnostics);
    }

    [Fact]
    public void AnalyzeWithExistingNextReviewProposesNoChange()
    {
        using var directory =
            new TemporaryDirectory();
        var library =
            DocumentationTestFactory.LoadLibrary(
                directory,
                new DocumentationTestFactory.TestDocument(
                    "Healthy.md",
                    DocumentationTestFactory.CreateDocument()));
        var document =
            Assert.Single(library.AllDocuments);
        var changes =
            new List<DocumentChange>();
        var diagnostics =
            new List<RepairDiagnostic>();

        new MissingNextReviewRepairRule().Analyze(
            new RepairRuleContext(
                library,
                document),
            changes,
            diagnostics);

        Assert.Empty(changes);
        Assert.Empty(diagnostics);
    }
}
