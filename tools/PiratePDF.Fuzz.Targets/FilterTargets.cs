using BlackFlag.Engine.Filters;

namespace PiratePDF.Fuzz.Targets;

internal static class FilterTargets
{
    internal const int MaximumInputBytes = 16 * 1024;

    internal static FuzzResult Flate(ReadOnlySpan<byte> input, CancellationToken cancellationToken) =>
        Decode(input, new PdfFlateFilterDecoder(), cancellationToken);

    internal static FuzzResult AsciiHex(ReadOnlySpan<byte> input, CancellationToken cancellationToken) =>
        Decode(input, new PdfAsciiHexFilterDecoder(), cancellationToken);

    private static FuzzResult Decode(
        ReadOnlySpan<byte> input, IPdfFilterDecoder decoder, CancellationToken cancellationToken)
    {
        var pipeline = new PdfFilterPipeline(new PdfFilterRegistry([decoder]));
        // Configuration v1: no predictors, one fixed filter; reuse production budget accounting.
        var limits = new PdfFilterDecodeLimits(32 * 1024, 32, 4, 1024);
        try
        {
            ReadOnlyMemory<byte> output = pipeline.Decode(
                input.ToArray(), [decoder.Name], null, limits, cancellationToken);
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
