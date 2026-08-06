using System.Buffers;
using BlackFlag.Engine.Objects;

namespace BlackFlag.Engine.Filters;

/// <summary>
/// Decodes PDF <c>/RunLengthDecode</c> streams.
/// </summary>
public sealed class PdfRunLengthFilterDecoder : IPdfFilterDecoder
{
    private static readonly PdfName FilterName =
        new("RunLengthDecode");

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

        int offset = 0;
        bool terminated = false;

        while (offset < encodedData.Length)
        {
            context.Budget.ThrowIfCancellationRequested();

            byte lengthByte = encodedData[offset++];

            if (lengthByte == 128)
            {
                terminated = true;

                if (offset != encodedData.Length)
                {
                    throw new PdfFilterDecodeException(
                        PdfFilterDecodeFailure.InvalidData,
                        "RunLengthDecode contains data after the end-of-data marker.");
                }

                break;
            }

            if (lengthByte <= 127)
            {
                int literalCount =
                    lengthByte + 1;

                if (literalCount > encodedData.Length - offset)
                {
                    throw new PdfFilterDecodeException(
                        PdfFilterDecodeFailure.InvalidData,
                        "RunLengthDecode contains an incomplete literal run.");
                }

                Write(
                    destination,
                    encodedData.Slice(
                        offset,
                        literalCount));

                offset += literalCount;
                continue;
            }

            if (offset >= encodedData.Length)
            {
                throw new PdfFilterDecodeException(
                    PdfFilterDecodeFailure.InvalidData,
                    "RunLengthDecode contains an incomplete repeated run.");
            }

            int repeatedCount =
                257 -
                lengthByte;

            byte repeatedValue =
                encodedData[offset++];

            Span<byte> output =
                destination.GetSpan(
                    repeatedCount);

            output[..repeatedCount]
                .Fill(repeatedValue);

            destination.Advance(
                repeatedCount);
        }

        if (!terminated)
        {
            throw new PdfFilterDecodeException(
                PdfFilterDecodeFailure.InvalidData,
                "RunLengthDecode data is missing the end-of-data marker.");
        }

        context.Budget.ThrowIfCancellationRequested();
    }

    private static void Write(
        IBufferWriter<byte> destination,
        ReadOnlySpan<byte> data)
    {
        Span<byte> output =
            destination.GetSpan(
                data.Length);

        data.CopyTo(output);
        destination.Advance(
            data.Length);
    }
}

