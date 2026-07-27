using PiratePDF.DocGen.Parsing;
using PiratePDF.DocGen.Tests.TestInfrastructure;

namespace PiratePDF.DocGen.Tests.Parsing;

public sealed class MarkdownMetadataParserTests
{
    [Fact]
    public void Parse_WithValidMetadata_ReturnsAllValues()
    {
        using var directory = new TemporaryDirectory();

        var filePath = directory.CreateFile(
            "Valid.md",
            DocumentationTestFactory.CreateDocument(
                documentId: "PP-SPEC-999",
                title: "Parser Test",
                category: "Technical Specification",
                version: "2.5",
                status: "In Development",
                owner: "PiratePDF Project",
                relatedWorkItems: "BF-1030, BF-2100",
                lastUpdated: "2026-07-25",
                nextReview: "2026-11-25"));

        var metadata = MarkdownMetadataParser.Parse(
            filePath);

        Assert.Equal(
            "PP-SPEC-999",
            metadata.DocumentId);

        Assert.Equal(
            "Parser Test",
            metadata.Title);

        Assert.Equal(
            "Technical Specification",
            metadata.Category);

        Assert.Equal(
            "2.5",
            metadata.Version);

        Assert.Equal(
            "In Development",
            metadata.Status);

        Assert.Equal(
            "PiratePDF Project",
            metadata.Owner);

        Assert.Equal(
            "BF-1030, BF-2100",
            metadata.RelatedWorkItems);

        Assert.Equal(
            "2026-07-25",
            metadata.LastUpdated);

        Assert.Equal(
            "2026-11-25",
            metadata.NextReview);
    }

    [Fact]
    public void Parse_WithLaterMarkdownTable_DoesNotOverwriteMetadata()
    {
        using var directory = new TemporaryDirectory();

        var filePath = directory.CreateFile(
            "Later-Table.md",
            DocumentationTestFactory.CreateDocument(
                documentId: "PP-GOV-999",
                status: "Approved",
                body:
                    """
                    ## Example

                    | Field | Value |
                    |---|---|
                    | **Document ID** | PP-WRONG-999 |
                    | **Status** | Rejected |
                    """));

        var metadata = MarkdownMetadataParser.Parse(
            filePath);

        Assert.Equal(
            "PP-GOV-999",
            metadata.DocumentId);

        Assert.Equal(
            "Approved",
            metadata.Status);
    }

    [Fact]
    public void Parse_WithoutLevelOneTitle_ReturnsEmptyTitleAndMetadata()
    {
        using var directory = new TemporaryDirectory();

        var filePath = directory.CreateFile(
            "No-Title.md",
            """
            ## Purpose

            This file has no level-one heading.
            """);

        var metadata = MarkdownMetadataParser.Parse(
            filePath);

        Assert.Equal(
            string.Empty,
            metadata.Title);

        Assert.Equal(
            string.Empty,
            metadata.DocumentId);

        Assert.Equal(
            string.Empty,
            metadata.Status);
    }

    [Fact]
    public void Parse_WithMetadataNotImmediatelyAfterTitle_IgnoresTable()
    {
        using var directory = new TemporaryDirectory();

        var filePath = directory.CreateFile(
            "Misplaced-Metadata.md",
            """
            # Misplaced Metadata

            This paragraph appears before the table.

            | Field | Value |
            |---|---|
            | **Document ID** | PP-TEST-998 |
            | **Status** | Approved |
            """);

        var metadata = MarkdownMetadataParser.Parse(
            filePath);

        Assert.Equal(
            "Misplaced Metadata",
            metadata.Title);

        Assert.Equal(
            string.Empty,
            metadata.DocumentId);

        Assert.Equal(
            string.Empty,
            metadata.Status);
    }
}