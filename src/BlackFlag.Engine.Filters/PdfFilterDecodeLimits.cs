namespace BlackFlag.Engine.Filters;

/// <summary>
/// Defines bounded resource limits for a PDF stream-filter operation.
/// </summary>
public sealed class PdfFilterDecodeLimits
{
    /// <summary>
    /// Gets the default maximum decoded size: 256 MiB.
    /// </summary>
    public const long DefaultMaximumDecodedBytes = 256L * 1024L * 1024L;

    /// <summary>
    /// Gets the default maximum decoded-to-encoded expansion ratio.
    /// </summary>
    public const double DefaultMaximumExpansionRatio = 200.0;

    /// <summary>
    /// Gets the default maximum number of filters in one chain.
    /// </summary>
    public const int DefaultMaximumFilterCount = 8;

    /// <summary>
    /// Gets the default temporary buffer size: 64 KiB.
    /// </summary>
    public const int DefaultBufferSize = 64 * 1024;

    /// <summary>
    /// Initializes default decode limits.
    /// </summary>
    public PdfFilterDecodeLimits()
        : this(
            DefaultMaximumDecodedBytes,
            DefaultMaximumExpansionRatio,
            DefaultMaximumFilterCount,
            DefaultBufferSize)
    {
    }

    /// <summary>
    /// Initializes explicit decode limits.
    /// </summary>
    /// <param name="maximumDecodedBytes">Maximum permitted decoded bytes.</param>
    /// <param name="maximumExpansionRatio">
    /// Maximum permitted decoded-to-encoded expansion ratio.
    /// </param>
    /// <param name="maximumFilterCount">Maximum filters in one chain.</param>
    /// <param name="bufferSize">Preferred temporary buffer size.</param>
    public PdfFilterDecodeLimits(
        long maximumDecodedBytes,
        double maximumExpansionRatio,
        int maximumFilterCount,
        int bufferSize)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maximumDecodedBytes);

        if (
            double.IsNaN(maximumExpansionRatio) ||
            double.IsInfinity(maximumExpansionRatio) ||
            maximumExpansionRatio <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(maximumExpansionRatio));
        }

        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maximumFilterCount);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(bufferSize);

        MaximumDecodedBytes = maximumDecodedBytes;
        MaximumExpansionRatio = maximumExpansionRatio;
        MaximumFilterCount = maximumFilterCount;
        BufferSize = bufferSize;
    }

    /// <summary>
    /// Gets the maximum permitted decoded-byte count.
    /// </summary>
    public long MaximumDecodedBytes { get; }

    /// <summary>
    /// Gets the maximum permitted decoded-to-encoded expansion ratio.
    /// </summary>
    public double MaximumExpansionRatio { get; }

    /// <summary>
    /// Gets the maximum number of filters permitted in one chain.
    /// </summary>
    public int MaximumFilterCount { get; }

    /// <summary>
    /// Gets the preferred temporary buffer size.
    /// </summary>
    public int BufferSize { get; }
}
