using System.Buffers.Binary;

namespace PiratePDF.Fuzz.Runner;

internal sealed record AppliedMutation(byte[] Bytes, MutationOperator Operation, int Offset, int Count, byte Value);

/// <summary>Shared bounded byte operations; deterministic-v1 choice framing remains unchanged.</summary>
internal static class MutationBytes
{
    internal static AppliedMutation Apply(ReadOnlySpan<byte> source, int maximum, MutationOperator operation, byte[] choice)
    {
        uint position = BinaryPrimitives.ReadUInt32LittleEndian(choice);
        int length = source.Length;
        int offset = length == 0 ? 0 : (int)(position % (uint)length);
        byte value = choice[4];
        int amount = 1 + choice[5] % 8;

        if (operation is MutationOperator.Insert or MutationOperator.Extend && length == maximum)
            operation = MutationOperator.Replace;
        if (length == 0 && operation is MutationOperator.BitFlip or MutationOperator.Replace or MutationOperator.Overwrite)
            operation = MutationOperator.Insert;

        int count = operation switch
        {
            MutationOperator.Insert => 1,
            MutationOperator.Extend => Math.Min(amount, maximum - length),
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
        int affected = operation == MutationOperator.Overwrite ? Math.Min(amount, length - offset) : count;
        byte parameter = operation == MutationOperator.BitFlip ? (byte)(1 << (choice[6] % 8)) :
            operation == MutationOperator.Replace && length > 0 ? output[offset] : value;
        return new(output, operation, offset, affected, parameter);
    }
}
