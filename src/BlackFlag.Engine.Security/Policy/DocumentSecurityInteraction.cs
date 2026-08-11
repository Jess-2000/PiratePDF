
namespace BlackFlag.Engine.Security.Policy;

/// <summary>
/// Describes the intentionally small set of conditions that may interrupt the
/// user during automatic security handling.
/// </summary>
public enum DocumentSecurityInteraction
{
    /// <summary>No user interruption is required.</summary>
    None,

    /// <summary>A valid encrypted document requires a password.</summary>
    PasswordRequired,

    /// <summary>Encryption metadata is damaged and safe authentication is impossible.</summary>
    EncryptionDamage,

    /// <summary>Structural damage was recovered or is about to be repaired.</summary>
    StructuralRecovery,

    /// <summary>Content is being removed or neutralized by sanitization.</summary>
    Sanitization
}
