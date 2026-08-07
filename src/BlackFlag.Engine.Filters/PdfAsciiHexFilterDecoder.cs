using System.Buffers;
using BlackFlag.Engine.Objects;

namespace BlackFlag.Engine.Filters;

// Decodes PDF <c>/ASCIIHexDecode</c> streams.
public sealed class PdfAsciiHexFilterDecoder : IPdfFilterDecoder
{
    private static readonly PdfName FilterName =
        new("ASCIIHexDecode");

    //  <inheritdoc />
    public PdfName Name => FilterName;

    //  <inheritdoc />
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

            byte value =
                encodedData[index];

            if (PdfFilterByteClassifier.IsWhiteSpace(value))
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

            int nibble =
                GetHexValue(value);

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
        foreach (byte value in trailingData)
        {
            if (!PdfFilterByteClassifier.IsWhiteSpace(value))
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
        return value switch
        {
            >= (byte)'0' and <= (byte)'9' =>
                value -
                (byte)'0',

            >= (byte)'A' and <= (byte)'F' =>
                value -
                (byte)'A' +
                10,

            >= (byte)'a' and <= (byte)'f' =>
                value -
                (byte)'a' +
                10,

            _ => -1
        };
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
