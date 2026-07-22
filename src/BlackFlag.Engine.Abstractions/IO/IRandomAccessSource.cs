namespace BlackFlag.Engine.Abstractions.IO;

/// <summary>
/// Provides position-independent access to a binary data source.
/// </summary>
public interface IRandomAccessSource : IDisposable, IAsyncDisposable
{
    /// <summary>
    /// Gets the total source length in bytes.
    /// </summary>
    long Length { get; }

    /// <summary>
    /// Reads bytes beginning at an absolute source offset.
    /// </summary>
    /// <param name="offset">
    /// The zero-based byte offset at which reading begins.
    /// </param>
    /// <param name="destination">
    /// The buffer that receives the bytes.
    /// </param>
    /// <returns>
    /// The number of bytes read. This may be smaller than the destination
    /// length when the end of the source is reached.
    /// </returns>
    int Read(long offset, Span<byte> destination);

    /// <summary>
    /// Asynchronously reads bytes beginning at an absolute source offset.
    /// </summary>
    /// <param name="offset">
    /// The zero-based byte offset at which reading begins.
    /// </param>
    /// <param name="destination">
    /// The buffer that receives the bytes.
    /// </param>
    /// <param name="cancellationToken">
    /// A token used to cancel the operation.
    /// </param>
    /// <returns>
    /// The number of bytes read.
    /// </returns>
    ValueTask<int> ReadAsync(
        long offset,
        Memory<byte> destination,
        CancellationToken cancellationToken = default);
}