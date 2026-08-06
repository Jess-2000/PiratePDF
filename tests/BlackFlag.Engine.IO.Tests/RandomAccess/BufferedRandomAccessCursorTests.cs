using BlackFlag.Engine.IO.RandomAccess;

namespace BlackFlag.Engine.IO.Tests.RandomAccess;

public sealed class BufferedRandomAccessCursorTests
{
    [Fact]
    public void ReadByte_ReadsAcrossBufferBoundaries()
    {
        using var source =
            new TestRandomAccessSource(
            [
                1,
                2,
                3,
                4,
                5,
                6
            ]);

        using var cursor =
            new BufferedRandomAccessCursor(
                source,
                bufferSize: 2);

        Assert.Equal(1, cursor.ReadByte());
        Assert.Equal(2, cursor.ReadByte());
        Assert.Equal(3, cursor.ReadByte());
        Assert.Equal(4, cursor.ReadByte());
        Assert.Equal(5, cursor.ReadByte());
        Assert.Equal(6, cursor.ReadByte());

        Assert.True(cursor.EndOfSource);
    }

    [Fact]
    public void Seek_WithinExistingBuffer_ReusesBufferedData()
    {
        using var source =
            new TestRandomAccessSource(
            [
                10,
                20,
                30,
                40
            ]);

        using var cursor =
            new BufferedRandomAccessCursor(
                source,
                bufferSize: 4);

        Assert.Equal(
            10,
            cursor.ReadByte());

        cursor.Seek(0);

        Assert.Equal(
            10,
            cursor.ReadByte());

        Assert.Equal(
            1,
            source.ReadCallCount);
    }

    [Fact]
    public void Read_LargerThanBuffer_ReadsCompleteDestination()
    {
        using var source =
            new TestRandomAccessSource(
                Enumerable
                    .Range(0, 64)
                    .Select(value => (byte)value)
                    .ToArray());

        using var cursor =
            new BufferedRandomAccessCursor(
                source,
                bufferSize: 8);

        Span<byte> destination =
            stackalloc byte[32];

        var bytesRead =
            cursor.Read(destination);

        Assert.Equal(
            destination.Length,
            bytesRead);

        Assert.Equal(
            Enumerable
                .Range(0, 32)
                .Select(value => (byte)value)
                .ToArray(),
            destination.ToArray());
    }

    [Fact]
    public void Dispose_PreventsFurtherUse()
    {
        using var source = new TestRandomAccessSource([1]);

        var cursor =
            new BufferedRandomAccessCursor(
                source,
                bufferSize: 2);

        cursor.Dispose();

        Assert.Throws<ObjectDisposedException>(
            () => cursor.ReadByte());
    }
}