using PiratePDF.DocGen.Changes;
using PiratePDF.DocGen.Repair.Metadata;
using PiratePDF.DocGen.Repair.Diagnostics;

namespace PiratePDF.DocGen.Repair.Rules;

internal sealed class CanonicalStatusRepairRule
    : IDocumentRepairRule
{
    private static readonly Dictionary<string, string>
        CanonicalStatuses =
            new(
                StringComparer.OrdinalIgnoreCase)
            {
                ["Proposed"] = "Proposed",
                ["Draft"] = "Draft",
                ["Review"] = "Review",
                ["Approved"] = "Approved",
                ["In Development"] = "In Development",
                ["Implemented"] = "Implemented",
                ["Tested"] = "Tested",
                ["Released"] = "Released",
                ["Deferred"] = "Deferred",
                ["Deprecated"] = "Deprecated",
                ["Superseded"] = "Superseded",
                ["Rejected"] = "Rejected"
            };

    public string Name =>
        "Canonical status casing";

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

        if (!MarkdownMetadataLocator.TryFindField(
                context.FileContents,
                "Status",
                out var statusField))
        {
            return;
        }

        if (!CanonicalStatuses.TryGetValue(
                statusField.Value,
                out var canonicalStatus))
        {
            return;
        }

        if (string.Equals(
                statusField.Value,
                canonicalStatus,
                StringComparison.Ordinal))
        {
            return;
        }

        changes.Add(
            DocumentChange.Replace(
                context.Document.FilePath,
                statusField.StartIndex,
                statusField.Length,
                canonicalStatus,
                "Normalize the document status to its canonical casing."));
    }
}
