using System.Buffers;

using BlackFlag.Engine.Abstractions.IO;

namespace BlackFlag.Engine.IO.RandomAccess;

/// <summary>
/// Provides a bounded, pooled read-ahead cursor over a
/// position-independent source.
/// </summary>
public sealed class BufferedRandomAccessCursor :
    IRandomAccessCursor,
    IDisposable
{
    private readonly IRandomAccessSource source;
    private readonly byte[] buffer;

    private long position;
    private long bufferStart;
    private int bufferLength;
    private bool disposed;

    public BufferedRandomAccessCursor(
        IRandomAccessSource source,
        int bufferSize = RandomAccessDefaults.BufferSize,
        long initialPosition = 0)
    {
        ArgumentNullException.ThrowIfNull(source);

        if (bufferSize <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(bufferSize),
                bufferSize,
                "The buffer size must be greater than zero.");
        }

        ValidatePosition(
            initialPosition,
            source.Length,
            nameof(initialPosition));

        this.source = source;
        buffer = ArrayPool<byte>.Shared.Rent(
            bufferSize);

        position = initialPosition;
        bufferStart = 0;
        bufferLength = 0;
    }

    public long Length
    {
        get
        {
            EnsureNotDisposed();

            return source.Length;
        }
    }

    public long Position
    {
        get
        {
            EnsureNotDisposed();

            return position;
        }
    }

    public bool EndOfSource
    {
        get
        {
            EnsureNotDisposed();

            return position >= source.Length;
        }
    }

    public void Seek(long position)
    {
        EnsureNotDisposed();

        ValidatePosition(
            position,
            source.Length,
            nameof(position));

        this.position = position;
    }

    public void Skip(long byteCount)
    {
        EnsureNotDisposed();

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
        EnsureNotDisposed();

        if (!TryReadByte(out var value))
        {
            throw new EndOfStreamException(
                "The cursor is positioned at the end of the source.");
        }

        return value;
    }

    public bool TryReadByte(out byte value)
    {
        EnsureNotDisposed();

        if (!EnsurePositionBuffered())
        {
            value = default;
            return false;
        }

        var bufferOffset = checked(
            (int)(position - bufferStart));

        value = buffer[bufferOffset];
        position++;

        return true;
    }

    public byte PeekByte()
    {
        EnsureNotDisposed();

        if (!TryPeekByte(out var value))
        {
            throw new EndOfStreamException(
                "The cursor is positioned at the end of the source.");
        }

        return value;
    }

    public bool TryPeekByte(out byte value)
    {
        EnsureNotDisposed();

        if (!EnsurePositionBuffered())
        {
            value = default;
            return false;
        }

        var bufferOffset = checked(
            (int)(position - bufferStart));

        value = buffer[bufferOffset];

        return true;
    }

    public int Read(Span<byte> destination)
    {
        EnsureNotDisposed();

        if (destination.IsEmpty ||
            position >= source.Length)
        {
            return 0;
        }

        var totalBytesRead = 0;

        while (!destination.IsEmpty &&
               position < source.Length)
        {
            var bufferedBytesCopied =
                CopyBufferedBytes(destination);

            if (bufferedBytesCopied > 0)
            {
                totalBytesRead +=
                    bufferedBytesCopied;

                destination =
                    destination[bufferedBytesCopied..];

                continue;
            }

            if (destination.Length >= buffer.Length)
            {
                var directBytesRead = source.Read(
                    position,
                    destination);

                ValidateReadCount(
                    directBytesRead,
                    destination.Length);

                if (directBytesRead == 0)
                {
                    break;
                }

                position = checked(
                    position + directBytesRead);

                totalBytesRead +=
                    directBytesRead;

                destination =
                    destination[directBytesRead..];

                InvalidateBuffer();

                continue;
            }

            FillBuffer();

            if (bufferLength == 0)
            {
                break;
            }
        }

        return totalBytesRead;
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        ArrayPool<byte>.Shared.Return(
            buffer,
            clearArray: false);

        disposed = true;
    }

    private bool EnsurePositionBuffered()
    {
        if (position >= source.Length)
        {
            return false;
        }

        if (!IsPositionBuffered(position))
        {
            FillBuffer();
        }

        return IsPositionBuffered(position);
    }

    private void FillBuffer()
    {
        bufferStart = position;

        bufferLength = source.Read(
            bufferStart,
            buffer);

        ValidateReadCount(
            bufferLength,
            buffer.Length);
    }

    private int CopyBufferedBytes(
        Span<byte> destination)
    {
        if (!IsPositionBuffered(position))
        {
            return 0;
        }

        var bufferOffset = checked(
            (int)(position - bufferStart));

        var availableBytes =
            bufferLength - bufferOffset;

        var bytesToCopy = Math.Min(
            availableBytes,
            destination.Length);

        buffer
            .AsSpan(
                bufferOffset,
                bytesToCopy)
            .CopyTo(destination);

        position = checked(
            position + bytesToCopy);

        return bytesToCopy;
    }

    private bool IsPositionBuffered(
        long candidatePosition)
    {
        return
            candidatePosition >= bufferStart &&
            candidatePosition <
                bufferStart + bufferLength;
    }

    private void InvalidateBuffer()
    {
        bufferStart = 0;
        bufferLength = 0;
    }

    private void EnsureNotDisposed()
    {
        ObjectDisposedException.ThrowIf(
            disposed,
            this);
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