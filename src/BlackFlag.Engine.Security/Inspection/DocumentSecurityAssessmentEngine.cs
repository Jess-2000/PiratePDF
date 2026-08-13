using BlackFlag.Engine.Security.Findings;
using BlackFlag.Engine.Security.Model;
using BlackFlag.Engine.Security.Policy;

namespace BlackFlag.Engine.Security.Inspection;

/// <summary>
/// Applies one shared policy to normalized observations and enforces the
/// silent-by-default reporting and interaction rules.
/// </summary>
public sealed class DocumentSecurityAssessmentEngine
{
    private readonly IDocumentSecurityPolicy policy;

    /// <summary>Initializes an assessment engine.</summary>
    public DocumentSecurityAssessmentEngine(
        IDocumentSecurityPolicy? policy = null)
    {
        this.policy =
            policy ??
            new BlackFlagDocumentSecurityPolicy();
    }

    /// <summary>Evaluates normalized observations for one document operation.</summary>
    public DocumentSecurityAssessment Evaluate(
        IEnumerable<DocumentSecurityObservation> observations,
        DocumentSecurityOperation operation,
        DocumentSecurityInspectionBudget? budget = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(observations);

        budget ??=
            new DocumentSecurityInspectionBudget();

        var findings =
            new List<DocumentSecurityFinding>();

        var interactions =
            new List<DocumentSecurityInteraction>();

        foreach (DocumentSecurityObservation observation in observations)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ArgumentNullException.ThrowIfNull(observation);

            budget.ConsumeObservation();
            budget.ConsumeWork();

            DocumentSecurityPolicyDecision decision =
                policy.Evaluate(
                    observation,
                    operation);

            if (decision.Interaction != DocumentSecurityInteraction.None)
            {
                interactions.Add(decision.Interaction);
            }

            if (decision.Action != DocumentSecurityPolicyAction.Allow)
            {
                findings.Add(
                    new DocumentSecurityFinding(
                        observation,
                        decision.Action,
                        decision.Severity,
                        decision.Message,
                        decision.Interaction));
            }
        }

        return new DocumentSecurityAssessment(
            findings,
            interactions,
            manualInspection:
                operation == DocumentSecurityOperation.ManualInspection);
    }
}
