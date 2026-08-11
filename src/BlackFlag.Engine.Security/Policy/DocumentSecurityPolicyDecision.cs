
namespace BlackFlag.Engine.Security.Policy;

/// <summary>Contains the shared policy decision for one observation.</summary>
public sealed record DocumentSecurityPolicyDecision(
    DocumentSecurityPolicyAction Action,
    DocumentSecuritySeverity Severity,
    string Message,
    DocumentSecurityInteraction Interaction = DocumentSecurityInteraction.None)
{
    /// <summary>Creates a silent allow decision.</summary>
    public static DocumentSecurityPolicyDecision Allow(string message = "Allowed.") =>
        new(
            DocumentSecurityPolicyAction.Allow,
            DocumentSecuritySeverity.Information,
            message);
}
