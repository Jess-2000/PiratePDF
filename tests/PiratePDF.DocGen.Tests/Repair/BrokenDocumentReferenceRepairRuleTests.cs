using PiratePDF.DocGen.Changes;
using PiratePDF.DocGen.Repair.Diagnostics;
using PiratePDF.DocGen.Repair.Rules;
using PiratePDF.DocGen.Tests.TestInfrastructure;

namespace PiratePDF.DocGen.Tests.Repair;

public sealed class BrokenDocumentReferenceRepairRuleTests
{
    [Fact]
    public void Analyze_WithValidReference_ProposesNothing()
    {
        using var directory = new TemporaryDirectory();

        var library = DocumentationTestFactory.LoadLibrary(
            directory,
            new DocumentationTestFactory.TestDocument(
                "First.md",
                DocumentationTestFactory.CreateDocument(
                    documentId: "PP-TEST-701",
                    body:
                        """
                        ## Related Documents

                        - PP-TEST-702
                        """)),

            new DocumentationTestFactory.TestDocument(
                "Second.md",
                DocumentationTestFactory.CreateDocument(
                    documentId: "PP-TEST-702")));

        var document = library.Documents.FindByDocumentId(
            "PP-TEST-701");

        Assert.NotNull(document);

        var changes = new List<DocumentChange>();

        var diagnostics =
            new List<RepairDiagnostic>();

        var rule =
            new BrokenDocumentReferenceRepairRule();

        rule.Analyze(
            new RepairRuleContext(
                library,
                document),
            changes,
            diagnostics);

        Assert.Empty(changes);
        Assert.Empty(diagnostics);
    }

    [Fact]
    public void Analyze_WithUnregisteredReference_RequiresManualReview()
    {
        using var directory = new TemporaryDirectory();

        var library = DocumentationTestFactory.LoadLibrary(
            directory,
            new DocumentationTestFactory.TestDocument(
                "Missing.md",
                DocumentationTestFactory.CreateDocument(
                    documentId: "PP-TEST-703",
                    body:
                        """
                        ## Related Documents

                        - PP-NOTFOUND-999
                        """)));

        var document = Assert.Single(
            library.AllDocuments);

        var changes = new List<DocumentChange>();

        var diagnostics =
            new List<RepairDiagnostic>();

        var rule =
            new BrokenDocumentReferenceRepairRule();

        rule.Analyze(
            new RepairRuleContext(
                library,
                document),
            changes,
            diagnostics);

        Assert.Empty(changes);

        var diagnostic = Assert.Single(
            diagnostics);

        Assert.Equal(
            RepairDiagnosticSeverity.Warning,
            diagnostic.Severity);

        Assert.Contains(
            "manual review",
            diagnostic.Message,
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Analyze_WithRegisteredRedirect_ProposesReplacement()
    {
        using var directory = new TemporaryDirectory();

        var library = DocumentationTestFactory.LoadLibrary(
            directory,
            new DocumentationTestFactory.TestDocument(
                "Source.md",
                DocumentationTestFactory.CreateDocument(
                    documentId: "PP-TEST-704",
                    body:
                        """
                        ## Related Documents

                        - PP-GOV-001
                        """)),

            new DocumentationTestFactory.TestDocument(
                "Replacement.md",
                DocumentationTestFactory.CreateDocument(
                    documentId: "PP-GOV-002",
                    title: "Documentation Standards")));

        var document = library.Documents.FindByDocumentId(
            "PP-TEST-704");

        Assert.NotNull(document);

        var changes = new List<DocumentChange>();

        var diagnostics =
            new List<RepairDiagnostic>();

        var rule =
            new BrokenDocumentReferenceRepairRule();

        rule.Analyze(
            new RepairRuleContext(
                library,
                document),
            changes,
            diagnostics);

        var change = Assert.Single(changes);

        Assert.Equal(
            DocumentChangeType.Replace,
            change.ChangeType);

        Assert.Equal(
            "PP-GOV-002",
            change.ReplacementText);

        var diagnostic = Assert.Single(
            diagnostics);

        Assert.Equal(
            RepairDiagnosticSeverity.Information,
            diagnostic.Severity);

        Assert.Contains(
            "PP-GOV-001",
            diagnostic.Message,
            StringComparison.Ordinal);

        Assert.Contains(
            "PP-GOV-002",
            diagnostic.Message,
            StringComparison.Ordinal);
    }
}