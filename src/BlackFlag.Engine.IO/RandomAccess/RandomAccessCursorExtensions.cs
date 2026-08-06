namespace BlackFlag.Engine.IO.RandomAccess;

/// <summary>
/// Provides allocation-free cursor helpers.
/// </summary>
public static class RandomAccessCursorExtensions
{
    public static void ReadExactly(
        this IRandomAccessCursor cursor,
        Span<byte> destination)
    {
        ArgumentNullException.ThrowIfNull(cursor);

        var totalBytesRead = 0;

        while (totalBytesRead < destination.Length)
        {
            var bytesRead = cursor.Read(
                destination[totalBytesRead..]);

            if (bytesRead == 0)
            {
                throw new EndOfStreamException(
                    $"Expected {destination.Length} bytes, but only " +
                    $"{totalBytesRead} bytes were available.");
            }

            totalBytesRead += bytesRead;
        }
    }

    public static bool TryReadExactly(
        this IRandomAccessCursor cursor,
        Span<byte> destination)
    {
        ArgumentNullException.ThrowIfNull(cursor);

        var initialPosition =
            cursor.Position;

        var totalBytesRead = 0;

        while (totalBytesRead < destination.Length)
        {
            var bytesRead = cursor.Read(
                destination[totalBytesRead..]);

            if (bytesRead == 0)
            {
                cursor.Seek(initialPosition);
                return false;
            }

            totalBytesRead += bytesRead;
        }

        return true;
    }
}