using PiratePDF.DocGen.Parsing;
using PiratePDF.DocGen.Tests.TestInfrastructure;
using PiratePDF.DocGen.Validation;

namespace PiratePDF.DocGen.Tests.Validation;

public sealed class MarkdownStructureValidatorTests
{
    [Fact]
    public void Validate_WithBalancedBacktickFence_ReturnsNoIssues()
    {
        using var directory = new TemporaryDirectory();

        var filePath = directory.CreateFile(
            "Balanced.md",
            CreateDocument(
                """
                ## Example

                ```csharp
                Console.WriteLine("PiratePDF");
                ```

                ## Revision History

                | Version | Date | Description |
                |---|---|---|
                | 1.0 | 2026-07-25 | Initial version. |
                """));

        var document = MarkdownDocumentParser.Parse(filePath);

        var issues = MarkdownStructureValidator.Validate(
            document);

        Assert.Empty(issues);
    }

    [Fact]
    public void Validate_WithBalancedTildeFence_ReturnsNoIssues()
    {
        using var directory = new TemporaryDirectory();

        var filePath = directory.CreateFile(
            "Balanced-Tilde.md",
            CreateDocument(
                """
                ## Example

                ~~~~powershell
                dotnet build
                ~~~~

                ## Revision History

                | Version | Date | Description |
                |---|---|---|
                | 1.0 | 2026-07-25 | Initial version. |
                """));

        var document = MarkdownDocumentParser.Parse(filePath);

        var issues = MarkdownStructureValidator.Validate(
            document);

        Assert.Empty(issues);
    }

    [Fact]
    public void Validate_WithUnclosedFence_ReturnsLineSpecificError()
    {
        using var directory = new TemporaryDirectory();

        var filePath = directory.CreateFile(
            "Unclosed.md",
            CreateDocument(
                """
                ## Example

                ```csharp
                Console.WriteLine("PiratePDF");
                """));

        var document = MarkdownDocumentParser.Parse(filePath);

        var issue = Assert.Single(
            MarkdownStructureValidator.Validate(document));

        Assert.Equal(
            ValidationSeverity.Error,
            issue.Severity);

        Assert.Contains(
            "Unclosed fenced code block",
            issue.Message,
            StringComparison.Ordinal);

        var expectedLineNumber = File
            .ReadAllLines(filePath)
            .Select((line, index) => new
            {
                Line = line,
                LineNumber = index + 1
            })
        .Single(item => item.Line == "```csharp")
        .LineNumber;

        Assert.Contains(
            $"line {expectedLineNumber}",
            issue.Message,
            StringComparison.Ordinal);

        Assert.Contains(
            "Manual review is required",
            issue.Message,
            StringComparison.Ordinal);
    }

    [Fact]
    public void Validate_WithDifferentClosingMarker_ReturnsError()
    {
        using var directory = new TemporaryDirectory();

        var filePath = directory.CreateFile(
            "Wrong-Closing-Marker.md",
            CreateDocument(
                """
                ## Example

                ```text
                PiratePDF
                ~~~
                """));

        var document = MarkdownDocumentParser.Parse(filePath);

        var issue = Assert.Single(
            MarkdownStructureValidator.Validate(document));

        Assert.Equal(
            ValidationSeverity.Error,
            issue.Severity);
    }

    private static string CreateDocument(
        string body)
    {
        return
            """
            # Test Document

            | Field | Value |
            |---|---|
            | **Document ID** | PP-TEST-900 |
            | **Category** | Testing Documentation |
            | **Version** | 1.0 |
            | **Status** | Approved |
            | **Owner** | PiratePDF Project |
            | **Related BF Work Items** | BF-1030 |
            | **Last Updated** | 2026-07-25 |
            | **Next Review** | 2026-10-25 |

            ---

            """ +
            body +
            Environment.NewLine;
    }
}
