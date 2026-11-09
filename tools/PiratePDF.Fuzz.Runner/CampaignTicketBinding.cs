using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace PiratePDF.Fuzz.Runner;

/// <summary>Bind a one-use ticket to the exact launched child, without changing the v1 invocation codec.</summary>
public sealed record CampaignTicketBinding(string InvocationHash, int ChildId, long ChildStartTicks, string Proof)
{
    public const int MaximumBytes = 512;
    public override string ToString() => "Campaign ticket binding (redacted)";

    public static CampaignTicketBinding Create(CampaignInvocationIdentity identity, CampaignProcessIdentity child)
    {
        ArgumentNullException.ThrowIfNull(identity);
        var binding = new CampaignTicketBinding(identity.Digest(), child.ProcessId, child.StartTicks, "");
        binding.Validate(identity);
        return binding with { Proof = binding.Sign(identity) };
    }

    public byte[] Encode()
    {
        if (!FindingFiles.Hash(InvocationHash) || !FindingFiles.Hash(Proof) || ChildId <= 0 ||
            ChildStartTicks <= 0 || ChildStartTicks > DateTime.MaxValue.Ticks)
            throw new ArgumentException("Invalid ticket binding.");
        byte[] bytes = JsonSerializer.SerializeToUtf8Bytes(this);
        if (bytes.Length > MaximumBytes) throw new ArgumentException("Ticket binding exceeds ceiling.");
        return bytes;
    }

    public static CampaignTicketBinding Decode(ReadOnlySpan<byte> bytes, CampaignInvocationIdentity identity,
        CampaignProcessIdentity child)
    {
        ArgumentNullException.ThrowIfNull(identity);
        if (bytes.Length is < 1 or > MaximumBytes) throw new ArgumentException("Invalid ticket binding length.");
        var binding = JsonSerializer.Deserialize<CampaignTicketBinding>(bytes) ?? throw new ArgumentException("Missing binding.");
        binding.Validate(identity);
        if (binding.ChildId != child.ProcessId || binding.ChildStartTicks != child.StartTicks ||
            !FindingFiles.Hash(binding.Proof) || !bytes.SequenceEqual(binding.Encode()) ||
            !CryptographicOperations.FixedTimeEquals(Convert.FromHexString(binding.Proof), Convert.FromHexString(binding.Sign(identity))))
            throw new ArgumentException("Mismatched ticket binding.");
        return binding;
    }

    private void Validate(CampaignInvocationIdentity identity)
    {
        if (InvocationHash != identity.Digest() || ChildId <= 0 || ChildId == identity.ParentId ||
            ChildStartTicks <= 0 || ChildStartTicks > DateTime.MaxValue.Ticks)
            throw new ArgumentException("Invalid admitted child identity.");
    }

    private string Sign(CampaignInvocationIdentity identity)
    {
        byte[] key = Convert.FromHexString(identity.Nonce);
        try
        {
            byte[] message = Encoding.UTF8.GetBytes(FormattableString.Invariant(
                $"campaign-child-binding-v1\n{InvocationHash}\n{ChildId}\n{ChildStartTicks}"));
            return Convert.ToHexString(HMACSHA256.HashData(key, message));
        }
        finally { CryptographicOperations.ZeroMemory(key); }
    }
}
