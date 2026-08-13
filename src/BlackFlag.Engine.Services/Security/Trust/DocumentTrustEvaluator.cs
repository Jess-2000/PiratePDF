
using System.Buffers;
using System.Security.Cryptography;

namespace BlackFlag.Engine.Services.Security.Trust;

/// <summary>
/// Performs the local fingerprint-backed trusted-document opening check.
/// Metadata hints never grant trust and no trust state is persisted here.
/// </summary>
public sealed class DocumentTrustEvaluator
{
    private const int HashBufferSize = 64 * 1024;

    /// <summary>Evaluates a seekable document stream against local trust.</summary>
    public static DocumentTrustEvaluation Evaluate(
        Stream document,
        DocumentTrustRecord? trustRecord,
        string securityPolicyVersion,
        DocumentTrustEvidence? evidence = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentException.ThrowIfNullOrWhiteSpace(securityPolicyVersion);

        if (!document.CanRead)
        {
            throw new ArgumentException(
                "The document stream must be readable.",
                nameof(document));
        }

        if (!document.CanSeek)
        {
            throw new ArgumentException(
                "The trusted fast path requires a seekable stream so opening semantics remain unchanged.",
                nameof(document));
        }

        cancellationToken.ThrowIfCancellationRequested();

        long originalPosition = document.Position;

        try
        {
            document.Position = 0;

            string fingerprint =
                ComputeSha256(
                    document,
                    cancellationToken);

            bool trusted =
                IsCurrentTrust(
                    trustRecord,
                    fingerprint,
                    securityPolicyVersion,
                    evidence);

            return new(
                trusted
                    ? DocumentTrustDecision.TrustedFastPath
                    : DocumentTrustDecision.FullInspectionRequired,
                fingerprint);
        }
        finally
        {
            document.Position = originalPosition;
        }
    }

    private static bool IsCurrentTrust(
        DocumentTrustRecord? trustRecord,
        string fingerprint,
        string securityPolicyVersion,
        DocumentTrustEvidence? evidence)
    {
        if (trustRecord is null ||
            trustRecord.Sha256Fingerprint.Length !=
                DocumentTrustRecord.Sha256HexLength ||
            !string.Equals(
                trustRecord.SecurityPolicyVersion,
                securityPolicyVersion,
                StringComparison.Ordinal) ||
            !string.Equals(
                trustRecord.Sha256Fingerprint,
                fingerprint,
                StringComparison.Ordinal))
        {
            return false;
        }

        // Writer metadata is explanatory only. When both the local record and
        // current metadata contain a writer identity, a mismatch invalidates
        // the fast path; a metadata match can never establish trust by itself.
        return trustRecord.WriterIdentity is null ||
            evidence?.WriterIdentity is null ||
            string.Equals(
                trustRecord.WriterIdentity,
                evidence.WriterIdentity,
                StringComparison.Ordinal);
    }

    private static string ComputeSha256(
        Stream document,
        CancellationToken cancellationToken)
    {
        using IncrementalHash hash =
            IncrementalHash.CreateHash(
                HashAlgorithmName.SHA256);

        byte[] buffer =
            ArrayPool<byte>.Shared.Rent(
                HashBufferSize);

        try
        {
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();

                int read =
                    document.Read(
                        buffer,
                        0,
                        HashBufferSize);

                if (read == 0)
                {
                    break;
                }

                hash.AppendData(
                    buffer,
                    0,
                    read);
            }

            return Convert.ToHexString(
                    hash.GetHashAndReset())
                .ToLowerInvariant();
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }
}
