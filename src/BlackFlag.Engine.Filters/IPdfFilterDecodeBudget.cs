namespace BlackFlag.Engine.Filters;

/// <summary>
/// Tracks bounded resource use during one PDF filter-decoding operation.
/// </summary>
public interface IPdfFilterDecodeBudget
{
    /// <summary>
    /// Gets the limits governing this operation.
    /// </summary>
    PdfFilterDecodeLimits Limits { get; }

    /// <summary>
    /// Gets the encoded-byte count used to calculate expansion limits.
    /// </summary>
    long EncodedBytes { get; }

    /// <summary>
    /// Gets the decoded-byte count accounted so far.
    /// </summary>
    long DecodedBytes { get; }

    /// <summary>
    /// Throws when cancellation has been requested.
    /// </summary>
    void ThrowIfCancellationRequested();

    /// <summary>
    /// Accounts decoded bytes and enforces all applicable limits.
    /// </summary>
    /// <param name="byteCount">The positive number of decoded bytes.</param>
    void AccountDecodedBytes(int byteCount);
}
