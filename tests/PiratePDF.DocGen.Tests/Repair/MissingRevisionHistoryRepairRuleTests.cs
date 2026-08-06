using PiratePDF.DocGen.Changes;
using PiratePDF.DocGen.Repair.Diagnostics;
using PiratePDF.DocGen.Repair.Rules;
using PiratePDF.DocGen.Tests.TestInfrastructure;

namespace PiratePDF.DocGen.Tests.Repair;

public sealed class MissingRevisionHistoryRepairRuleTests
{
    [Fact]
    public void Analyze_WithRevisionHistory_ProposesNoChange()
    {
        using var directory = new TemporaryDirectory();

        var library = DocumentationTestFactory.LoadLibrary(
            directory,
            new DocumentationTestFactory.TestDocument(
                "Healthy.md",
                DocumentationTestFactory.CreateDocument(
                    includeRevisionHistory: true)));

        var document = Assert.Single(
            library.AllDocuments);

        var context = new RepairRuleContext(
            library,
            document);

        var changes = new List<DocumentChange>();

        var diagnostics =
            new List<RepairDiagnostic>();

        var rule =
            new MissingRevisionHistoryRepairRule();

        rule.Analyze(
            context,
            changes,
            diagnostics);

        Assert.Empty(changes);
        Assert.Empty(diagnostics);
    }

    [Fact]
    public void Analyze_WithoutRevisionHistory_ProposesInsertion()
    {
        using var directory = new TemporaryDirectory();

        var library = DocumentationTestFactory.LoadLibrary(
            directory,
            new DocumentationTestFactory.TestDocument(
                "Missing.md",
                DocumentationTestFactory.CreateDocument(
                    version: "2.0",
                    includeRevisionHistory: false)));

        var document = Assert.Single(
            library.AllDocuments);

        var context = new RepairRuleContext(
            library,
            document);

        var changes = new List<DocumentChange>();

        var diagnostics =
            new List<RepairDiagnostic>();

        var rule =
            new MissingRevisionHistoryRepairRule();

        rule.Analyze(
            context,
            changes,
            diagnostics);

        var change = Assert.Single(changes);

        Assert.Equal(
            DocumentChangeType.Insert,
            change.ChangeType);

        Assert.Equal(
            document.RawMarkdown.Length,
            change.StartIndex);

        Assert.Contains(
            "## Revision History",
            change.ReplacementText,
            StringComparison.Ordinal);

        Assert.Contains(
            "| 2.0 |",
            change.ReplacementText,
            StringComparison.Ordinal);

        Assert.True(
            change.ReplacementText.EndsWith('\n'));

        Assert.Empty(diagnostics);
    }

    [Fact]
    public void Analyze_WithWindowsNewlines_PreservesNewlineStyle()
    {
        using var directory = new TemporaryDirectory();

        var content = DocumentationTestFactory
            .CreateDocument(
                includeRevisionHistory: false)
            .Replace(
                "\n",
                "\r\n",
                StringComparison.Ordinal);

        var library = DocumentationTestFactory.LoadLibrary(
            directory,
            new DocumentationTestFactory.TestDocument(
                "Windows.md",
                content));

        var document = Assert.Single(
            library.AllDocuments);

        var context = new RepairRuleContext(
            library,
            document);

        var changes = new List<DocumentChange>();

        var diagnostics =
            new List<RepairDiagnostic>();

        var rule =
            new MissingRevisionHistoryRepairRule();

        rule.Analyze(
            context,
            changes,
            diagnostics);

        var change = Assert.Single(changes);

        Assert.Contains(
            "\r\n## Revision History\r\n",
            change.ReplacementText,
            StringComparison.Ordinal);

        var withoutWindowsNewlines =
            change.ReplacementText.Replace(
                "\r\n",
                string.Empty,
                StringComparison.Ordinal);

        Assert.DoesNotContain(
            "\n## Revision History\n",
            withoutWindowsNewlines,
            StringComparison.Ordinal);

        Assert.Empty(diagnostics);
    }
}
