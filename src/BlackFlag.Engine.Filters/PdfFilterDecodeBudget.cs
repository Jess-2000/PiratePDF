namespace BlackFlag.Engine.Filters;

/// <summary>
/// Enforces bounded resource use for one filter-decoding stage.
/// </summary>
public sealed class PdfFilterDecodeBudget : IPdfFilterDecodeBudget
{
    private readonly CancellationToken cancellationToken;
    private long decodedBytes;

    /// <summary>
    /// Initializes a decode budget.
    /// </summary>
    /// <param name="encodedBytes">
    /// The number of bytes supplied to the filter stage.
    /// </param>
    /// <param name="limits">The limits governing the stage.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public PdfFilterDecodeBudget(
        long encodedBytes,
        PdfFilterDecodeLimits limits,
        CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(encodedBytes);
        ArgumentNullException.ThrowIfNull(limits);

        EncodedBytes = encodedBytes;
        Limits = limits;
        this.cancellationToken = cancellationToken;
    }

    /// <inheritdoc />
    public PdfFilterDecodeLimits Limits { get; }

    /// <inheritdoc />
    public long EncodedBytes { get; }

    /// <inheritdoc />
    public long DecodedBytes =>
        Interlocked.Read(
            ref decodedBytes);

    /// <inheritdoc />
    public void ThrowIfCancellationRequested()
    {
        if (!cancellationToken.IsCancellationRequested)
        {
            return;
        }

        throw new PdfFilterDecodeException(
            PdfFilterDecodeFailure.Cancelled,
            "The PDF filter-decoding operation was cancelled.",
            new OperationCanceledException(cancellationToken));
    }

    /// <inheritdoc />
    public void AccountDecodedBytes(
        int byteCount)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(byteCount);

        ThrowIfCancellationRequested();

        long total;

        try
        {
            total =
                checked(
                    DecodedBytes +
                    byteCount);
        }
        catch (OverflowException exception)
        {
            throw new PdfFilterDecodeException(
                PdfFilterDecodeFailure.DecodedByteLimitExceeded,
                "The decoded-byte count exceeded the supported range.",
                exception);
        }

        if (total > Limits.MaximumDecodedBytes)
        {
            throw new PdfFilterDecodeException(
                PdfFilterDecodeFailure.DecodedByteLimitExceeded,
                $"The decoded-byte limit of {Limits.MaximumDecodedBytes} bytes was exceeded.");
        }

        if (ExceedsExpansionRatio(total))
        {
            throw new PdfFilterDecodeException(
                PdfFilterDecodeFailure.ExpansionRatioExceeded,
                $"The maximum expansion ratio of {Limits.MaximumExpansionRatio} was exceeded.");
        }

        Interlocked.Exchange(
            ref decodedBytes,
            total);
    }

    private bool ExceedsExpansionRatio(
        long decodedByteCount)
    {
        if (EncodedBytes == 0)
        {
            return decodedByteCount > 0;
        }

        double ratio =
            (double)decodedByteCount /
            EncodedBytes;

        return ratio >
            Limits.MaximumExpansionRatio;
    }
}

