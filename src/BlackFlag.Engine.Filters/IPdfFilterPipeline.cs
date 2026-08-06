using BlackFlag.Engine.Objects;

namespace BlackFlag.Engine.Filters;

/// <summary>
/// Applies an ordered sequence of PDF stream filters under bounded resource
/// limits.
/// </summary>
public interface IPdfFilterPipeline
{
    /// <summary>
    /// Applies the requested filter chain in declaration order.
    /// </summary>
    /// <param name="encodedData">The initial encoded bytes.</param>
    /// <param name="filters">The canonical filter names.</param>
    /// <param name="decodeParameters">
    /// Optional decode-parameter dictionaries aligned with the filters.
    /// </param>
    /// <param name="limits">The operation resource limits.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>An owned immutable snapshot of decoded bytes.</returns>
    ReadOnlyMemory<byte> Decode(
        ReadOnlyMemory<byte> encodedData,
        IReadOnlyList<PdfName> filters,
        IReadOnlyList<PdfDictionary?>? decodeParameters,
        PdfFilterDecodeLimits limits,
        CancellationToken cancellationToken = default);
}
