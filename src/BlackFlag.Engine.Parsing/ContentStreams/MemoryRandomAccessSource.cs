using BlackFlag.Engine.Abstractions.IO;

namespace BlackFlag.Engine.Parsing.ContentStreams;

internal sealed class MemoryRandomAccessSource : IRandomAccessSource
{
    private readonly byte[] bytes;

    public MemoryRandomAccessSource(ReadOnlySpan<byte> bytes) => this.bytes = bytes.ToArray();

    public long Length => bytes.LongLength;

    public int Read(long offset, Span<byte> destination)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(offset);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(offset, Length);
        int count = checked((int)Math.Min(destination.Length, Length - offset));
        bytes.AsSpan(checked((int)offset), count).CopyTo(destination);
        return count;
    }

    public ValueTask<int> ReadAsync(long offset, Memory<byte> destination, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return ValueTask.FromResult(Read(offset, destination.Span));
    }

    public void Dispose() { }
    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
