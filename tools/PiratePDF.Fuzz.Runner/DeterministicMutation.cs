using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using PiratePDF.Fuzz.Targets;

namespace PiratePDF.Fuzz.Runner;

public enum MutationOperator { BitFlip, Replace, Insert, Delete, Overwrite, Truncate, Extend }

/// <summary>Pure deterministic-v1 byte generation. This does not execute targets or accept source paths.</summary>
public static class DeterministicMutation
{
    public const string Algorithm = "deterministic-v1";
    public const int FixedSeed = 1049;
    public const int MutationsPerSeed = 7;
    public const int MaximumAttempts = 32;

    public static MutationOperator Operator(int index)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(index);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(index, MutationsPerSeed);
        return (MutationOperator)index;
    }

    public static byte[] Generate(string targetId, string seedId, int configurationVersion, int index, ReadOnlySpan<byte> source)
    {
        MutationOperator operation = Operator(index);
        FuzzTarget target = TargetRegistry.Get(targetId);
        if (configurationVersion != target.ConfigurationVersion || string.IsNullOrEmpty(seedId) || seedId.Length > 64 ||
            seedId.Any(static ch => ch is not (>= 'a' and <= 'z' or >= '0' and <= '9' or '-')))
            throw new ArgumentException("Invalid mutation identity.");
        if (source.Length > target.MaximumInputBytes) throw new ArgumentOutOfRangeException(nameof(source));

        // Explicit ASCII/UTF-8 framing, invariant decimal and SHA-256 avoid runtime PRNG/version dependence.
        string identity = string.Create(CultureInfo.InvariantCulture,
            $"{Algorithm}\n{FixedSeed}\n{targetId}\n{seedId}\n{configurationVersion}\n{index}\n{Convert.ToHexString(SHA256.HashData(source))}");
        byte[] choice = SHA256.HashData(Encoding.UTF8.GetBytes(identity));
        return MutationBytes.Apply(source, target.MaximumInputBytes, operation, choice).Bytes;
    }
}
