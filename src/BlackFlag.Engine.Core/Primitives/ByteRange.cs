namespace BlackFlag.Engine.Core.Primitives;

/// <summary>
/// Represents a contiguous byte range in a data source.
/// </summary>
public readonly record struct ByteRange
{
    /// <summary>
    /// Initializes a new byte range.
    /// </summary>
    /// <param name="offset">
    /// The zero-based starting byte offset.
    /// </param>
    /// <param name="length">
    /// The number of bytes in the range.
    /// </param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when the offset or length is negative.
    /// </exception>
    public ByteRange(long offset, long length)
    {
        if (offset < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(offset),
                offset,
                "The byte offset cannot be negative.");
        }

        if (length < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(length),
                length,
                "The byte length cannot be negative.");
        }

        Offset = offset;
        Length = length;
    }

    /// <summary>
    /// Gets the zero-based starting byte offset.
    /// </summary>
    public long Offset { get; }

    /// <summary>
    /// Gets the number of bytes in the range.
    /// </summary>
    public long Length { get; }

    /// <summary>
    /// Gets the exclusive ending byte offset.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the calculated end exceeds the supported address space.
    /// </exception>
    public long End
    {
        get
        {
            try
            {
                return checked(Offset + Length);
            }
            catch (OverflowException exception)
            {
                throw new InvalidOperationException(
                    "The byte range exceeds the supported address space.",
                    exception);
            }
        }
    }

    /// <summary>
    /// Gets whether the range contains no bytes.
    /// </summary>
    public bool IsEmpty => Length == 0;

    /// <summary>
    /// Determines whether an absolute offset falls within this range.
    /// </summary>
    public bool Contains(long offset)
    {
        return offset >= Offset && offset < End;
    }

    /// <summary>
    /// Determines whether another byte range is fully contained within this range.
    /// </summary>
    public bool Contains(ByteRange range)
    {
        return range.Offset >= Offset && range.End <= End;
    }

    /// <summary>
    /// Determines whether this range intersects another range.
    /// </summary>
    public bool Intersects(ByteRange range)
    {
        return Offset < range.End && range.Offset < End;
    }
}