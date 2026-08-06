namespace BlackFlag.Engine.IO.RandomAccess;

/// <summary>
/// Provides stateful sequential and random navigation over a
/// position-independent binary source.
/// </summary>
public interface IRandomAccessCursor
{
    /// <summary>
    /// Gets the total source length in bytes.
    /// </summary>
    long Length { get; }

    /// <summary>
    /// Gets the current absolute source position.
    /// </summary>
    long Position { get; }

    /// <summary>
    /// Gets whether the cursor is positioned at the end of the source.
    /// </summary>
    bool EndOfSource { get; }

    /// <summary>
    /// Moves the cursor to an absolute source position.
    /// </summary>
    void Seek(long position);

    /// <summary>
    /// Advances the cursor by the specified non-negative byte count.
    /// </summary>
    void Skip(long byteCount);

    /// <summary>
    /// Reads one byte and advances the cursor.
    /// </summary>
    byte ReadByte();

    /// <summary>
    /// Attempts to read one byte and advance the cursor.
    /// </summary>
    bool TryReadByte(out byte value);

    /// <summary>
    /// Reads one byte without advancing the cursor.
    /// </summary>
    byte PeekByte();

    /// <summary>
    /// Attempts to read one byte without advancing the cursor.
    /// </summary>
    bool TryPeekByte(out byte value);

    /// <summary>
    /// Reads bytes into the destination and advances the cursor.
    /// </summary>
    int Read(Span<byte> destination);
}
