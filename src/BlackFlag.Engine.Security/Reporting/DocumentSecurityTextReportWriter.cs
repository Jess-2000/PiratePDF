using System.Globalization;
using BlackFlag.Engine.Security.Findings;
using BlackFlag.Engine.Security.Policy;

namespace BlackFlag.Engine.Security.Reporting;

/// <summary>
/// Writes a deterministic, human-readable representation of an ephemeral
/// document security report.
/// </summary>
public static class DocumentSecurityTextReportWriter
{
    /// <summary>Writes a security report without persisting it automatically.</summary>
    public static void Write(
        DocumentSecurityReport report,
        TextWriter writer)
    {
        ArgumentNullException.ThrowIfNull(report);
        ArgumentNullException.ThrowIfNull(writer);

        writer.WriteLine("BlackFlag Document Security Report");
        writer.Write("Generated (UTC): ");
        writer.WriteLine(
            report.GeneratedAtUtc
                .ToUniversalTime()
                .ToString("O", CultureInfo.InvariantCulture));

        writer.Write("Inspection: ");
        writer.WriteLine(
            report.ManualInspection
                ? "Manual"
                : "Automatic");

        WriteInteractions(report.Interactions, writer);

        writer.Write("Findings: ");
        writer.WriteLine(
            report.Findings.Count.ToString(CultureInfo.InvariantCulture));

        for (int index = 0; index < report.Findings.Count; index++)
        {
            WriteFinding(
                report.Findings[index],
                index + 1,
                writer);
        }
    }

    private static void WriteInteractions(
        IReadOnlyList<DocumentSecurityInteraction> interactions,
        TextWriter writer)
    {
        writer.Write("Interactions: ");

        if (interactions.Count == 0)
        {
            writer.WriteLine("None");
            return;
        }

        for (int index = 0; index < interactions.Count; index++)
        {
            if (index > 0)
            {
                writer.Write(", ");
            }

            writer.Write(interactions[index].ToString());
        }

        writer.WriteLine();
    }

    private static void WriteFinding(
        DocumentSecurityFinding finding,
        int number,
        TextWriter writer)
    {
        writer.WriteLine();
        writer.Write("Finding ");
        writer.Write(
            number.ToString(CultureInfo.InvariantCulture));
        writer.WriteLine(":");

        writer.Write("  Severity: ");
        writer.WriteLine(finding.Severity.ToString());

        writer.Write("  Action: ");
        writer.WriteLine(finding.Action.ToString());

        writer.Write("  Kind: ");
        writer.WriteLine(finding.Observation.Kind);

        writer.Write("  Source format: ");
        writer.WriteLine(finding.Observation.SourceFormat);

        if (!string.IsNullOrWhiteSpace(
                finding.Observation.Location?.LogicalPath))
        {
            writer.Write("  Location: ");
            writer.WriteLine(
                finding.Observation.Location.LogicalPath);
        }

        writer.Write("  Description: ");
        writer.WriteLine(finding.Observation.Description);

        writer.Write("  Policy: ");
        writer.WriteLine(finding.Message);
    }
}
