using PiratePDF.DocGen.Tests.TestInfrastructure;
using PiratePDF.DocGen.Validation;

namespace PiratePDF.DocGen.Tests.Validation;

// Existing governance requires metadata/history for authoritative docs, including
// reports. A filename or self-asserted historical label is not an exemption.
public sealed class HistoricalReportPolicyTests
{
    [Theory]
    [InlineData("Current-Document.md")]
    [InlineData("Historical-Qualification-Report.md")]
    [InlineData("New-Qualification-Report.md")]
    public void MissingMetadataAndHistoryRemainVisibleAndContentUnchanged(string filename)
    {
        using var directory = new TemporaryDirectory();
        const string original = "# Qualification Report\n\nImmutable historical evidence.\n";
        var library = DocumentationTestFactory.LoadLibrary(directory,
            new DocumentationTestFactory.TestDocument(filename, original));

        DocumentationValidationResult result = DocumentationValidator.Validate(library);

        string[] expected = ["Document ID", "Category", "Version", "Status", "Owner",
            "Related BF Work Items", "Last Updated", "Next Review", "Revision History section"];
        Assert.Equal(9, result.Issues.Count);
        foreach (string field in expected)
            Assert.Contains(result.Issues, issue => issue.Severity == ValidationSeverity.Error &&
                issue.Message == field + " is missing.");
        Assert.Equal(original, File.ReadAllText(Path.Combine(directory.Path, filename)));
    }

    [Theory]
    [InlineData("Current-Document.md")]
    [InlineData("Historical-Qualification-Report.md")]
    [InlineData("New-Qualification-Report.md")]
    public void CompleteMetadataDoesNotExcuseMissingHistory(string filename)
    {
        using var directory = new TemporaryDirectory();
        string original = DocumentationTestFactory.CreateDocument(includeRevisionHistory: false);
        var library = DocumentationTestFactory.LoadLibrary(directory,
            new DocumentationTestFactory.TestDocument(filename, original));

        DocumentationValidationResult result = DocumentationValidator.Validate(library);

        Assert.True(result.HasErrors);
        Assert.Contains(result.Issues, issue => issue.Message == "Revision History section is missing.");
        Assert.Equal(original, File.ReadAllText(Path.Combine(directory.Path, filename)));
    }
}
