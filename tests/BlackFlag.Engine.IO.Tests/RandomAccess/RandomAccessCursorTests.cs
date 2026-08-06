using BlackFlag.Engine.Abstractions.IO;
using BlackFlag.Engine.IO.RandomAccess;

namespace BlackFlag.Engine.IO.Tests.RandomAccess;

public sealed class RandomAccessCursorTests
{
    [Fact]
    public void ReadByte_AdvancesPosition()
    {
        using var source =
            new TestRandomAccessSource(
            [
                10,
                20,
                30
            ]);

        var cursor =
            new RandomAccessCursor(source);

        Assert.Equal(
            10,
            cursor.ReadByte());

        Assert.Equal(
            1,
            cursor.Position);

        Assert.Equal(
            20,
            cursor.ReadByte());

        Assert.Equal(
            2,
            cursor.Position);
    }

    [Fact]
    public void PeekByte_DoesNotAdvancePosition()
    {
        using var source =
            new TestRandomAccessSource(
            [
                10,
                20
            ]);

        var cursor =
            new RandomAccessCursor(source);

        Assert.Equal(
            10,
            cursor.PeekByte());

        Assert.Equal(
            0,
            cursor.Position);

        Assert.Equal(
            10,
            cursor.ReadByte());
    }

    [Fact]
    public void Seek_AllowsForwardAndBackwardNavigation()
    {
        using var source =
            new TestRandomAccessSource(
            [
                10,
                20,
                30
            ]);

        var cursor =
            new RandomAccessCursor(source);

        cursor.Seek(2);

        Assert.Equal(
            30,
            cursor.ReadByte());

        cursor.Seek(0);

        Assert.Equal(
            10,
            cursor.ReadByte());
    }

    [Fact]
    public void Read_StopsAtEndOfSource()
    {
        using var source =
            new TestRandomAccessSource(
            [
                1,
                2,
                3
            ]);

        var cursor =
            new RandomAccessCursor(
                source,
                initialPosition: 1);

        Span<byte> destination =
            stackalloc byte[4];

        var bytesRead =
            cursor.Read(destination);

        Assert.Equal(
            2,
            bytesRead);

        Assert.Equal(
            [2, 3],
            destination[..bytesRead].ToArray());

        Assert.True(cursor.EndOfSource);
    }

    [Fact]
    public void TryReadExactly_WithInsufficientBytes_RestoresPosition()
    {
        using var source =
            new TestRandomAccessSource(
            [
                1,
                2
            ]);

        var cursor =
            new RandomAccessCursor(source);

        Span<byte> destination =
            stackalloc byte[3];

        Assert.False(
            cursor.TryReadExactly(destination));

        Assert.Equal(
            0,
            cursor.Position);
    }

    [Fact]
    public void Seek_BeyondSource_Throws()
    {
        using var source =
            new TestRandomAccessSource(
            [
                1,
                2
            ]);

        var cursor =
            new RandomAccessCursor(source);

        Assert.Throws<ArgumentOutOfRangeException>(
            () => cursor.Seek(3));
    }
}

internal sealed class TestRandomAccessSource :
    IRandomAccessSource
{
    private readonly byte[] data;

    private bool disposed;

    public TestRandomAccessSource(
        ReadOnlySpan<byte> data)
    {
        this.data = data.ToArray();
    }

    public int ReadCallCount { get; private set; }

    public long Length
    {
        get
        {
            EnsureNotDisposed();

            return data.LongLength;
        }
    }

    public int Read(
        long offset,
        Span<byte> destination)
    {
        EnsureNotDisposed();

        ReadCallCount++;

        ValidateOffset(offset);

        if (
            destination.IsEmpty ||
            offset == data.LongLength)
        {
            return 0;
        }

        var availableBytes =
            data.LongLength - offset;

        var bytesToCopy = checked(
            (int)Math.Min(
                availableBytes,
                destination.Length));

        data
            .AsSpan(
                checked((int)offset),
                bytesToCopy)
            .CopyTo(destination);

        return bytesToCopy;
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
        disposed = true;
    }

    public ValueTask DisposeAsync()
    {
        Dispose();

        return ValueTask.CompletedTask;
    }

    private void ValidateOffset(long offset)
    {
        if (
            offset < 0 ||
            offset > data.LongLength)
        {
            throw new ArgumentOutOfRangeException(
                nameof(offset),
                offset,
                $"The read offset must be between 0 and {data.LongLength}.");
        }
    }

    private void EnsureNotDisposed()
    {
        ObjectDisposedException.ThrowIf(
            disposed,
            this);
    }
}
