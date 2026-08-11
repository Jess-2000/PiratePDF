
using BlackFlag.Engine.Security.Inspection;

namespace BlackFlag.Engine.Security.Reporting;

/// <summary>Creates reports only when BF-1049 reporting rules require one.</summary>
public static class DocumentSecurityReportFactory
{
    /// <summary>
    /// Creates an ephemeral report when findings exist or the user explicitly
    /// requested a manual scan. Returns <see langword="null"/> for a clean
    /// automatic inspection.
    /// </summary>
    public static DocumentSecurityReport? Create(
        DocumentSecurityAssessment assessment,
        DateTimeOffset generatedAtUtc)
    {
        ArgumentNullException.ThrowIfNull(assessment);

        if (!assessment.ShouldGenerateReport)
        {
            return null;
        }

        return new DocumentSecurityReport(
            assessment.Findings,
            generatedAtUtc);
    }
}
