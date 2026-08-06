using PiratePDF.DocGen.Changes;
using PiratePDF.DocGen.Repair.Diagnostics;

namespace PiratePDF.DocGen.Repair.Rules;

internal interface IDocumentRepairRule
{
    string Name { get; }

    void Analyze(
        RepairRuleContext context,
        List<DocumentChange> changes,
        List<RepairDiagnostic> diagnostics);
}