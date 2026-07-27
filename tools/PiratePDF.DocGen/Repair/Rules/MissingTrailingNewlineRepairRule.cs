using PiratePDF.DocGen.Changes;
using PiratePDF.DocGen.Repair.Diagnostics;

namespace PiratePDF.DocGen.Repair.Rules;

internal sealed class MissingTrailingNewlineRepairRule
    : IDocumentRepairRule
{
    public string Name =>
        "Missing trailing newline";

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

        var document = context.Document;
        var content = context.FileContents;

        // Revision-history insertion already adds a terminated block. Avoid
        // proposing two insertions at the same end-of-file position.
        if (!document.HasSection("Revision History"))
        {
            return;
        }

        if (
            content.Length == 0 ||
            EndsWithNewline(content))
        {
            return;
        }

        changes.Add(
            DocumentChange.Insert(
                document.FilePath,
                content.Length,
                DetectNewline(content),
                "Add the required trailing newline."));
    }

    // =========================================================================
    // Newline Helpers
    // =========================================================================

    private static string DetectNewline(
        string content)
    {
        return content.Contains(
            "\r\n",
            StringComparison.Ordinal)
            ? "\r\n"
            : "\n";
    }

    private static bool EndsWithNewline(
        string content)
    {
        return content.EndsWith('\n') ||
            content.EndsWith('\r');
    }
}