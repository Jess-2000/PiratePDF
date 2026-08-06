using System.Globalization;
using System.Text;
using PiratePDF.DocGen.Repair.Diagnostics;
using PiratePDF.DocGen.Changes;

namespace PiratePDF.DocGen.Repair.Rules;

internal sealed class MissingRevisionHistoryRepairRule
    : IDocumentRepairRule
{
    private const string RevisionHistoryHeading =
        "Revision History";

    public string Name =>
        "Missing revision history";

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

        if (document.HasSection(
                RevisionHistoryHeading))
        {
            return;
        }

        var newline = DetectNewline(
            context.FileContents);

        var insertionText = BuildInsertion(
            context,
            newline);

        changes.Add(
            DocumentChange.Insert(
                document.FilePath,
                context.FileContents.Length,
                insertionText,
                "Add the required Revision History section."));
    }

    // =========================================================================
    // Revision-History Generation
    // =========================================================================

    private static string BuildInsertion(
        RepairRuleContext context,
        string newline)
    {
        var document = context.Document;
        var content = context.FileContents;
        var builder = new StringBuilder();

        if (content.Length > 0)
        {
            if (!EndsWithNewline(content))
            {
                builder.Append(newline);
            }

            if (!EndsWithBlankLine(content))
            {
                builder.Append(newline);
            }
        }

        var version = string.IsNullOrWhiteSpace(
            document.Version)
            ? "1.0"
            : document.Version.Trim();

        var currentDate = DateOnly
            .FromDateTime(DateTime.UtcNow)
            .ToString(
                "yyyy-MM-dd",
                CultureInfo.InvariantCulture);

        builder.Append("## Revision History");
        builder.Append(newline);
        builder.Append(newline);

        builder.Append(
            "| Version | Date | Description |");

        builder.Append(newline);
        builder.Append("|---|---|---|");
        builder.Append(newline);

        builder.Append("| ");
        builder.Append(version);
        builder.Append(" | ");
        builder.Append(currentDate);
        builder.Append(
            " | Added missing revision history section. |");

        builder.Append(newline);

        return builder.ToString();
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

    private static bool EndsWithBlankLine(
        string content)
    {
        return content.EndsWith(
                "\r\n\r\n",
                StringComparison.Ordinal) ||
            content.EndsWith(
                "\n\n",
                StringComparison.Ordinal) ||
            content.EndsWith(
                "\r\r",
                StringComparison.Ordinal);
    }
}
