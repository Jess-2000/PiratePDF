using System.Collections.ObjectModel;
using BlackFlag.Engine.Security.Findings;
using BlackFlag.Engine.Security.Policy;

namespace BlackFlag.Engine.Security.Reporting;

/// <summary>Represents an ephemeral security report for one inspected document.</summary>
public sealed class DocumentSecurityReport
{
    private readonly ReadOnlyCollection<DocumentSecurityFinding> findings;
    private readonly ReadOnlyCollection<DocumentSecurityInteraction> interactions;

    /// <summary>Initializes a security report.</summary>
    public DocumentSecurityReport(
        IEnumerable<DocumentSecurityFinding> findings,
        DateTimeOffset generatedAtUtc)
        : this(
            findings,
            [],
            manualInspection: false,
            generatedAtUtc)
    {
    }

    /// <summary>Initializes a security report with assessment context.</summary>
    public DocumentSecurityReport(
        IEnumerable<DocumentSecurityFinding> findings,
        IEnumerable<DocumentSecurityInteraction> interactions,
        bool manualInspection,
        DateTimeOffset generatedAtUtc)
    {
        ArgumentNullException.ThrowIfNull(findings);
        ArgumentNullException.ThrowIfNull(interactions);

        this.findings = Array.AsReadOnly(findings.ToArray());
        this.interactions = Array.AsReadOnly(
            interactions
                .Where(
                    static interaction =>
                        interaction != DocumentSecurityInteraction.None)
                .Distinct()
                .ToArray());

        ManualInspection = manualInspection;
        GeneratedAtUtc = generatedAtUtc;
    }

    /// <summary>Gets the report findings.</summary>
    public IReadOnlyList<DocumentSecurityFinding> Findings => findings;

    /// <summary>Gets user interactions associated with the assessment.</summary>
    public IReadOnlyList<DocumentSecurityInteraction> Interactions => interactions;

    /// <summary>Gets whether this report was produced by a manual inspection.</summary>
    public bool ManualInspection { get; }

    /// <summary>Gets when the report was generated.</summary>
    public DateTimeOffset GeneratedAtUtc { get; }
}
