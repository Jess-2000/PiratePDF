
namespace BlackFlag.Engine.Services.Security.Trust;

/// <summary>
/// Creates local trust records only after the caller has established that a
/// complete security inspection succeeded.
/// </summary>
public static class DocumentTrustRecordFactory
{
    /// <summary>Creates a record from a completed full-inspection fingerprint.</summary>
    public static DocumentTrustRecord CreateAfterSuccessfulInspection(
        DocumentTrustEvaluation evaluation,
        string securityPolicyVersion,
        DateTimeOffset validatedAtUtc,
        string? writerIdentity = null)
    {
        ArgumentNullException.ThrowIfNull(evaluation);
        ArgumentException.ThrowIfNullOrWhiteSpace(securityPolicyVersion);

        // The evaluator deliberately returns the current fingerprint even
        // when no prior trust exists. The application may persist this record
        // only after the full security inspection has succeeded.
        return new(
            evaluation.Sha256Fingerprint,
            securityPolicyVersion,
            validatedAtUtc,
            writerIdentity);
    }
}
