using PiratePDF.DocGen.Changes;
using PiratePDF.DocGen.Repair.Diagnostics;

namespace PiratePDF.DocGen.Repair;

internal sealed class DocumentationRepairPlan
{
    private readonly RepairDiagnostic[] diagnostics;

    public DocumentationRepairPlan(
        DocumentChangeSet changeSet,
        IEnumerable<RepairDiagnostic> diagnostics)
    {
        ArgumentNullException.ThrowIfNull(changeSet);
        ArgumentNullException.ThrowIfNull(diagnostics);

        ChangeSet = changeSet;

        this.diagnostics = diagnostics
            .OrderBy(
                diagnostic => diagnostic.FilePath,
                StringComparer.OrdinalIgnoreCase)
            .ThenBy(diagnostic => diagnostic.Severity)
            .ThenBy(
                diagnostic => diagnostic.RuleName,
                StringComparer.OrdinalIgnoreCase)
            .ThenBy(
                diagnostic => diagnostic.Message,
                StringComparer.Ordinal)
            .ToArray();
    }

    public DocumentChangeSet ChangeSet { get; }

    public IReadOnlyList<RepairDiagnostic> Diagnostics =>
        diagnostics;

    public int DiagnosticCount =>
        diagnostics.Length;

    public int WarningCount =>
        diagnostics.Count(
            diagnostic =>
                diagnostic.Severity ==
                RepairDiagnosticSeverity.Warning);

    public int ErrorCount =>
        diagnostics.Count(
            diagnostic =>
                diagnostic.Severity ==
                RepairDiagnosticSeverity.Error);

    public bool HasChanges =>
        !ChangeSet.IsEmpty;

    public bool HasErrors =>
        ErrorCount > 0;
}