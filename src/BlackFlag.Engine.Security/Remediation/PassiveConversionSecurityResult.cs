using BlackFlag.Engine.Security.Inspection;

namespace BlackFlag.Engine.Security.Remediation;

/// <summary>Contains the assessment and remediation boundary a passive converter must honor.</summary>
public sealed record PassiveConversionSecurityResult(
    DocumentSecurityAssessment Assessment,
    DocumentSecurityRemediationPlan RemediationPlan)
{
    /// <summary>Gets whether passive conversion may proceed.</summary>
    public bool CanProceed => !RemediationPlan.IsBlocked;

    /// <summary>Gets whether the converter must omit or neutralize content.</summary>
    public bool RequiresRemediation => RemediationPlan.RequiresMutation;
}
