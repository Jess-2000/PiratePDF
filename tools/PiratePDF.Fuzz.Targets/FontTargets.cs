using BlackFlag.Engine.Fonts.Sanitization;

namespace PiratePDF.Fuzz.Targets;

internal static class FontTargets
{
    internal const int MaximumInputBytes = 64 * 1024;

    internal static FuzzResult Sanitize(ReadOnlySpan<byte> input, CancellationToken cancellationToken)
    {
        // Configuration v1 uses the existing sanitizer policy with small, finite test limits.
        var options = new PdfExternalFontSanitizationOptions
        {
            MaximumInputBytes = MaximumInputBytes,
            MaximumSingleTableBytes = MaximumInputBytes,
            MaximumTableCount = 32,
            MaximumGlyphCount = 256,
            MaximumCMapSubtableCount = 16,
            MaximumNameRecordCount = 64
        };
        using var source = new MemoryStream(input.ToArray(), writable: false);
        try
        {
            PdfSanitizedExternalFontProgram program =
                new PdfOpenTypeFontSanitizer().Sanitize(source, options, cancellationToken);

            // The sanitizer already computes this digest; no trusted buffer access or extra font copy.
            return FuzzResult.SuccessDigest(program.Sha256);
        }
        catch (PdfExternalFontSanitizationException exception) when (exception.Failure is
            PdfExternalFontSanitizationFailure.InputTooLarge or
            PdfExternalFontSanitizationFailure.UnsupportedContainer or
            PdfExternalFontSanitizationFailure.MalformedHeader or
            PdfExternalFontSanitizationFailure.TooManyTables or
            PdfExternalFontSanitizationFailure.DuplicateTable or
            PdfExternalFontSanitizationFailure.InvalidTableRange or
            PdfExternalFontSanitizationFailure.OverlappingTables or
            PdfExternalFontSanitizationFailure.InvalidChecksum or
            PdfExternalFontSanitizationFailure.MissingRequiredTable or
            PdfExternalFontSanitizationFailure.MalformedRequiredTable or
            PdfExternalFontSanitizationFailure.ResourceLimitExceeded)
        {
            // UnreadableInput would indicate a harness error for our owned MemoryStream.
            return FuzzResult.Rejected(exception.Failure.ToString());
        }
    }
}
