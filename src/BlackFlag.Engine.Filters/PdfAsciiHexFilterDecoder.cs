using System.Buffers;
using BlackFlag.Engine.Objects;

namespace BlackFlag.Engine.Filters;

/// <summary>
/// Decodes PDF <c>/ASCIIHexDecode</c> streams.
/// </summary>
public sealed class PdfAsciiHexFilterDecoder : IPdfFilterDecoder
{
    private static readonly PdfName FilterName =
        new("ASCIIHexDecode");

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

        int highNibble = -1;
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

            if (value == (byte)'>')
            {
                terminated = true;

                EnsureOnlyTrailingWhiteSpace(
                    encodedData[(index + 1)..]);

                break;
            }

            int nibble = GetHexValue(value);

            if (nibble < 0)
            {
                throw new PdfFilterDecodeException(
                    PdfFilterDecodeFailure.InvalidData,
                    $"ASCIIHexDecode encountered invalid byte 0x{value:X2} at offset {index}.");
            }

            if (highNibble < 0)
            {
                highNibble = nibble;
                continue;
            }

            WriteByte(
                destination,
                (byte)(
                    (highNibble << 4) |
                    nibble));

            highNibble = -1;
        }

        if (!terminated)
        {
            throw new PdfFilterDecodeException(
                PdfFilterDecodeFailure.InvalidData,
                "ASCIIHexDecode data is missing the required '>' end marker.");
        }

        if (highNibble >= 0)
        {
            WriteByte(
                destination,
                (byte)(
                    highNibble << 4));
        }

        context.Budget.ThrowIfCancellationRequested();
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
                    "ASCIIHexDecode contains non-whitespace data after the end marker.");
            }
        }
    }

    private static int GetHexValue(
        byte value)
    {
        if (
            value >= (byte)'0' &&
            value <= (byte)'9')
        {
            return value - (byte)'0';
        }

        if (
            value >= (byte)'A' &&
            value <= (byte)'F')
        {
            return value - (byte)'A' + 10;
        }

        if (
            value >= (byte)'a' &&
            value <= (byte)'f')
        {
            return value - (byte)'a' + 10;
        }

        return -1;
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

    private static void WriteByte(
        IBufferWriter<byte> destination,
        byte value)
    {
        Span<byte> output =
            destination.GetSpan(1);

        output[0] = value;
        destination.Advance(1);
    }
}
