
namespace BlackFlag.Engine.Security.Remediation;

/// <summary>
/// Describes format-independent remediation intent. Format-specific code owns
/// the actual document mutation.
/// </summary>
public enum DocumentSecurityRemediationIntent
{
    /// <summary>Remove active executable behavior.</summary>
    RemoveActiveContent,

    /// <summary>Remove embedded payload content.</summary>
    RemoveEmbeddedContent,

    /// <summary>Disable a reference to external content or behavior.</summary>
    DisableExternalReference,

    /// <summary>Preserve content as inert data when the format safely permits it.</summary>
    PreserveAsInertData,

    /// <summary>Reject the requested operation.</summary>
    RejectOperation
}
