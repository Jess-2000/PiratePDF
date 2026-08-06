namespace BlackFlag.Engine.IO.RandomAccess;

/// <summary>
/// Defines default settings for random-access I/O.
/// </summary>
public static class RandomAccessDefaults
{
    /// <summary>
    /// Gets the default bounded read-ahead buffer size.
    /// </summary>
    public const int BufferSize = 64 * 1024;
}
