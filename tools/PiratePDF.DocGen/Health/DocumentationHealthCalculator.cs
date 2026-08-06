using PiratePDF.DocGen.Library;
using PiratePDF.DocGen.Validation;

namespace PiratePDF.DocGen.Health;

internal static class DocumentationHealthCalculator
{
    private const double MetadataWeight = 0.55;
    private const double StructuralWeight = 0.25;
    private const double RelationshipWeight = 0.10;
    private const double ValidationWeight = 0.10;

    // =========================================================================
    // Public API
    // =========================================================================

    public static DocumentationHealthSnapshot Calculate(
        DocumentationLibrary library,
        DocumentationValidationResult validationResult)
    {
        ArgumentNullException.ThrowIfNull(library);
        ArgumentNullException.ThrowIfNull(validationResult);

        var coverage = library.Coverage.CreateSnapshot();

        var missingRelatedDocumentIds =
            library.Relationships.FindMissingRelatedDocumentIds();

        var overallScore = CalculateOverallScore(
            coverage,
            missingRelatedDocumentIds.Length,
            validationResult);

        return new DocumentationHealthSnapshot(
            library.Count,
            library.Statistics.SectionCount,
            library.Statistics.RevisionEntryCount,
            library.Statistics.RelatedDocumentReferenceCount,
            library.Statistics.RelatedWorkItemReferenceCount,
            library.Statistics.GetCategoryCounts(),
            library.Statistics.GetStatusCounts(),
            library.Statistics.GetOwnerCounts(),
            coverage,
            missingRelatedDocumentIds,
            validationResult.ErrorCount,
            validationResult.WarningCount,
            validationResult.InformationCount,
            overallScore,
            CalculateGrade(overallScore));
    }

    // =========================================================================
    // Scoring
    // =========================================================================

    private static double CalculateOverallScore(
        Library.Coverage.DocumentationCoverageSnapshot coverage,
        int missingRelatedDocumentCount,
        DocumentationValidationResult validationResult)
    {
        var metadataScore = Average(
            coverage.DocumentIdPercentage,
            coverage.VersionPercentage,
            coverage.StatusPercentage,
            coverage.OwnerPercentage,
            coverage.LastUpdatedPercentage,
            coverage.NextReviewPercentage);

        var structuralScore = Average(
            coverage.RevisionHistoryPercentage,
            coverage.RelatedDocumentsPercentage,
            coverage.RelatedWorkItemsPercentage);

        var relationshipScore =
            CalculateRelationshipScore(
                coverage.TotalDocuments,
                missingRelatedDocumentCount);

        var validationScore =
            CalculateValidationScore(validationResult);

        var weightedScore =
            (metadataScore * MetadataWeight) +
            (structuralScore * StructuralWeight) +
            (relationshipScore * RelationshipWeight) +
            (validationScore * ValidationWeight);

        return Math.Round(
            Math.Clamp(weightedScore, 0.0, 100.0),
            1,
            MidpointRounding.AwayFromZero);
    }

    private static double CalculateRelationshipScore(
        int documentCount,
        int missingRelatedDocumentCount)
    {
        if (missingRelatedDocumentCount == 0)
        {
            return 100.0;
        }

        if (documentCount == 0)
        {
            return 0.0;
        }

        var penalty =
            missingRelatedDocumentCount * 100.0 / documentCount;

        return Math.Clamp(
            100.0 - penalty,
            0.0,
            100.0);
    }

    private static double CalculateValidationScore(
        DocumentationValidationResult validationResult)
    {
        if (validationResult.ErrorCount > 0)
        {
            return Math.Max(
                0.0,
                60.0 - validationResult.ErrorCount);
        }

        if (validationResult.WarningCount > 0)
        {
            return Math.Max(
                70.0,
                100.0 - validationResult.WarningCount);
        }

        return 100.0;
    }

    private static string CalculateGrade(
        double score)
    {
        return score switch
        {
            >= 97.0 => "A+",
            >= 93.0 => "A",
            >= 90.0 => "A-",
            >= 87.0 => "B+",
            >= 83.0 => "B",
            >= 80.0 => "B-",
            >= 77.0 => "C+",
            >= 73.0 => "C",
            >= 70.0 => "C-",
            >= 67.0 => "D+",
            >= 63.0 => "D",
            >= 60.0 => "D-",
            _ => "F"
        };
    }

    private static double Average(
        params double[] values)
    {
        ArgumentNullException.ThrowIfNull(values);

        if (values.Length == 0)
        {
            return 100.0;
        }

        return values.Average();
    }
}
