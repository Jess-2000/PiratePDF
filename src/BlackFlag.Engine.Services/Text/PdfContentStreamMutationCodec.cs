using System.IO.Compression;
using BlackFlag.Engine.Document.Objects;
using BlackFlag.Engine.Document.Objects.Primitives;
using BlackFlag.Engine.Filters;

namespace BlackFlag.Engine.Services.Text;

/// <summary>
/// Decodes and re-encodes editable content streams under the narrow
/// preservation policy approved by BF-1054P.
/// </summary>
internal static class PdfContentStreamMutationCodec
{
    private static readonly PdfFilterDecodeLimits DefaultDecodeLimits = new();

    private static readonly PdfName FilterName =
        PdfName.FromLatin1("Filter");

    private static readonly PdfName DecodeParmsName =
        PdfName.FromLatin1("DecodeParms");

    private static readonly PdfName FlateDecodeName =
        PdfName.FromLatin1("FlateDecode");

    private static readonly PdfName FlName =
        PdfName.FromLatin1("Fl");

    private static readonly PdfName PredictorName =
        PdfName.FromLatin1("Predictor");

    public static byte[] Decode(
        PdfStream stream,
        CancellationToken cancellationToken = default) =>
        Decode(stream, DefaultDecodeLimits, cancellationToken);

    internal static byte[] Decode(
        PdfStream stream,
        PdfFilterDecodeLimits limits,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentNullException.ThrowIfNull(limits);

        FilterPolicy policy =
            GetPolicy(
                stream.Dictionary);

        // Keep the mutation API's OperationCanceledException contract. The
        // shared budget owns byte/ratio accounting, not cancellation wrapping.
        cancellationToken.ThrowIfCancellationRequested();
        var budget = new PdfFilterDecodeBudget(stream.Length, limits);

        return policy switch
        {
            FilterPolicy.None =>
                CopyUnfiltered(stream, budget, cancellationToken),

            FilterPolicy.Flate =>
                DecodeFlate(
                    stream.ToByteArray(),
                    budget,
                    cancellationToken),

            _ =>
                throw new InvalidOperationException(
                    "The content-stream filter policy is invalid.")
        };
    }

    public static byte[] Encode(
        PdfStream originalStream,
        ReadOnlySpan<byte> decodedData,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(originalStream);

        FilterPolicy policy =
            GetPolicy(
                originalStream.Dictionary);

        cancellationToken.ThrowIfCancellationRequested();

        return policy switch
        {
            FilterPolicy.None =>
                decodedData.ToArray(),

            FilterPolicy.Flate =>
                EncodeFlate(
                    decodedData,
                    cancellationToken),

            _ =>
                throw new InvalidOperationException(
                    "The content-stream filter policy is invalid.")
        };
    }

    private static FilterPolicy GetPolicy(
        PdfDictionary dictionary)
    {
        if (!dictionary.TryGetValue(
                FilterName,
                out PdfObject? filterValue))
        {
            ValidateDecodeParameters(
                dictionary,
                filtered: false);

            return FilterPolicy.None;
        }

        PdfName filter =
            filterValue switch
            {
                PdfName name =>
                    name,

                PdfArray array when array.Count == 1 &&
                                    array[0] is PdfName name =>
                    name,

                PdfArray =>
                    throw new NotSupportedException(
                        "BF-1054P does not rewrite multi-filter content streams."),

                _ =>
                    throw new InvalidDataException(
                        "Content-stream /Filter must be a name or an array of names.")
            };

        if (filter != FlateDecodeName &&
            filter != FlName)
        {
            throw new NotSupportedException(
                $"BF-1054P preserves only single /FlateDecode content streams; filter '{filter}' is not rewrite-safe yet.");
        }

        ValidateDecodeParameters(
            dictionary,
            filtered: true);

        return FilterPolicy.Flate;
    }

    private static void ValidateDecodeParameters(
        PdfDictionary dictionary,
        bool filtered)
    {
        if (!dictionary.TryGetValue(
                DecodeParmsName,
                out PdfObject? value))
        {
            return;
        }

        if (!filtered)
        {
            if (value is PdfNull)
            {
                return;
            }

            throw new InvalidDataException(
                "An unfiltered content stream cannot carry active /DecodeParms.");
        }

        PdfObject parameters =
            value switch
            {
                PdfNull =>
                    value,

                PdfDictionary =>
                    value,

                PdfArray array when array.Count == 1 =>
                    array[0],

                _ =>
                    throw new NotSupportedException(
                        "BF-1054P requires one Flate decode-parameter entry.")
            };

        if (parameters is PdfNull)
        {
            return;
        }

        if (parameters is not PdfDictionary dictionaryParameters)
        {
            throw new InvalidDataException(
                "Flate /DecodeParms must be a dictionary or null.");
        }

        if (!dictionaryParameters.TryGetValue(
                PredictorName,
                out PdfObject? predictorValue))
        {
            return;
        }

        if (predictorValue is not PdfInteger predictor)
        {
            throw new InvalidDataException(
                "Flate /Predictor must be an integer.");
        }

        if (predictor.Value != 1)
        {
            throw new NotSupportedException(
                $"BF-1054P cannot preservation-safely re-encode Flate predictor {predictor.Value}; only Predictor 1 is supported.");
        }
    }

    private static byte[] DecodeFlate(
        byte[] encoded,
        PdfFilterDecodeBudget budget,
        CancellationToken cancellationToken)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            using var source =
                new CompleteFlateInputStream(encoded);

            using var zlib =
                new ZLibStream(
                    source,
                    CompressionMode.Decompress,
                    leaveOpen: false);

            using var destination =
                new MemoryStream();

            Copy(
                zlib,
                destination,
                budget,
                cancellationToken);

            cancellationToken.ThrowIfCancellationRequested();
            return destination.ToArray();
        }
        catch (InvalidDataException exception)
        {
            throw new InvalidDataException(
                "The editable Flate content stream contains invalid or incomplete zlib data.",
                exception);
        }
        finally
        {
            encoded.AsSpan().Clear();
        }
    }

    private static byte[] CopyUnfiltered(
        PdfStream stream,
        PdfFilterDecodeBudget budget,
        CancellationToken cancellationToken)
    {
        if (stream.Length > 0)
        {
            budget.AccountDecodedBytes(stream.Length);
        }

        cancellationToken.ThrowIfCancellationRequested();
        return stream.ToByteArray();
    }

    private static byte[] EncodeFlate(
        ReadOnlySpan<byte> decoded,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (decoded.IsEmpty)
        {
            // .NET may emit nothing when no data is written. Preserve empty
            // content with a complete zlib header, final block and Adler-32.
            return [0x78, 0x9C, 0x03, 0x00, 0x00, 0x00, 0x00, 0x01];
        }

        using var destination =
            new MemoryStream();

        using (var zlib =
            new ZLibStream(
                destination,
                CompressionLevel.Optimal,
                leaveOpen: true))
        {
            const int ChunkSize = 64 * 1024;

            for (int offset = 0;
                offset < decoded.Length;
                offset += ChunkSize)
            {
                cancellationToken.ThrowIfCancellationRequested();

                int length =
                    Math.Min(
                        ChunkSize,
                        decoded.Length - offset);

                zlib.Write(
                    decoded.Slice(
                        offset,
                        length));
            }
        }

        cancellationToken.ThrowIfCancellationRequested();

        return destination.ToArray();
    }

    private static void Copy(
        Stream source,
        MemoryStream destination,
        PdfFilterDecodeBudget budget,
        CancellationToken cancellationToken)
    {
        byte[] buffer =
            new byte[(int)Math.Min(
                budget.Limits.BufferSize,
                Math.Min(budget.Limits.MaximumDecodedBytes, Array.MaxLength))];

        try
        {
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();

                int read =
                    source.Read(
                        buffer,
                        0,
                        buffer.Length);

                cancellationToken.ThrowIfCancellationRequested();

                if (read == 0)
                {
                    break;
                }

                // Reject the chunk before MemoryStream can allocate or grow.
                budget.AccountDecodedBytes(read);
                EnsureCapacity(destination, budget);

                destination.Write(
                    buffer,
                    0,
                    read);
            }
        }
        finally
        {
            buffer.AsSpan().Clear();
        }
    }

    private static void EnsureCapacity(
        MemoryStream destination,
        PdfFilterDecodeBudget budget)
    {
        long required = budget.DecodedBytes;
        if (required <= destination.Capacity)
        {
            return;
        }

        if (required > Array.MaxLength)
        {
            throw new PdfFilterDecodeException(
                PdfFilterDecodeFailure.DecodedByteLimitExceeded,
                "The editable content stream exceeds the supported decoded buffer size.");
        }

        // Bound spare capacity as well as written bytes. AccountDecodedBytes
        // remains authoritative at exact ratio boundaries (including rounding).
        double capacityLimit = Math.Min(
            Array.MaxLength,
            Math.Min(
                budget.Limits.MaximumDecodedBytes,
                budget.EncodedBytes * budget.Limits.MaximumExpansionRatio));

        destination.Capacity = (int)Math.Max(
            required,
            Math.Min((long)destination.Capacity * 2, capacityLimit));
    }

    private sealed class CompleteFlateInputStream(byte[] encoded)
        : MemoryStream(encoded, writable: false)
    {
        // ZLibStream may otherwise accept EOF before its end marker/checksum.
        // A completed inflater stops reading input; another non-empty read at
        // EOF therefore means the editable zlib member is incomplete.
        public override int Read(byte[] buffer, int offset, int count) =>
            RequireInput(base.Read(buffer, offset, count), count);

        public override int Read(Span<byte> buffer) =>
            RequireInput(base.Read(buffer), buffer.Length);

        private static int RequireInput(int read, int requested)
        {
            if (read == 0 && requested > 0)
            {
                throw new InvalidDataException("The zlib input ended before decompression completed.");
            }

            return read;
        }
    }

    private enum FilterPolicy
    {
        None,
        Flate
    }
}
