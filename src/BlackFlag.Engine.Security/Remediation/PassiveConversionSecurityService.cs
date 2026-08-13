using BlackFlag.Engine.Security.Inspection;
using BlackFlag.Engine.Security.Model;
using BlackFlag.Engine.Security.Policy;

namespace BlackFlag.Engine.Security.Remediation;

/// <summary>
/// Establishes the mandatory shared-policy boundary for passive conversion.
/// This service produces intent only and never executes source behavior.
/// </summary>
public sealed class PassiveConversionSecurityService
{
    private readonly DocumentSecurityAssessmentEngine assessmentEngine;
    private readonly DocumentSecurityRemediationPlanner remediationPlanner;

    /// <summary>Initializes the passive-conversion security service.</summary>
    public PassiveConversionSecurityService(
        DocumentSecurityAssessmentEngine? assessmentEngine = null,
        DocumentSecurityRemediationPlanner? remediationPlanner = null)
    {
        this.assessmentEngine =
            assessmentEngine ?? new DocumentSecurityAssessmentEngine();
        this.remediationPlanner =
            remediationPlanner ?? new DocumentSecurityRemediationPlanner();
    }

    /// <summary>Evaluates normalized observations and produces the converter remediation contract.</summary>
    public PassiveConversionSecurityResult Evaluate(
        IEnumerable<DocumentSecurityObservation> observations,
        DocumentSecurityInspectionBudget? budget = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(observations);

        DocumentSecurityAssessment assessment =
            assessmentEngine.Evaluate(
                observations,
                DocumentSecurityOperation.PassiveConversion,
                budget,
                cancellationToken);

        DocumentSecurityRemediationPlan plan =
            DocumentSecurityRemediationPlanner.CreatePlan(
                assessment,
                DocumentSecurityOperation.PassiveConversion,
                cancellationToken);

        return new PassiveConversionSecurityResult(assessment, plan);
    }
}
