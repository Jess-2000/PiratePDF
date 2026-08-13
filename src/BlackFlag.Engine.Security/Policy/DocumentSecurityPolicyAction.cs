
namespace BlackFlag.Engine.Security.Policy;

/// <summary>Describes the policy action selected for an observation.</summary>
public enum DocumentSecurityPolicyAction
{
    /// <summary>Allow the content without generating a finding.</summary>
    Allow,

    /// <summary>Allow the content and include it in a security report.</summary>
    AllowAndReport,

    /// <summary>Remove or neutralize the content during a sanitizing operation.</summary>
    Sanitize,

    /// <summary>Require an application-level user decision before proceeding.</summary>
    RequireUserDecision,

    /// <summary>Block the operation because it cannot proceed safely.</summary>
    Block
}
