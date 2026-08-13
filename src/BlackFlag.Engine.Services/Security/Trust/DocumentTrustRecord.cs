
namespace BlackFlag.Engine.Services.Security.Trust;

/// <summary>
/// Represents application-owned evidence that a specific document byte
/// sequence completed a successful security inspection under a specific
/// policy version.
/// </summary>
public sealed record DocumentTrustRecord(
    string Sha256Fingerprint,
    string SecurityPolicyVersion,
    DateTimeOffset ValidatedAtUtc,
    string? WriterIdentity = null)
{
    /// <summary>Gets the expected lowercase hexadecimal SHA-256 length.</summary>
    public const int Sha256HexLength = 64;
}
