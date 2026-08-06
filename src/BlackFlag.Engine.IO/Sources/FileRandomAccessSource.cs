using Microsoft.Win32.SafeHandles;
using BlackFlag.Engine.Abstractions.IO;
using FileRandomAccess = System.IO.RandomAccess;

namespace BlackFlag.Engine.IO.Sources;

/// <summary>
/// Provides position-independent random access to a file.
/// </summary>
public sealed class FileRandomAccessSource :
    IRandomAccessSource
{
    private readonly SafeFileHandle handle;
    private readonly string filePath;

    private bool disposed;

    public FileRandomAccessSource(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        filePath = Path.GetFullPath(path);

        handle = File.OpenHandle(
            filePath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            FileOptions.RandomAccess);
    }

    /// <summary>
    /// Gets the normalized path of the opened file.
    /// </summary>
    public string FilePath
    {
        get
        {
            EnsureNotDisposed();
            return filePath;
        }
    }

    /// <summary>
    /// Gets the current length of the file.
    /// </summary>
    public long Length
    {
        get
        {
            EnsureNotDisposed();

            return FileRandomAccess.GetLength(handle);
        }
    }

    public int Read(
        long offset,
        Span<byte> destination)
    {
        EnsureNotDisposed();
        ValidateOffset(offset);

        if (destination.IsEmpty)
        {
            return 0;
        }

        var length = Length;

        // Reading at or beyond EOF returns zero bytes.
        if (offset >= length)
        {
            return 0;
        }

        return FileRandomAccess.Read(
            handle,
            destination,
            offset);
    }

    public ValueTask<int> ReadAsync(
        long offset,
        Memory<byte> destination,
        CancellationToken cancellationToken = default)
    {
        EnsureNotDisposed();
        ValidateOffset(offset);

        cancellationToken.ThrowIfCancellationRequested();

        if (destination.IsEmpty)
        {
            return ValueTask.FromResult(0);
        }

        var length = Length;

        // Reading at or beyond EOF returns zero bytes.
        if (offset >= length)
        {
            return ValueTask.FromResult(0);
        }

        return FileRandomAccess.ReadAsync(
            handle,
            destination,
            offset,
            cancellationToken);
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        handle.Dispose();
        disposed = true;
    }

    public ValueTask DisposeAsync()
    {
        Dispose();
        return ValueTask.CompletedTask;
    }

    private static void ValidateOffset(long offset)
    {
        if (offset < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(offset),
                offset,
                "The read offset cannot be negative.");
        }
    }

    private void EnsureNotDisposed()
    {
        ObjectDisposedException.ThrowIf(
            disposed,
            this);
    }
}
