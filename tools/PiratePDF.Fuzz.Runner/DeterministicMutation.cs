using System.Buffers.Binary;
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
        uint position = BinaryPrimitives.ReadUInt32LittleEndian(choice);
        int length = source.Length;
        int offset = length == 0 ? 0 : (int)(position % (uint)length);
        byte value = choice[4];
        int amount = 1 + choice[5] % 8;

        if (operation is MutationOperator.Insert or MutationOperator.Extend && length == target.MaximumInputBytes)
            operation = MutationOperator.Replace;
        if (length == 0 && operation is MutationOperator.BitFlip or MutationOperator.Replace or MutationOperator.Overwrite)
            operation = MutationOperator.Insert;

        int count = operation switch
        {
            MutationOperator.Insert => 1,
            MutationOperator.Extend => Math.Min(amount, target.MaximumInputBytes - length),
            MutationOperator.Delete => Math.Min(amount, length - offset),
            MutationOperator.Truncate => length == 0 ? 0 : length - offset,
            _ => 0
        };
        int outputLength = operation switch
        {
            MutationOperator.Insert or MutationOperator.Extend => length + count,
            MutationOperator.Delete or MutationOperator.Truncate => length - count,
            _ => length
        };
        byte[] output = new byte[outputLength];
        switch (operation)
        {
            case MutationOperator.Insert:
                offset = (int)(position % (uint)(length + 1));
                source[..offset].CopyTo(output);
                output[offset] = value;
                source[offset..].CopyTo(output.AsSpan(offset + 1));
                break;
            case MutationOperator.Delete:
                source[..offset].CopyTo(output);
                source[(offset + count)..].CopyTo(output.AsSpan(offset));
                break;
            case MutationOperator.Truncate:
                source[..outputLength].CopyTo(output);
                break;
            default:
                source.CopyTo(output);
                switch (operation)
                {
                    case MutationOperator.BitFlip: output[offset] ^= (byte)(1 << (choice[6] % 8)); break;
                    case MutationOperator.Replace: output[offset] = value == source[offset] ? (byte)(value ^ 1) : value; break;
                    case MutationOperator.Overwrite: output.AsSpan(offset, Math.Min(amount, length - offset)).Fill(value); break;
                    case MutationOperator.Extend: output.AsSpan(length).Fill(value); break;
                }
                break;
        }
        return output;
    }
}
