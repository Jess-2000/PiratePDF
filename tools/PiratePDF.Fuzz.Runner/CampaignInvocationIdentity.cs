using System.Security.Cryptography;
using System.Text.Json;

namespace PiratePDF.Fuzz.Runner;

/// <summary>Portable protocol policy only; callers must separately establish admission and process ownership.</summary>
public sealed record CampaignInvocationIdentity(
    string Run, string Revision, string Target, int Cases, long DurationTicks,
    int ProcessedBytes, int PoolInputs, int PoolBytes, int TelemetryBytes,
    string RepositoryHash, string RunnerHash, string TargetsHash, string Nonce,
    int ParentId, long ParentStartTicks)
{
    public const int MaximumBytes = 2048;
    public string Algorithm { get; init; } = "deterministic-v2";
    public int Seed { get; init; } = 1049;
    public int Version { get; init; } = 1;

    // Never include the nonce in diagnostic object formatting.
    public override string ToString() => "Campaign invocation identity (redacted)";

    public byte[] Encode()
    {
        Validate();
        byte[] bytes = JsonSerializer.SerializeToUtf8Bytes(this);
        if (bytes.Length > MaximumBytes) throw new ArgumentException("Invocation exceeds protocol ceiling.");
        return bytes;
    }

    public string Digest() => Convert.ToHexString(SHA256.HashData(Encode()));

    public static CampaignInvocationIdentity Decode(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length is < 1 or > MaximumBytes) throw new ArgumentException("Invalid invocation length.");
        var identity = JsonSerializer.Deserialize<CampaignInvocationIdentity>(bytes) ??
            throw new ArgumentException("Missing invocation.");
        if (!bytes.SequenceEqual(identity.Encode())) throw new ArgumentException("Noncanonical invocation.");
        return identity;
    }

    private void Validate()
    {
        if (Version != 1 || Algorithm != "deterministic-v2" || Seed != 1049 ||
            !FindingFiles.Id(Run) || Run is "pending" or "proposal" or "proposal-work" || Revision is not { Length: 40 } ||
            !Revision.All(static c => c is >= '0' and <= '9' or >= 'a' and <= 'f') ||
            Target is not ("all" or "filter-asciihex" or "filter-flate" or "font-sanitize") ||
            !FindingFiles.Hash(RepositoryHash) || !FindingFiles.Hash(RunnerHash) ||
            !FindingFiles.Hash(TargetsHash) || !FindingFiles.Hash(Nonce) ||
            ParentId <= 0 || ParentStartTicks <= 0 || ParentStartTicks > DateTime.MaxValue.Ticks)
            throw new ArgumentException("Invalid invocation identity.");
        _ = new CampaignConfiguration(Cases, TimeSpan.FromTicks(DurationTicks), ProcessedBytes,
            PoolInputs, PoolBytes, TelemetryBytes);
    }
}
