namespace BlackFlag.Engine.Fonts.Sanitization;

/// <summary>
/// Converts untrusted external font bytes into a validated immutable program.
/// Unsanitized bytes must not be supplied to later font processing.
/// </summary>
public interface IPdfExternalFontSanitizer
{
    PdfSanitizedExternalFontProgram Sanitize(
        Stream untrustedFontProgram,
        PdfExternalFontSanitizationOptions? options = null,
        CancellationToken cancellationToken = default);
}
