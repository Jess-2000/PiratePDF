using System.Collections.ObjectModel;
using BlackFlag.Engine.Security.Findings;
using BlackFlag.Engine.Security.Policy;

namespace BlackFlag.Engine.Security.Inspection;

/// <summary>Contains the final shared-policy assessment for one inspection.</summary>
public sealed class DocumentSecurityAssessment
{
    private readonly ReadOnlyCollection<DocumentSecurityFinding> findings;
    private readonly ReadOnlyCollection<DocumentSecurityInteraction> interactions;

    /// <summary>Initializes an assessment.</summary>
    public DocumentSecurityAssessment(
        IEnumerable<DocumentSecurityFinding> findings,
        IEnumerable<DocumentSecurityInteraction> interactions,
        bool manualInspection)
    {
        ArgumentNullException.ThrowIfNull(findings);
        ArgumentNullException.ThrowIfNull(interactions);

        this.findings = Array.AsReadOnly(findings.ToArray());
        this.interactions = Array.AsReadOnly(
            interactions
                .Where(static interaction => interaction != DocumentSecurityInteraction.None)
                .Distinct()
                .ToArray());

        ManualInspection = manualInspection;
    }

    /// <summary>Gets reportable findings.</summary>
    public IReadOnlyList<DocumentSecurityFinding> Findings => findings;

    /// <summary>Gets user interactions required by the assessment.</summary>
    public IReadOnlyList<DocumentSecurityInteraction> Interactions => interactions;

    /// <summary>Gets whether the assessment came from a manual security scan.</summary>
    public bool ManualInspection { get; }

    /// <summary>
    /// Gets whether a report should exist. Automatic clean scans produce no
    /// report; manual scans always do.
    /// </summary>
    public bool ShouldGenerateReport =>
        ManualInspection ||
        findings.Count > 0;

    /// <summary>Gets whether any finding blocks the requested operation.</summary>
    public bool IsBlocked =>
        findings.Any(
            static finding =>
                finding.Action == DocumentSecurityPolicyAction.Block);
}
