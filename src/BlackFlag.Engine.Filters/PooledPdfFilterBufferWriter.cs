using System.Buffers;

namespace BlackFlag.Engine.Filters;

internal sealed class PooledPdfFilterBufferWriter :
    IBufferWriter<byte>,
    IDisposable
{
    private readonly IPdfFilterDecodeBudget budget;
    private byte[] buffer;
    private int writtenCount;
    private bool disposed;

    public PooledPdfFilterBufferWriter(
        IPdfFilterDecodeBudget budget)
    {
        ArgumentNullException.ThrowIfNull(budget);

        this.budget = budget;
        buffer =
            ArrayPool<byte>.Shared.Rent(
                budget.Limits.BufferSize);
    }

    public void Advance(
        int count)
    {
        ObjectDisposedException.ThrowIf(
            disposed,
            this);

        ArgumentOutOfRangeException.ThrowIfNegative(count);

        if (count > buffer.Length - writtenCount)
        {
            throw new InvalidOperationException(
                "Cannot advance beyond the memory supplied by the buffer writer.");
        }

        if (count == 0)
        {
            return;
        }

        budget.AccountDecodedBytes(count);
        writtenCount += count;
    }

    public Memory<byte> GetMemory(
        int sizeHint = 0)
    {
        ObjectDisposedException.ThrowIf(
            disposed,
            this);

        EnsureCapacity(sizeHint);
        return buffer.AsMemory(writtenCount);
    }

    public Span<byte> GetSpan(
        int sizeHint = 0)
    {
        ObjectDisposedException.ThrowIf(
            disposed,
            this);

        EnsureCapacity(sizeHint);
        return buffer.AsSpan(writtenCount);
    }

    public byte[] ToArray()
    {
        ObjectDisposedException.ThrowIf(
            disposed,
            this);

        return buffer.AsSpan(
                0,
                writtenCount)
            .ToArray();
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        buffer.AsSpan(
                0,
                writtenCount)
            .Clear();

        ArrayPool<byte>.Shared.Return(buffer);

        buffer = [];
        writtenCount = 0;
        disposed = true;
    }

    private void EnsureCapacity(
        int sizeHint)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(sizeHint);

        budget.ThrowIfCancellationRequested();

        int requiredAdditionalCapacity =
            sizeHint == 0
                ? 1
                : sizeHint;

        if (requiredAdditionalCapacity <= buffer.Length - writtenCount)
        {
            return;
        }

        long requiredCapacity =
            checked(
                (long)writtenCount +
                requiredAdditionalCapacity);

        if (
            requiredCapacity > budget.Limits.MaximumDecodedBytes ||
            requiredCapacity > Array.MaxLength)
        {
            throw new PdfFilterDecodeException(
                PdfFilterDecodeFailure.DecodedByteLimitExceeded,
                "The requested decoded buffer exceeds the permitted size.");
        }

        int doubledCapacity =
            buffer.Length <= Array.MaxLength / 2
                ? buffer.Length * 2
                : Array.MaxLength;

        int newCapacity =
            checked(
                (int)Math.Max(
                    requiredCapacity,
                    doubledCapacity));

        byte[] replacement =
            ArrayPool<byte>.Shared.Rent(
                newCapacity);

        buffer.AsSpan(
                0,
                writtenCount)
            .CopyTo(replacement);

        buffer.AsSpan(
                0,
                writtenCount)
            .Clear();

        ArrayPool<byte>.Shared.Return(buffer);
        buffer = replacement;
    }
}

