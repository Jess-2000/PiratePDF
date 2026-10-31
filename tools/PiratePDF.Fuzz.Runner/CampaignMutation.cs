using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using PiratePDF.Fuzz.Targets;

namespace PiratePDF.Fuzz.Runner;

public sealed record CampaignIdentity(string Algorithm, int Seed, string Target, int ConfigurationVersion,
    string RootSeed, int Index, string ParentSha256, string Operation, int Offset, int Count, byte Value, string OutputSha256);
public sealed record CampaignCandidate(CampaignIdentity Identity, byte[] Bytes);

public static class CampaignMutation
{
    public const string Algorithm = "deterministic-v2";
    public const int Seed = 1049;

    public static CampaignCandidate Generate(string targetId, string rootSeed, int index, ReadOnlySpan<byte> source)
    {
        if (index is < 0 or >= CampaignConfiguration.MaximumCases * CampaignConfiguration.DuplicateRetries)
            throw new ArgumentOutOfRangeException(nameof(index));
        if (!FindingFiles.Id(rootSeed)) throw new ArgumentException("Invalid owned seed identity.");
        FuzzTarget target = TargetRegistry.Get(targetId);
        if (source.Length > target.MaximumInputBytes) throw new ArgumentOutOfRangeException(nameof(source));
        string parent = Convert.ToHexString(SHA256.HashData(source));
        string identity = string.Create(CultureInfo.InvariantCulture, $"{Algorithm}\n{Seed}\n{targetId}\n1\n{rootSeed}\n{index}\n{parent}");
        byte[] choice = SHA256.HashData(Encoding.UTF8.GetBytes(identity));
        AppliedMutation mutation = MutationBytes.Apply(source, target.MaximumInputBytes, (MutationOperator)(choice[7] % 7), choice);
        return new(new(Algorithm, Seed, targetId, 1, rootSeed, index, parent, mutation.Operation.ToString(),
            mutation.Offset, mutation.Count, mutation.Value, FindingFiles.Digest(mutation.Bytes)), mutation.Bytes);
    }
}
