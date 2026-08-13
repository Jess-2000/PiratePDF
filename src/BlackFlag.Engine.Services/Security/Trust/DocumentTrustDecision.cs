
namespace BlackFlag.Engine.Services.Security.Trust;

/// <summary>Describes whether opening may use the trusted-document fast path.</summary>
public enum DocumentTrustDecision
{
    /// <summary>No current fingerprint-backed trust exists; perform a full inspection.</summary>
    FullInspectionRequired = 0,

    /// <summary>The current bytes and policy match validated local trust.</summary>
    TrustedFastPath = 1
}
