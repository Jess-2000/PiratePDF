
using BlackFlag.Engine.Security.Model;
using BlackFlag.Engine.Security.Policy;

namespace BlackFlag.Engine.Security.Findings;

/// <summary>Represents one reportable result after policy evaluation.</summary>
public sealed record DocumentSecurityFinding(
    DocumentSecurityObservation Observation,
    DocumentSecurityPolicyAction Action,
    DocumentSecuritySeverity Severity,
    string Message,
    DocumentSecurityInteraction Interaction);
