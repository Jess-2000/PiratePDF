
using BlackFlag.Engine.Security.Findings;

namespace BlackFlag.Engine.Security.Reporting;

/// <summary>Represents an ephemeral security report for one inspected document.</summary>
public sealed class DocumentSecurityReport
{
    private readonly DocumentSecurityFinding[] findings;

    /// <summary>Initializes a security report.</summary>
    public DocumentSecurityReport(
        IEnumerable<DocumentSecurityFinding> findings,
        DateTimeOffset generatedAtUtc)
    {
        ArgumentNullException.ThrowIfNull(findings);

        this.findings = findings.ToArray();
        GeneratedAtUtc = generatedAtUtc;
    }

    /// <summary>Gets the report findings.</summary>
    public IReadOnlyList<DocumentSecurityFinding> Findings => findings;

    /// <summary>Gets when the report was generated.</summary>
    public DateTimeOffset GeneratedAtUtc { get; }
}
