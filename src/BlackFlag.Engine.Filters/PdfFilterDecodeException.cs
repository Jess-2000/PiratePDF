namespace BlackFlag.Engine.Filters;

/// <summary>
/// Represents a deterministic failure while decoding PDF stream-filter data.
/// </summary>
public sealed class PdfFilterDecodeException : IOException
{
    /// <summary>
    /// Initializes a new filter-decoding exception.
    /// </summary>
    /// <param name="failure">The classified failure reason.</param>
    /// <param name="message">The error message.</param>
    public PdfFilterDecodeException(
        PdfFilterDecodeFailure failure,
        string message)
        : base(message)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(message);
        Failure = failure;
    }

    /// <summary>
    /// Initializes a new filter-decoding exception with an inner exception.
    /// </summary>
    /// <param name="failure">The classified failure reason.</param>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The exception that caused this failure.</param>
    public PdfFilterDecodeException(
        PdfFilterDecodeFailure failure,
        string message,
        Exception innerException)
        : base(message, innerException)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(message);
        ArgumentNullException.ThrowIfNull(innerException);
        Failure = failure;
    }

    /// <summary>
    /// Gets the classified failure reason.
    /// </summary>
    public PdfFilterDecodeFailure Failure { get; }
}

