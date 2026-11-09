using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace PiratePDF.Fuzz.Runner;

/// <summary>One registration-specific acknowledgement; only the gate state machine provides replay protection.</summary>
public sealed record CampaignWorkerAcknowledgement(string RegistrationHash, string Proof)
{
    public const int MaximumBytes = 256;
    public override string ToString() => "Campaign worker acknowledgement (redacted)";

    public static CampaignWorkerAcknowledgement Create(CampaignInvocationIdentity identity, CampaignWorkerRegistration registration)
    {
        ArgumentNullException.ThrowIfNull(identity);
        ArgumentNullException.ThrowIfNull(registration);
        _ = CampaignWorkerRegistration.Decode(registration.Encode(), identity);
        string hash = Convert.ToHexString(SHA256.HashData(registration.Encode()));
        return new(hash, Sign(identity, hash));
    }

    public byte[] Encode()
    {
        if (!FindingFiles.Hash(RegistrationHash) || !FindingFiles.Hash(Proof)) throw new ArgumentException("Invalid ACK fields.");
        return JsonSerializer.SerializeToUtf8Bytes(this);
    }

    public static void Validate(ReadOnlySpan<byte> bytes, CampaignInvocationIdentity identity, CampaignWorkerRegistration registration)
    {
        if (bytes.Length is < 1 or > MaximumBytes) throw new ArgumentException("Invalid ACK length.");
        var acknowledgement = JsonSerializer.Deserialize<CampaignWorkerAcknowledgement>(bytes) ?? throw new ArgumentException("Missing ACK.");
        var expected = Create(identity, registration);
        if (!bytes.SequenceEqual(acknowledgement.Encode()) || acknowledgement.RegistrationHash != expected.RegistrationHash ||
            !CryptographicOperations.FixedTimeEquals(Convert.FromHexString(acknowledgement.Proof), Convert.FromHexString(expected.Proof)))
            throw new ArgumentException("Invalid ACK proof.");
    }

    private static string Sign(CampaignInvocationIdentity identity, string hash)
    {
        byte[] key = Convert.FromHexString(identity.Nonce);
        try { return Convert.ToHexString(HMACSHA256.HashData(key, Encoding.UTF8.GetBytes("campaign-worker-ack-v1\n" + hash))); }
        finally { CryptographicOperations.ZeroMemory(key); }
    }
}
