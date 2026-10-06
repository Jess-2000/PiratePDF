using System.Security.Cryptography;

namespace PiratePDF.Fuzz.Targets;

/// <summary>A bounded outcome containing a digest or a fixed rejection code, never document data.</summary>
public sealed record FuzzResult
{
    private FuzzResult(string outcome, string observation)
    {
        Outcome = outcome;
        Observation = observation;
    }

    public string Outcome { get; }
    public string Observation { get; }

    public static FuzzResult Success(ReadOnlySpan<byte> bytes) =>
        new("success", Convert.ToHexString(SHA256.HashData(bytes)));

    internal static FuzzResult SuccessDigest(string digest)
    {
        ArgumentNullException.ThrowIfNull(digest);
        if (digest.Length != 64 || digest.Any(static value => !char.IsAsciiHexDigitUpper(value)))
        {
            throw new ArgumentException("Expected an uppercase SHA-256 digest.", nameof(digest));
        }

        return new("success", digest);
    }

    public static FuzzResult Rejected(string reason)
    {
        ArgumentException.ThrowIfNullOrEmpty(reason);
        if (reason.Length > 64 || reason.Any(static value =>
                !char.IsAsciiLetterOrDigit(value) && value is not '-' and not '_'))
        {
            throw new ArgumentException("Expected a bounded rejection code, not an exception message.", nameof(reason));
        }

        return new("rejected", reason);
    }
}
