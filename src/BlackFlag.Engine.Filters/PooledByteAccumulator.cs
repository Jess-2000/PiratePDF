using System.Buffers;

namespace BlackFlag.Engine.Filters;

internal sealed class PooledByteAccumulator : IDisposable
{
    private readonly long maximumBytes;
    private readonly long encodedBytes;
    private readonly double maximumExpansionRatio;
    private byte[] buffer;
    private int count;
    private bool disposed;

    public PooledByteAccumulator(
        int initialCapacity,
        long maximumBytes,
        long encodedBytes,
        double maximumExpansionRatio)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(initialCapacity);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maximumBytes);
        ArgumentOutOfRangeException.ThrowIfNegative(encodedBytes);

        if (
            double.IsNaN(maximumExpansionRatio) ||
            double.IsInfinity(maximumExpansionRatio) ||
            maximumExpansionRatio <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(maximumExpansionRatio));
        }

        this.maximumBytes = maximumBytes;
        this.encodedBytes = encodedBytes;
        this.maximumExpansionRatio = maximumExpansionRatio;

        int capacity =
            (int)Math.Min(
                initialCapacity,
                Math.Min(
                    maximumBytes,
                    Array.MaxLength));

        buffer =
            ArrayPool<byte>.Shared.Rent(
                Math.Max(capacity, 1));
    }

    public int Count => count;

    public void Append(
        ReadOnlySpan<byte> data)
    {
        ObjectDisposedException.ThrowIf(
            disposed,
            this);

        if (data.IsEmpty)
        {
            return;
        }

        long required =
            checked(
                (long)count +
                data.Length);

        EnforceLimits(required);
        EnsureCapacity((int)required);

        data.CopyTo(
            buffer.AsSpan(count));

        count =
            (int)required;
    }

    public ReadOnlySpan<byte> AsSpan()
    {
        ObjectDisposedException.ThrowIf(
            disposed,
            this);

        return buffer.AsSpan(
            0,
            count);
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        buffer.AsSpan(
                0,
                count)
            .Clear();

        ArrayPool<byte>.Shared.Return(buffer);

        buffer = [];
        count = 0;
        disposed = true;
    }

    private void EnforceLimits(
        long required)
    {
        if (required > maximumBytes)
        {
            throw new PdfFilterDecodeException(
                PdfFilterDecodeFailure.DecodedByteLimitExceeded,
                $"Flate decompression exceeded the decoded-byte limit of {maximumBytes} bytes.");
        }

        if (encodedBytes == 0)
        {
            if (required > 0)
            {
                throw new PdfFilterDecodeException(
                    PdfFilterDecodeFailure.ExpansionRatioExceeded,
                    "A zero-length Flate input produced decoded data.");
            }

            return;
        }

        double ratio =
            (double)required /
            encodedBytes;

        if (ratio > maximumExpansionRatio)
        {
            throw new PdfFilterDecodeException(
                PdfFilterDecodeFailure.ExpansionRatioExceeded,
                $"Flate decompression exceeded the maximum expansion ratio of {maximumExpansionRatio}.");
        }
    }

    private void EnsureCapacity(
        int required)
    {
        if (required <= buffer.Length)
        {
            return;
        }

        int doubled =
            buffer.Length <= Array.MaxLength / 2
                ? buffer.Length * 2
                : Array.MaxLength;

        int newCapacity =
            Math.Max(
                required,
                doubled);

        byte[] replacement =
            ArrayPool<byte>.Shared.Rent(
                newCapacity);

        buffer.AsSpan(
                0,
                count)
            .CopyTo(replacement);

        buffer.AsSpan(
                0,
                count)
            .Clear();

        ArrayPool<byte>.Shared.Return(buffer);
        buffer = replacement;
    }
}

