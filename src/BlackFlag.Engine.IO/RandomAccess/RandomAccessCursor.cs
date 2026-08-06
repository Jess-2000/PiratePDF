using BlackFlag.Engine.Abstractions.IO;

namespace BlackFlag.Engine.IO.RandomAccess;

/// <summary>
/// Provides an unbuffered cursor over a position-independent source.
/// </summary>
public sealed class RandomAccessCursor :
    IRandomAccessCursor
{
    private readonly IRandomAccessSource source;

    private long position;

    public RandomAccessCursor(
        IRandomAccessSource source,
        long initialPosition = 0)
    {
        ArgumentNullException.ThrowIfNull(source);

        ValidatePosition(
            initialPosition,
            source.Length,
            nameof(initialPosition));

        this.source = source;
        position = initialPosition;
    }

    public long Length =>
        source.Length;

    public long Position =>
        position;

    public bool EndOfSource =>
        position >= source.Length;

    public void Seek(long position)
    {
        ValidatePosition(
            position,
            source.Length,
            nameof(position));

        this.position = position;
    }

    public void Skip(long byteCount)
    {
        if (byteCount < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(byteCount),
                byteCount,
                "The number of bytes to skip cannot be negative.");
        }

        long destination;

        try
        {
            destination = checked(
                position + byteCount);
        }
        catch (OverflowException)
        {
            throw new ArgumentOutOfRangeException(
                nameof(byteCount),
                byteCount,
                "The requested cursor position exceeds the supported range.");
        }

        Seek(destination);
    }

    public byte ReadByte()
    {
        if (!TryReadByte(out var value))
        {
            throw new EndOfStreamException(
                "The cursor is positioned at the end of the source.");
        }

        return value;
    }

    public bool TryReadByte(out byte value)
    {
        if (EndOfSource)
        {
            value = default;
            return false;
        }

        Span<byte> destination =
            stackalloc byte[1];

        var bytesRead = source.Read(
            position,
            destination);

        ValidateReadCount(
            bytesRead,
            destination.Length);

        if (bytesRead == 0)
        {
            value = default;
            return false;
        }

        value = destination[0];
        position++;

        return true;
    }

    public byte PeekByte()
    {
        if (!TryPeekByte(out var value))
        {
            throw new EndOfStreamException(
                "The cursor is positioned at the end of the source.");
        }

        return value;
    }

    public bool TryPeekByte(out byte value)
    {
        if (EndOfSource)
        {
            value = default;
            return false;
        }

        Span<byte> destination =
            stackalloc byte[1];

        var bytesRead = source.Read(
            position,
            destination);

        ValidateReadCount(
            bytesRead,
            destination.Length);

        if (bytesRead == 0)
        {
            value = default;
            return false;
        }

        value = destination[0];

        return true;
    }

    public int Read(Span<byte> destination)
    {
        if (destination.IsEmpty ||
            EndOfSource)
        {
            return 0;
        }

        var bytesRead = source.Read(
            position,
            destination);

        ValidateReadCount(
            bytesRead,
            destination.Length);

        position = checked(
            position + bytesRead);

        return bytesRead;
    }

    private static void ValidatePosition(
        long position,
        long length,
        string parameterName)
    {
        if (position < 0 ||
            position > length)
        {
            throw new ArgumentOutOfRangeException(
                parameterName,
                position,
                $"The cursor position must be between 0 and {length}.");
        }
    }

    private static void ValidateReadCount(
        int bytesRead,
        int destinationLength)
    {
        if (bytesRead < 0 ||
            bytesRead > destinationLength)
        {
            throw new InvalidDataException(
                "The random-access source returned an invalid byte count.");
        }
    }
}