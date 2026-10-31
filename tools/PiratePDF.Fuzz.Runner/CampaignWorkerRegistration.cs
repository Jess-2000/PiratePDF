using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace PiratePDF.Fuzz.Runner;

/// <summary>Authenticated metadata; neither replay protection nor evidence that the process exists or exited.</summary>
public sealed record CampaignWorkerRegistration(string InvocationHash, int CaseIndex,
    int ProcessId, long StartTicks, string Proof)
{
    public const int MaximumBytes = 512;
    public int Version { get; init; } = 1;
    public override string ToString() => "Campaign worker registration (redacted)";

    public static CampaignWorkerRegistration Create(CampaignInvocationIdentity identity,
        int caseIndex, int processId, long startTicks)
    {
        ArgumentNullException.ThrowIfNull(identity);
        var registration = new CampaignWorkerRegistration(identity.Digest(), caseIndex, processId, startTicks, "");
        registration.ValidateFields(identity);
        return registration with { Proof = registration.Sign(identity) };
    }

    public byte[] Encode()
    {
        if (Version != 1 || !FindingFiles.Hash(InvocationHash) || !FindingFiles.Hash(Proof) ||
            CaseIndex is < 0 or >= CampaignConfiguration.MaximumCases || ProcessId <= 0 ||
            StartTicks <= 0 || StartTicks > DateTime.MaxValue.Ticks)
            throw new ArgumentException("Invalid registration fields.");
        byte[] bytes = JsonSerializer.SerializeToUtf8Bytes(this);
        if (bytes.Length > MaximumBytes) throw new ArgumentException("Registration exceeds protocol ceiling.");
        return bytes;
    }

    public static CampaignWorkerRegistration Decode(ReadOnlySpan<byte> bytes, CampaignInvocationIdentity identity)
    {
        ArgumentNullException.ThrowIfNull(identity);
        if (bytes.Length is < 1 or > MaximumBytes) throw new ArgumentException("Invalid registration length.");
        var registration = JsonSerializer.Deserialize<CampaignWorkerRegistration>(bytes) ??
            throw new ArgumentException("Missing registration.");
        registration.ValidateFields(identity);
        if (!FindingFiles.Hash(registration.Proof) || !bytes.SequenceEqual(registration.Encode()) ||
            !CryptographicOperations.FixedTimeEquals(Convert.FromHexString(registration.Proof),
                Convert.FromHexString(registration.Sign(identity))))
            throw new ArgumentException("Invalid registration proof.");
        return registration;
    }

    private void ValidateFields(CampaignInvocationIdentity identity)
    {
        if (Version != 1 || InvocationHash != identity.Digest() || CaseIndex < 0 || CaseIndex >= identity.Cases ||
            ProcessId <= 0 || ProcessId == identity.ParentId || StartTicks <= 0 || StartTicks > DateTime.MaxValue.Ticks)
            throw new ArgumentException("Invalid worker identity.");
    }

    private string Sign(CampaignInvocationIdentity identity)
    {
        byte[] key = Convert.FromHexString(identity.Nonce);
        try
        {
            byte[] message = Encoding.UTF8.GetBytes(FormattableString.Invariant(
                $"campaign-worker-v1\n{InvocationHash}\n{CaseIndex}\n{ProcessId}\n{StartTicks}"));
            return Convert.ToHexString(HMACSHA256.HashData(key, message));
        }
        finally { CryptographicOperations.ZeroMemory(key); }
    }
}
