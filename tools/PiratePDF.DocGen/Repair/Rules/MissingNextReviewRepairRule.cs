using System.Globalization;

using PiratePDF.DocGen.Changes;
using PiratePDF.DocGen.Models;
using PiratePDF.DocGen.Repair.Diagnostics;
using PiratePDF.DocGen.Repair.Metadata;

namespace PiratePDF.DocGen.Repair.Rules;

internal sealed class MissingNextReviewRepairRule
    : IDocumentRepairRule
{
    public string Name =>
        "Missing next review";

    public void Analyze(
        RepairRuleContext context,
        List<DocumentChange> changes,
        List<RepairDiagnostic> diagnostics)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(changes);
        ArgumentNullException.ThrowIfNull(diagnostics);

        Dictionary<string, MarkdownMetadataValueSpan> fields =
            MarkdownMetadataLocator.FindFields(
                context.FileContents);

        if (fields.ContainsKey(
                DocumentationContract.NextReview))
        {
            return;
        }

        if (!fields.TryGetValue(
                DocumentationContract.LastUpdated,
                out MarkdownMetadataValueSpan lastUpdatedField))
        {
            return;
        }

        if (!DateOnly.TryParseExact(
                lastUpdatedField.Value,
                "yyyy-MM-dd",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out DateOnly lastUpdated))
        {
            return;
        }

        DateOnly nextReview =
            DocumentationContract.CalculateNextReview(
                lastUpdated);

        string newline = context.FileContents.Contains(
            "\r\n",
            StringComparison.Ordinal)
            ? "\r\n"
            : "\n";

        int lineEnd = context.FileContents.IndexOf(
            '\n',
            lastUpdatedField.StartIndex);

        int insertionIndex = lineEnd >= 0
            ? lineEnd + 1
            : context.FileContents.Length;

        string insertion =
            $"| **{DocumentationContract.NextReview}** | " +
            $"{nextReview:yyyy-MM-dd} |{newline}";

        changes.Add(
            DocumentChange.Insert(
                context.Document.FilePath,
                insertionIndex,
                insertion,
                "Add Next Review using the canonical three-month review cadence."));
    }
}
