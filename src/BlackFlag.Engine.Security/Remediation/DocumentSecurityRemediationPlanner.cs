using BlackFlag.Engine.Security.Findings;
using BlackFlag.Engine.Security.Inspection;
using BlackFlag.Engine.Security.Model;
using BlackFlag.Engine.Security.Policy;

namespace BlackFlag.Engine.Security.Remediation;

/// <summary>
/// Converts shared-policy findings into deterministic, format-independent
/// remediation intent. It never mutates source documents.
/// </summary>
public sealed class DocumentSecurityRemediationPlanner
{
    /// <summary>Creates a remediation plan for a sanitizing operation.</summary>
    public static DocumentSecurityRemediationPlan CreatePlan(
        DocumentSecurityAssessment assessment,
        DocumentSecurityOperation operation,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(assessment);

        if (operation is not (
            DocumentSecurityOperation.PassiveConversion or
            DocumentSecurityOperation.Sanitize))
        {
            throw new ArgumentOutOfRangeException(
                nameof(operation),
                operation,
                "Remediation plans are only valid for passive conversion or sanitization.");
        }

        var actions =
            new List<DocumentSecurityRemediationAction>(
                assessment.Findings.Count);

        bool blocked = assessment.IsBlocked;

        foreach (DocumentSecurityFinding finding in assessment.Findings)
        {
            cancellationToken.ThrowIfCancellationRequested();

            DocumentSecurityRemediationIntent? intent =
                SelectIntent(finding);

            if (intent is null)
            {
                continue;
            }

            blocked |= intent == DocumentSecurityRemediationIntent.RejectOperation;
            actions.Add(new(finding, intent.Value));
        }

        return new DocumentSecurityRemediationPlan(actions, blocked);
    }

    private static DocumentSecurityRemediationIntent? SelectIntent(
        DocumentSecurityFinding finding)
    {
        if (finding.Action == DocumentSecurityPolicyAction.Block ||
            finding.Observation.Category ==
                DocumentSecurityObservationCategory.EncryptionCondition)
        {
            return DocumentSecurityRemediationIntent.RejectOperation;
        }

        if (finding.Action != DocumentSecurityPolicyAction.Sanitize)
        {
            return null;
        }

        return finding.Observation.Category switch
        {
            DocumentSecurityObservationCategory.ExternalReference =>
                DocumentSecurityRemediationIntent.DisableExternalReference,

            DocumentSecurityObservationCategory.ActiveContent or
            DocumentSecurityObservationCategory.ScriptContent or
            DocumentSecurityObservationCategory.AutomaticAction or
            DocumentSecurityObservationCategory.ExecutableContent or
            DocumentSecurityObservationCategory.SuspiciousRelationship =>
                DocumentSecurityRemediationIntent.RemoveActiveContent,

            DocumentSecurityObservationCategory.EmbeddedContent =>
                DocumentSecurityRemediationIntent.RemoveEmbeddedContent,

            _ => DocumentSecurityRemediationIntent.PreserveAsInertData
        };
    }
}
