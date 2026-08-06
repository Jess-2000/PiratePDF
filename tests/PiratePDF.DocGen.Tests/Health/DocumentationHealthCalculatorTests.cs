using PiratePDF.DocGen.Health;
using PiratePDF.DocGen.Tests.TestInfrastructure;
using PiratePDF.DocGen.Validation;

namespace PiratePDF.DocGen.Tests.Health;

public sealed class DocumentationHealthCalculatorTests
{
    [Fact]
    public void Calculate_WithHealthyLibrary_ReturnsHighScore()
    {
        using var directory = new TemporaryDirectory();

        var library = DocumentationTestFactory.LoadLibrary(
            directory,
            new DocumentationTestFactory.TestDocument(
                "One.md",
                DocumentationTestFactory.CreateDocument(
                    documentId: "PP-TEST-901",
                    body:
                        """
                        ## Purpose

                        First document.

                        ## Related Documents

                        - PP-TEST-902
                        """)),

            new DocumentationTestFactory.TestDocument(
                "Two.md",
                DocumentationTestFactory.CreateDocument(
                    documentId: "PP-TEST-902",
                    title: "Second Document",
                    body:
                        """
                        ## Purpose

                        Second document.

                        ## Related Documents

                        - PP-TEST-901
                        """)));

        var validationResult =
            DocumentationValidator.Validate(library);

        var snapshot =
            DocumentationHealthCalculator.Calculate(
                library,
                validationResult);

        Assert.Equal(
            2,
            snapshot.DocumentCount);

        Assert.Equal(
            0,
            snapshot.ValidationErrorCount);

        Assert.Empty(
            snapshot.MissingRelatedDocumentIds);

        Assert.InRange(
            snapshot.OverallScore,
            97.0,
            100.0);

        Assert.Equal(
            "A+",
            snapshot.Grade);
    }

    [Fact]
    public void Calculate_WithMissingRelationships_LowersScore()
    {
        using var directory = new TemporaryDirectory();

        var library = DocumentationTestFactory.LoadLibrary(
            directory,
            new DocumentationTestFactory.TestDocument(
                "One.md",
                DocumentationTestFactory.CreateDocument(
                    documentId: "PP-TEST-903",
                    body:
                        """
                        ## Purpose

                        Test document.

                        ## Related Documents

                        - PP-MISSING-999
                        """)));

        var validationResult =
            DocumentationValidator.Validate(library);

        var snapshot =
            DocumentationHealthCalculator.Calculate(
                library,
                validationResult);

        Assert.Contains(
            "PP-MISSING-999",
            snapshot.MissingRelatedDocumentIds);

        Assert.True(
            snapshot.OverallScore < 100.0);
    }

    [Fact]
    public void Calculate_WithValidationError_LowersValidationScore()
    {
        using var directory = new TemporaryDirectory();

        var library = DocumentationTestFactory.LoadLibrary(
            directory,
            new DocumentationTestFactory.TestDocument(
                "Invalid.md",
                DocumentationTestFactory.CreateDocument(
                    documentId: "PP-TEST-904",
                    status: "Invalid Status")));

        var validationResult =
            DocumentationValidator.Validate(library);

        var snapshot =
            DocumentationHealthCalculator.Calculate(
                library,
                validationResult);

        Assert.True(
            snapshot.ValidationErrorCount > 0);

        Assert.True(
            snapshot.OverallScore < 100.0);
    }
}
