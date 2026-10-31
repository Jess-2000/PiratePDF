using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using PiratePDF.Fuzz.Targets;

namespace PiratePDF.Fuzz.Runner;

/// <summary>Bounded canonical control frame; sequence and lifecycle admission belong to the broker.</summary>
public sealed record CampaignBrokerMessage(int Sequence, string Kind, string Data, string Proof)
{
    public const int MaximumBytes = 8192;
    public override string ToString() => "Campaign broker frame (redacted)";
    public static CampaignBrokerMessage Create(CampaignInvocationIdentity identity, int sequence, string kind, string data)
    {
        ArgumentNullException.ThrowIfNull(identity);
        _ = identity.Encode();
        var frame = new CampaignBrokerMessage(sequence, kind, data, "");
        frame.ValidateFields();
        return frame with { Proof = frame.Sign(identity) };
    }
    public byte[] Encode()
    {
        ValidateFields();
        if (!FindingFiles.Hash(Proof)) throw new ArgumentException("Invalid control proof.");
        byte[] bytes = JsonSerializer.SerializeToUtf8Bytes(this);
        if (bytes.Length > MaximumBytes) throw new ArgumentException("Control frame exceeds ceiling.");
        return bytes;
    }
    public static CampaignBrokerMessage Decode(ReadOnlySpan<byte> bytes, CampaignInvocationIdentity identity, int sequence, string kind)
    {
        if (bytes.Length is < 1 or > MaximumBytes) throw new ArgumentException("Invalid control length.");
        var frame = JsonSerializer.Deserialize<CampaignBrokerMessage>(bytes) ?? throw new ArgumentException("Missing control frame.");
        if (frame.Sequence != sequence || frame.Kind != kind || !bytes.SequenceEqual(frame.Encode()) ||
            !CryptographicOperations.FixedTimeEquals(Convert.FromHexString(frame.Proof), Convert.FromHexString(frame.Sign(identity))))
            throw new ArgumentException("Mismatched control frame.");
        return frame;
    }
    private void ValidateFields()
    {
        if (Sequence is < 0 or > 60010 || Kind is not ("hello" or "ready" or "begin" or "case" or "result" or "end" or "finish") ||
            Data is null || Encoding.UTF8.GetByteCount(Data) > 4096) throw new ArgumentException("Invalid control fields.");
    }
    private string Sign(CampaignInvocationIdentity identity)
    {
        byte[] key = Convert.FromHexString(identity.Nonce);
        try { return Convert.ToHexString(HMACSHA256.HashData(key, Encoding.UTF8.GetBytes(
            FormattableString.Invariant($"campaign-control-v1\n{identity.Digest()}\n{Sequence}\n{Kind}\n{Data}")))); }
        finally { CryptographicOperations.ZeroMemory(key); }
    }
}

public sealed record CampaignCaseRequest(string Target, int Case, int Attempt, string InputHash, int Length, int Work, long RemainingTicks);

public sealed record CampaignWorkerReply(SupervisionStatus Status, string Target, int? ExitCode, string? Code,
    int Stdout, int Stderr, bool StdoutTruncated, bool StderrTruncated)
{
    internal static CampaignWorkerReply From(SupervisionResult result) => new(result.Status, result.TargetId!, result.ExitCode,
        result.WorkerCode, result.StdoutRetainedBytes, result.StderrRetainedBytes, result.StdoutTruncated, result.StderrTruncated);
    internal SupervisionResult ToResult()
    {
        if (!Enum.IsDefined(Status) || Stdout is < 0 or > WorkerSupervisor.MaximumRetainedBytes || Stderr is < 0 or > WorkerSupervisor.MaximumRetainedBytes ||
            Code is not (null or "TargetResult" or "UnexpectedTargetFailure" or "UnexpectedRunnerFailure" or "InputUnavailable" or "InputTooLarge" or "UnsupportedInputPlatform" or "Cancelled"))
            throw new ArgumentException("Invalid worker reply.");
        return new(this, TargetRegistry.Get(Target));
    }
}

internal static class CampaignControlJson
{
    internal static string Encode<T>(T value) => JsonSerializer.Serialize(value);
    internal static T Decode<T>(string text)
    {
        T value = JsonSerializer.Deserialize<T>(text) ?? throw new ArgumentException("Missing control data.");
        if (text != Encode(value)) throw new ArgumentException("Noncanonical control data.");
        return value;
    }
    internal static string Case(CampaignCandidate candidate, SupervisionResult worker)
    {
        bool completed = worker.Status == SupervisionStatus.Completed && worker.ExitCode is 0 or 1 && worker.WorkerCode == "TargetResult";
        return Encode(new { schema = "piratepdf-mutation-case", version = 1, identity = candidate.Identity,
            length = candidate.Bytes.Length, worker = worker.ToJson(), completed, mutationOnly = true, reproduction = "explicit-pp-aud-037-only" });
    }
}
