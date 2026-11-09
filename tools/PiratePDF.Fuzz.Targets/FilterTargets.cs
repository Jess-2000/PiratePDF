using BlackFlag.Engine.Filters;
using BlackFlag.Engine.Objects;

namespace PiratePDF.Fuzz.Targets;

internal static class FilterTargets
{
    internal const int MaximumInputBytes = 16 * 1024;

    // Decoders, registry and limits retain only immutable configuration. Decode creates a fresh budget and writer per call.
    private static readonly PdfFilterPipeline FlatePipeline = new(new PdfFilterRegistry([new PdfFlateFilterDecoder()]));
    private static readonly PdfFilterPipeline AsciiHexPipeline = new(new PdfFilterRegistry([new PdfAsciiHexFilterDecoder()]));
    private static readonly PdfName[] FlateFilters = [new("FlateDecode")];
    private static readonly PdfName[] AsciiHexFilters = [new("ASCIIHexDecode")];
    private static readonly PdfFilterDecodeLimits Limits = new(32 * 1024, 32, 4, 1024);

    internal static FuzzResult Flate(ReadOnlySpan<byte> input, CancellationToken cancellationToken) =>
        Decode(input, FlatePipeline, FlateFilters, cancellationToken);

    internal static FuzzResult AsciiHex(ReadOnlySpan<byte> input, CancellationToken cancellationToken) =>
        Decode(input, AsciiHexPipeline, AsciiHexFilters, cancellationToken);

    private static FuzzResult Decode(
        ReadOnlySpan<byte> input, PdfFilterPipeline pipeline, IReadOnlyList<PdfName> filters, CancellationToken cancellationToken)
    {
        // Configuration v1: no predictors, one fixed filter; reuse production budget accounting.
        try
        {
            ReadOnlyMemory<byte> output = pipeline.Decode(
                input.ToArray(), filters, null, Limits, cancellationToken);
            return FuzzResult.Success(output.Span);
        }
        catch (PdfFilterDecodeException exception) when (exception.Failure is
            PdfFilterDecodeFailure.InvalidData or
            PdfFilterDecodeFailure.DecodedByteLimitExceeded or
            PdfFilterDecodeFailure.ExpansionRatioExceeded)
        {
            return FuzzResult.Rejected(exception.Failure.ToString());
        }
    }
}
