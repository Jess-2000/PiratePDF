namespace BlackFlag.Engine.SDK.Security;

/// <summary>Represents the stable SDK-facing result of one security evaluation.</summary>
public sealed class DocumentSecuritySdkResult
{
    private readonly DocumentSecuritySdkFinding[] findings;
    private readonly string[] interactions;

    /// <summary>Initializes an SDK security result.</summary>
    public DocumentSecuritySdkResult(
        IEnumerable<DocumentSecuritySdkFinding> findings,
        IEnumerable<string> interactions,
        bool isBlocked,
        bool hasReport,
        bool manualInspection,
        DateTimeOffset? reportGeneratedAtUtc)
    {
        ArgumentNullException.ThrowIfNull(findings);
        ArgumentNullException.ThrowIfNull(interactions);

        this.findings = findings.ToArray();
        this.interactions = interactions.ToArray();
        IsBlocked = isBlocked;
        HasReport = hasReport;
        ManualInspection = manualInspection;
        ReportGeneratedAtUtc = reportGeneratedAtUtc;
    }

    /// <summary>Gets the reportable findings.</summary>
    public IReadOnlyList<DocumentSecuritySdkFinding> Findings => findings;

    /// <summary>Gets requested application interactions as stable names.</summary>
    public IReadOnlyList<string> Interactions => interactions;

    /// <summary>Gets whether policy blocks the requested operation.</summary>
    public bool IsBlocked { get; }

    /// <summary>Gets whether an ephemeral report exists for this evaluation.</summary>
    public bool HasReport { get; }

    /// <summary>Gets whether the result came from manual inspection.</summary>
    public bool ManualInspection { get; }

    /// <summary>Gets the report generation time when a report exists.</summary>
    public DateTimeOffset? ReportGeneratedAtUtc { get; }
}
