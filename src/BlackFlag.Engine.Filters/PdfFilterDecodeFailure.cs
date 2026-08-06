namespace BlackFlag.Engine.Filters;

/// <summary>
/// Identifies the reason a PDF stream-filter operation could not complete.
/// </summary>
public enum PdfFilterDecodeFailure
{
    /// <summary>
    /// The failure does not fit a more specific category.
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// The requested filter is not registered.
    /// </summary>
    UnsupportedFilter = 1,

    /// <summary>
    /// The encoded input is malformed or incomplete.
    /// </summary>
    InvalidData = 2,

    /// <summary>
    /// The decoded-byte limit would be exceeded.
    /// </summary>
    DecodedByteLimitExceeded = 3,

    /// <summary>
    /// The permitted encoded-to-decoded expansion ratio would be exceeded.
    /// </summary>
    ExpansionRatioExceeded = 4,

    /// <summary>
    /// The permitted number of filters in a chain would be exceeded.
    /// </summary>
    FilterCountLimitExceeded = 5,

    /// <summary>
    /// The operation was cancelled.
    /// </summary>
    Cancelled = 6,
}

