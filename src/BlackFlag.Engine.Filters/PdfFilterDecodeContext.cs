using BlackFlag.Engine.Objects;

namespace BlackFlag.Engine.Filters;

/// <summary>
/// Supplies immutable operation state to a PDF filter decoder.
/// </summary>
public sealed class PdfFilterDecodeContext
{
    /// <summary>
    /// Initializes a filter-decoding context.
    /// </summary>
    /// <param name="budget">The resource budget for the operation.</param>
    /// <param name="parameters">
    /// Optional filter-specific decode parameters.
    /// </param>
    /// <param name="filterIndex">The zero-based filter position.</param>
    public PdfFilterDecodeContext(
        IPdfFilterDecodeBudget budget,
        PdfDictionary? parameters = null,
        int filterIndex = 0)
    {
        ArgumentNullException.ThrowIfNull(budget);
        ArgumentOutOfRangeException.ThrowIfNegative(filterIndex);

        Budget = budget;
        Parameters = parameters;
        FilterIndex = filterIndex;
    }

    /// <summary>
    /// Gets the resource budget for this operation.
    /// </summary>
    public IPdfFilterDecodeBudget Budget { get; }

    /// <summary>
    /// Gets optional filter-specific decode parameters.
    /// </summary>
    public PdfDictionary? Parameters { get; }

    /// <summary>
    /// Gets the zero-based position of this filter in the chain.
    /// </summary>
    public int FilterIndex { get; }
}

