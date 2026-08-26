using System.Globalization;
using PiratePDF.DocGen.Repair.Diagnostics;
using PiratePDF.DocGen.Changes;
using PiratePDF.DocGen.Models;
using PiratePDF.DocGen.Repair.Metadata;

namespace PiratePDF.DocGen.Repair.Rules;

internal sealed class IsoDateRepairRule
    : IDocumentRepairRule
{
    private static readonly string[] SupportedDateFormats =
    [
        "yyyy-M-d",
        "yyyy-M-dd",
        "yyyy-MM-d",
        "yyyy/M/d",
        "yyyy/M/dd",
        "yyyy/MM/d",
        "yyyy/MM/dd",
        "MMM d, yyyy",
        "MMMM d, yyyy"
    ];

    public string Name =>
        "ISO date normalization";

    // =========================================================================
    // Repair Analysis
    // =========================================================================

    public void Analyze(
        RepairRuleContext context,
        List<DocumentChange> changes,
        List<RepairDiagnostic> diagnostics)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(changes);
        ArgumentNullException.ThrowIfNull(diagnostics);

        var metadataFields = MarkdownMetadataLocator.FindFields(
            context.FileContents);

        PlanDateRepair(
            context,
            metadataFields,
            DocumentationContract.LastUpdated,
            changes,
            diagnostics);

        PlanDateRepair(
            context,
            metadataFields,
            DocumentationContract.NextReview,
            changes,
            diagnostics);
    }

    // =========================================================================
    // Date Repair
    // =========================================================================

    private static void PlanDateRepair(
        RepairRuleContext context,
        Dictionary<string, MarkdownMetadataValueSpan> metadataFields,
        string fieldName,
        List<DocumentChange> changes,
        List<RepairDiagnostic> diagnostics)
    {
        if (!metadataFields.TryGetValue(
                fieldName,
                out var field))
        {
            return;
        }

        if (DateOnly.TryParseExact(
                field.Value,
                "yyyy-MM-dd",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out _))
        {
            return;
        }

        if (!TryNormalizeDate(
                field.Value,
                out var normalizedDate))
        {
            return;
        }

        changes.Add(
            DocumentChange.Replace(
                context.Document.FilePath,
                field.StartIndex,
                field.Length,
                normalizedDate,
                $"Normalize {fieldName} to the YYYY-MM-DD format."));
    }

    private static bool TryNormalizeDate(
        string value,
        out string normalizedDate)
    {
        normalizedDate = string.Empty;

        if (!DateOnly.TryParseExact(
                value,
                SupportedDateFormats,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AllowWhiteSpaces,
                out var parsedDate))
        {
            return false;
        }

        normalizedDate = parsedDate.ToString(
            "yyyy-MM-dd",
            CultureInfo.InvariantCulture);

        return true;
    }
}