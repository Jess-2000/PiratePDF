using System.Buffers;
using BlackFlag.Engine.Objects;

namespace BlackFlag.Engine.Filters;

/// <summary>
/// Decodes PDF <c>/ASCII85Decode</c> streams.
/// </summary>
public sealed class PdfAscii85FilterDecoder : IPdfFilterDecoder
{
    private const uint MaximumTupleValue = uint.MaxValue;

    private static readonly PdfName FilterName =
        new("ASCII85Decode");

    /// <inheritdoc />
    public PdfName Name => FilterName;

    /// <inheritdoc />
    public void Decode(
        ReadOnlySpan<byte> encodedData,
        IBufferWriter<byte> destination,
        PdfFilterDecodeContext context)
    {
        ArgumentNullException.ThrowIfNull(destination);
        ArgumentNullException.ThrowIfNull(context);

        Span<byte> group =
            stackalloc byte[5];

        int groupCount = 0;
        bool terminated = false;

        for (int index = 0; index < encodedData.Length; index++)
        {
            if ((index & 0x0FFF) == 0)
            {
                context.Budget.ThrowIfCancellationRequested();
            }

            byte value = encodedData[index];

            if (IsWhiteSpace(value))
            {
                continue;
            }

            if (value == (byte)'~')
            {
                int markerEnd =
                    FindMarkerEnd(
                        encodedData,
                        index + 1);

                EnsureOnlyTrailingWhiteSpace(
                    encodedData[(markerEnd + 1)..]);

                terminated = true;
                DecodePartialGroup(
                    group,
                    groupCount,
                    destination);

                break;
            }

            if (value == (byte)'z')
            {
                if (groupCount != 0)
                {
                    throw new PdfFilterDecodeException(
                        PdfFilterDecodeFailure.InvalidData,
                        "ASCII85Decode shorthand 'z' is only valid at a group boundary.");
                }

                WriteTuple(
                    destination,
                    0,
                    4);

                continue;
            }

            if (
                value < (byte)'!' ||
                value > (byte)'u')
            {
                throw new PdfFilterDecodeException(
                    PdfFilterDecodeFailure.InvalidData,
                    $"ASCII85Decode encountered invalid byte 0x{value:X2} at offset {index}.");
            }

            group[groupCount] =
                (byte)(
                    value -
                    (byte)'!');

            groupCount++;

            if (groupCount == 5)
            {
                uint tuple =
                    DecodeTuple(group);

                WriteTuple(
                    destination,
                    tuple,
                    4);

                groupCount = 0;
            }
        }

        if (!terminated)
        {
            throw new PdfFilterDecodeException(
                PdfFilterDecodeFailure.InvalidData,
                "ASCII85Decode data is missing the required '~>' end marker.");
        }

        context.Budget.ThrowIfCancellationRequested();
    }

    private static int FindMarkerEnd(
        ReadOnlySpan<byte> encodedData,
        int startIndex)
    {
        for (int index = startIndex; index < encodedData.Length; index++)
        {
            byte value = encodedData[index];

            if (IsWhiteSpace(value))
            {
                continue;
            }

            if (value == (byte)'>')
            {
                return index;
            }

            throw new PdfFilterDecodeException(
                PdfFilterDecodeFailure.InvalidData,
                "ASCII85Decode '~' must be followed by the '>' end marker.");
        }

        throw new PdfFilterDecodeException(
            PdfFilterDecodeFailure.InvalidData,
            "ASCII85Decode contains an incomplete '~>' end marker.");
    }

    private static void DecodePartialGroup(
        Span<byte> group,
        int groupCount,
        IBufferWriter<byte> destination)
    {
        if (groupCount == 0)
        {
            return;
        }

        if (groupCount == 1)
        {
            throw new PdfFilterDecodeException(
                PdfFilterDecodeFailure.InvalidData,
                "ASCII85Decode contains an invalid one-character final group.");
        }

        for (int index = groupCount; index < 5; index++)
        {
            group[index] = 84;
        }

        uint tuple =
            DecodeTuple(group);

        WriteTuple(
            destination,
            tuple,
            groupCount - 1);
    }

    private static uint DecodeTuple(
        ReadOnlySpan<byte> group)
    {
        ulong value = 0;

        for (int index = 0; index < 5; index++)
        {
            value =
                checked(
                    (value * 85) +
                    group[index]);
        }

        if (value > MaximumTupleValue)
        {
            throw new PdfFilterDecodeException(
                PdfFilterDecodeFailure.InvalidData,
                "ASCII85Decode group exceeds the 32-bit tuple range.");
        }

        return (uint)value;
    }

    private static void WriteTuple(
        IBufferWriter<byte> destination,
        uint tuple,
        int byteCount)
    {
        Span<byte> output =
            destination.GetSpan(byteCount);

        if (byteCount > 0)
        {
            output[0] =
                (byte)(
                    tuple >> 24);
        }

        if (byteCount > 1)
        {
            output[1] =
                (byte)(
                    tuple >> 16);
        }

        if (byteCount > 2)
        {
            output[2] =
                (byte)(
                    tuple >> 8);
        }

        if (byteCount > 3)
        {
            output[3] =
                (byte)tuple;
        }

        destination.Advance(byteCount);
    }

    private static void EnsureOnlyTrailingWhiteSpace(
        ReadOnlySpan<byte> trailingData)
    {
        for (int index = 0; index < trailingData.Length; index++)
        {
            if (!IsWhiteSpace(trailingData[index]))
            {
                throw new PdfFilterDecodeException(
                    PdfFilterDecodeFailure.InvalidData,
                    "ASCII85Decode contains non-whitespace data after the end marker.");
            }
        }
    }

    private static bool IsWhiteSpace(
        byte value)
    {
        return value is
            0x00 or
            0x09 or
            0x0A or
            0x0C or
            0x0D or
            0x20;
    }
}

