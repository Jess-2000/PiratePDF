
namespace BlackFlag.Engine.Services.Security.Trust;

/// <summary>
/// Carries non-authoritative document metadata that may help explain the
/// provenance of a trust record. Metadata never establishes trust.
/// </summary>
public sealed record DocumentTrustEvidence(
    string? Producer = null,
    string? WriterIdentity = null);
