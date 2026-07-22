using BlackFlag.Engine.Abstractions.IO;

using Microsoft.Win32.SafeHandles;

namespace BlackFlag.Engine.IO.Sources;

/// <summary>
/// Provides position-independent, read-only access to a file.
/// </summary>
public sealed class FileRandomAccessSource : IRandomAccessSource
{
    private readonly SafeFileHandle fileHandle;
    private bool isDisposed;

    /// <summary>
    /// Initializes a new source for the specified file.
    /// </summary>
    /// <param name="filePath">
    /// The path of the file to open.
    /// </param>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="filePath"/> is null, empty, or whitespace.
    /// </exception>
    /// <exception cref="FileNotFoundException">
    /// Thrown when the specified file does not exist.
    /// </exception>
    public FileRandomAccessSource(string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        FilePath = Path.GetFullPath(filePath);

        if (!File.Exists(FilePath))
        {
            throw new FileNotFoundException(
                "The requested random-access file could not be found.",
                FilePath);
        }

        fileHandle = File.OpenHandle(
            FilePath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            FileOptions.Asynchronous | FileOptions.RandomAccess);

        try
        {
            Length = RandomAccess.GetLength(fileHandle);
        }
        catch
        {
            fileHandle.Dispose();
            throw;
        }
    }

    /// <summary>
    /// Gets the normalized absolute path of the source file.
    /// </summary>
    public string FilePath { get; }

    /// <inheritdoc />
    public long Length { get; }

    /// <inheritdoc />
    public int Read(long offset, Span<byte> destination)
    {
        ThrowIfDisposed();
        ValidateOffset(offset);

        if (destination.IsEmpty || offset >= Length)
        {
            return 0;
        }

        return RandomAccess.Read(
            fileHandle,
            destination,
            offset);
    }

    /// <inheritdoc />
    public ValueTask<int> ReadAsync(
        long offset,
        Memory<byte> destination,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        ValidateOffset(offset);

        cancellationToken.ThrowIfCancellationRequested();

        if (destination.IsEmpty || offset >= Length)
        {
            return ValueTask.FromResult(0);
        }

        return RandomAccess.ReadAsync(
            fileHandle,
            destination,
            offset,
            cancellationToken);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (isDisposed)
        {
            return;
        }

        fileHandle.Dispose();
        isDisposed = true;
    }

    /// <inheritdoc />
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

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(
            isDisposed,
            this);
    }
}