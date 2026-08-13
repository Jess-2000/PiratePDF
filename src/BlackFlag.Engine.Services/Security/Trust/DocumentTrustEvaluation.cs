
namespace BlackFlag.Engine.Services.Security.Trust;

/// <summary>Contains the silent trusted-document opening decision.</summary>
public sealed record DocumentTrustEvaluation(
    DocumentTrustDecision Decision,
    string Sha256Fingerprint)
{
    /// <summary>Gets whether the full security inspection may be skipped.</summary>
    public bool CanUseTrustedFastPath =>
        Decision == DocumentTrustDecision.TrustedFastPath;
}
