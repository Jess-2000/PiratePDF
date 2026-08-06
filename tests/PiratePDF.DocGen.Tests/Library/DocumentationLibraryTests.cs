using PiratePDF.DocGen.Tests.TestInfrastructure;

namespace PiratePDF.DocGen.Tests.Library;

public sealed class DocumentationLibraryTests
{
    [Fact]
    public void Documents_FindByDocumentId_IsCaseInsensitive()
    {
        using var directory = new TemporaryDirectory();

        var library = DocumentationTestFactory.LoadLibrary(
            directory,
            new DocumentationTestFactory.TestDocument(
                "Document.md",
                DocumentationTestFactory.CreateDocument(
                    documentId: "PP-SPEC-500")));

        var document = library.Documents.FindByDocumentId(
            "pp-spec-500");

        Assert.NotNull(document);

        Assert.Equal(
            "PP-SPEC-500",
            document.DocumentId);
    }

    [Fact]
    public void Documents_FindByWorkItem_ReturnsMatchingDocuments()
    {
        using var directory = new TemporaryDirectory();

        var library = DocumentationTestFactory.LoadLibrary(
            directory,
            new DocumentationTestFactory.TestDocument(
                "First.md",
                DocumentationTestFactory.CreateDocument(
                    documentId: "PP-TEST-501",
                    relatedWorkItems: "BF-1030, BF-2100")),

            new DocumentationTestFactory.TestDocument(
                "Second.md",
                DocumentationTestFactory.CreateDocument(
                    documentId: "PP-TEST-502",
                    relatedWorkItems: "BF-2200")));

        var documents = library.Documents.FindByWorkItem(
            "BF-2100");

        var document = Assert.Single(documents);

        Assert.Equal(
            "PP-TEST-501",
            document.DocumentId);
    }

    [Fact]
    public void Statistics_GetCategoryCounts_GroupsCaseInsensitively()
    {
        using var directory = new TemporaryDirectory();

        var library = DocumentationTestFactory.LoadLibrary(
            directory,
            new DocumentationTestFactory.TestDocument(
                "First.md",
                DocumentationTestFactory.CreateDocument(
                    documentId: "PP-TEST-503",
                    category: "Governance")),

            new DocumentationTestFactory.TestDocument(
                "Second.md",
                DocumentationTestFactory.CreateDocument(
                    documentId: "PP-TEST-504",
                    category: "governance")));

        var counts = library.Statistics.GetCategoryCounts();

        Assert.Single(counts);

        Assert.Equal(
            2,
            counts["Governance"]);
    }

    [Fact]
    public void Relationships_FindMissingRelatedDocumentIds_ReturnsUnknownIds()
    {
        using var directory = new TemporaryDirectory();

        var library = DocumentationTestFactory.LoadLibrary(
            directory,
            new DocumentationTestFactory.TestDocument(
                "Document.md",
                DocumentationTestFactory.CreateDocument(
                    documentId: "PP-TEST-505",
                    body:
                        """
                        ## Purpose

                        Test document.

                        ## Related Documents

                        - PP-TEST-999
                        """)));

        var missingIds =
            library.Relationships
                .FindMissingRelatedDocumentIds();

        var missingId = Assert.Single(
            missingIds);

        Assert.Equal(
            "PP-TEST-999",
            missingId);
    }

    [Fact]
    public void Coverage_CreateSnapshot_ReportsCompleteMetadata()
    {
        using var directory = new TemporaryDirectory();

        var library = DocumentationTestFactory.LoadLibrary(
            directory,
            new DocumentationTestFactory.TestDocument(
                "Document.md",
                DocumentationTestFactory.CreateDocument()));

        var snapshot =
            library.Coverage.CreateSnapshot();

        Assert.Equal(
            100.0,
            snapshot.DocumentIdPercentage);

        Assert.Equal(
            100.0,
            snapshot.VersionPercentage);

        Assert.Equal(
            100.0,
            snapshot.StatusPercentage);

        Assert.Equal(
            100.0,
            snapshot.OwnerPercentage);

        Assert.Equal(
            100.0,
            snapshot.RevisionHistoryPercentage);

        Assert.Equal(
            100.0,
            snapshot.RelatedWorkItemsPercentage);
    }
}
