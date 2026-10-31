using PiratePDF.DocGen.Changes;
using PiratePDF.DocGen.Infrastructure;
using PiratePDF.DocGen.Library;
using PiratePDF.DocGen.Repair.Diagnostics;
using PiratePDF.DocGen.Repair.Rules;

namespace PiratePDF.DocGen.Repair;

internal static class DocumentationRepairPlanner
{
    private static readonly IReadOnlyList<IDocumentRepairRule>
        RepairRules =
            RepairRuleRegistry.Create();

    // =========================================================================
    // Public API
    // =========================================================================

    public static DocumentationRepairPlan Plan(
        DocumentationLibrary library)
    {
        ArgumentNullException.ThrowIfNull(library);

        var changes = new List<DocumentChange>();
        var diagnostics = new List<RepairDiagnostic>();

        foreach (var document in library.AllDocuments)
        {
            // Valid supplements satisfy registration requirements without rewriting evidence.
            if (document.MetadataSupplementPath is not null) continue;
            if (HistoricalMetadataSupplements.IsHistoricalPath(document.FilePath))
            {
                diagnostics.Add(new RepairDiagnostic(RepairDiagnosticSeverity.Error, "HistoricalMetadata",
                    document.FilePath, document.DocumentId, "Immutable historical report requires a valid supplement; no rewrite proposed."));
                continue;
            }
            var context = new RepairRuleContext(
                library,
                document);

            AnalyzeDocument(
                context,
                changes,
                diagnostics);
        }

        return new DocumentationRepairPlan(
            new DocumentChangeSet(changes),
            diagnostics);
    }

    // =========================================================================
    // Rule Orchestration
    // =========================================================================

    private static void AnalyzeDocument(
        RepairRuleContext context,
        List<DocumentChange> changes,
        List<RepairDiagnostic> diagnostics)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(changes);
        ArgumentNullException.ThrowIfNull(diagnostics);

        foreach (var rule in RepairRules)
        {
            rule.Analyze(
                context,
                changes,
                diagnostics);
        }
    }
}
