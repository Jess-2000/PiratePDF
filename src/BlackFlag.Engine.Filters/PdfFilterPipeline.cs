using BlackFlag.Engine.Objects;

namespace BlackFlag.Engine.Filters;

/// <summary>
/// Applies registered PDF filters in declaration order under bounded limits.
/// </summary>
public sealed class PdfFilterPipeline : IPdfFilterPipeline
{
    private readonly IPdfFilterRegistry registry;

    /// <summary>
    /// Initializes a filter pipeline.
    /// </summary>
    /// <param name="registry">The decoder registry.</param>
    public PdfFilterPipeline(
        IPdfFilterRegistry registry)
    {
        ArgumentNullException.ThrowIfNull(registry);
        this.registry = registry;
    }

    /// <inheritdoc />
    public ReadOnlyMemory<byte> Decode(
        ReadOnlyMemory<byte> encodedData,
        IReadOnlyList<PdfName> filters,
        IReadOnlyList<PdfDictionary?>? decodeParameters,
        PdfFilterDecodeLimits limits,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(filters);
        ArgumentNullException.ThrowIfNull(limits);

        ValidateFilterCount(
            filters.Count,
            limits);

        ValidateDecodeParameters(
            filters.Count,
            decodeParameters);

        if (filters.Count == 0)
        {
            return CopyUnfilteredData(
                encodedData,
                limits,
                cancellationToken);
        }

        ReadOnlyMemory<byte> current =
            encodedData;

        for (int index = 0; index < filters.Count; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            PdfName filter =
                filters[index]
                ?? throw new ArgumentException(
                    "A filter chain cannot contain a null filter name.",
                    nameof(filters));

            IPdfFilterDecoder decoder =
                registry.GetRequired(filter);

            var budget =
                new PdfFilterDecodeBudget(
                    current.Length,
                    limits,
                    cancellationToken);

            var context =
                new PdfFilterDecodeContext(
                    budget,
                    decodeParameters?[index],
                    index);

            current =
                DecodeStage(
                    decoder,
                    current,
                    context);
        }

        return current;
    }

    private static ReadOnlyMemory<byte> DecodeStage(
        IPdfFilterDecoder decoder,
        ReadOnlyMemory<byte> encodedData,
        PdfFilterDecodeContext context)
    {
        using var destination =
            new PooledPdfFilterBufferWriter(
                context.Budget);

        try
        {
            context.Budget.ThrowIfCancellationRequested();

            decoder.Decode(
                encodedData.Span,
                destination,
                context);

            context.Budget.ThrowIfCancellationRequested();

            return destination.ToArray();
        }
        catch (PdfFilterDecodeException)
        {
            throw;
        }
        catch (OperationCanceledException exception)
        {
            throw new PdfFilterDecodeException(
                PdfFilterDecodeFailure.Cancelled,
                $"PDF filter /{decoder.Name.Value} was cancelled.",
                exception);
        }
        catch (InvalidDataException exception)
        {
            throw new PdfFilterDecodeException(
                PdfFilterDecodeFailure.InvalidData,
                $"PDF filter /{decoder.Name.Value} rejected malformed data.",
                exception);
        }
    }

    private static ReadOnlyMemory<byte> CopyUnfilteredData(
        ReadOnlyMemory<byte> encodedData,
        PdfFilterDecodeLimits limits,
        CancellationToken cancellationToken)
    {
        var budget =
            new PdfFilterDecodeBudget(
                encodedData.Length,
                limits,
                cancellationToken);

        budget.ThrowIfCancellationRequested();

        if (encodedData.Length > 0)
        {
            budget.AccountDecodedBytes(
                encodedData.Length);
        }

        return encodedData.ToArray();
    }

    private static void ValidateFilterCount(
        int filterCount,
        PdfFilterDecodeLimits limits)
    {
        if (filterCount <= limits.MaximumFilterCount)
        {
            return;
        }

        throw new PdfFilterDecodeException(
            PdfFilterDecodeFailure.FilterCountLimitExceeded,
            $"The filter chain contains {filterCount} filters, exceeding the limit of {limits.MaximumFilterCount}.");
    }

    private static void ValidateDecodeParameters(
        int filterCount,
        IReadOnlyList<PdfDictionary?>? decodeParameters)
    {
        if (
            decodeParameters is null ||
            decodeParameters.Count == filterCount)
        {
            return;
        }

        throw new ArgumentException(
            "Decode-parameter entries must align one-to-one with the filter chain.",
            nameof(decodeParameters));
    }
}
