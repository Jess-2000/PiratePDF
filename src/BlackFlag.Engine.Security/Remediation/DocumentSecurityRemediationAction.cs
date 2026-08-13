using BlackFlag.Engine.Security.Findings;

namespace BlackFlag.Engine.Security.Remediation;

/// <summary>Couples one finding to the remediation intent a format-specific mutator must enforce.</summary>
public sealed record DocumentSecurityRemediationAction(
    DocumentSecurityFinding Finding,
    DocumentSecurityRemediationIntent Intent);
