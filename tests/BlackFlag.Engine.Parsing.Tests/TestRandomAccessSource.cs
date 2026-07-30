using System.Threading;
using System.Threading.Tasks;
using BlackFlag.Engine.Abstractions.IO;

namespace BlackFlag.Engine.Parsing.Tests;

internal sealed class TestRandomAccessSource :
    IRandomAccessSource
{
    private readonly byte[] bytes;

    public TestRandomAccessSource(
        byte[] bytes)
    {
        ArgumentNullException.ThrowIfNull(bytes);

        this.bytes =
            bytes.ToArray();
    }

    public long Length =>
        bytes.LongLength;

    public int Read(
        long offset,
        Span<byte> destination)
    {
        ValidateOffset(offset);

        int available =
            checked(
                (int)Math.Min(
                    destination.Length,
                    Length - offset));

        if (available == 0)
        {
            return 0;
        }

        bytes
            .AsSpan(
                checked(
                    (int)offset),
                available)
            .CopyTo(destination);

        return available;
    }

    public ValueTask<int> ReadAsync(
        long offset,
        Memory<byte> destination,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return ValueTask.FromResult(
            Read(
                offset,
                destination.Span));
    }

    public void Dispose()
    {
    }

    public ValueTask DisposeAsync()
    {
        return ValueTask.CompletedTask;
    }

    private void ValidateOffset(
        long offset)
    {
        if (
            offset < 0 ||
            offset > Length)
        {
            throw new ArgumentOutOfRangeException(
                nameof(offset));
        }
    }
}
