using System.Buffers;
using System.IO.Compression;
using BlackFlag.Engine.Objects;

namespace BlackFlag.Engine.Filters;

/// <summary>
/// Decodes PDF <c>/FlateDecode</c> streams using bounded zlib
/// decompression and optional TIFF or PNG prediction.
/// </summary>
internal delegate void PdfFlateChunkConsumer(ReadOnlySpan<byte> data);

public sealed class PdfFlateFilterDecoder : IPdfFilterDecoder
{
    private static readonly PdfName FilterName =
        new("FlateDecode");

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

        PdfFlateDecodeParameters parameters =
            PdfFlateDecodeParameters.FromDictionary(
                context.Parameters);

        context.Budget.ThrowIfCancellationRequested();

        if (parameters.Predictor == 1)
        {
            DecodeDirectly(
                encodedData,
                destination,
                context.Budget);

            return;
        }

        using var decompressed =
            new PooledByteAccumulator(
                context.Budget.Limits.BufferSize,
                context.Budget.Limits.MaximumDecodedBytes,
                encodedData.Length,
                context.Budget.Limits.MaximumExpansionRatio);

        Decompress(
            encodedData,
            context.Budget,
            decompressed.Append);

        PdfPredictorDecoder.Decode(
            decompressed.AsSpan(),
            destination,
            parameters,
            context.Budget);
    }

    private static void DecodeDirectly(
        ReadOnlySpan<byte> encodedData,
        IBufferWriter<byte> destination,
        IPdfFilterDecodeBudget budget)
    {
        Decompress(
            encodedData,
            budget,
            data =>
            {
                Span<byte> output =
                    destination.GetSpan(
                        data.Length);

                data.CopyTo(output);
                destination.Advance(
                    data.Length);
            });
    }

    private static void Decompress(
        ReadOnlySpan<byte> encodedData,
        IPdfFilterDecodeBudget budget,
        PdfFlateChunkConsumer consume)
    {
        if (encodedData.IsEmpty)
        {
            throw new PdfFilterDecodeException(
                PdfFilterDecodeFailure.InvalidData,
                "A FlateDecode stream cannot be empty.");
        }

        byte[] encodedCopy =
            encodedData.ToArray();

        byte[] buffer =
            ArrayPool<byte>.Shared.Rent(
                budget.Limits.BufferSize);

        try
        {
            using var source =
                new MemoryStream(
                    encodedCopy,
                    writable: false);

            using var stream =
                new ZLibStream(
                    source,
                    CompressionMode.Decompress,
                    leaveOpen: false);

            while (true)
            {
                budget.ThrowIfCancellationRequested();

                int read =
                    stream.Read(
                        buffer,
                        0,
                        buffer.Length);

                if (read == 0)
                {
                    break;
                }

                consume(
                    buffer.AsSpan(
                        0,
                        read));
            }
        }
        catch (PdfFilterDecodeException)
        {
            throw;
        }
        catch (InvalidDataException exception)
        {
            throw new PdfFilterDecodeException(
                PdfFilterDecodeFailure.InvalidData,
                "The FlateDecode stream contains invalid or incomplete zlib data.",
                exception);
        }
        finally
        {
            buffer.AsSpan().Clear();
            ArrayPool<byte>.Shared.Return(buffer);
            encodedCopy.AsSpan().Clear();
        }
    }
}

