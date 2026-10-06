using BlackFlag.Engine.Fonts.Sanitization;

namespace BlackFlag.Engine.Fonts;

/// <summary>Pairs a matched face with the provider that can open its program.</summary>
public sealed class PdfExternalFontMatch
{
    internal PdfExternalFontMatch(
        PdfExternalFontFace face,
        IPdfExternalFontProvider provider)
    {
        Face = face;
        Provider = provider;
    }

    public PdfExternalFontFace Face { get; }

    internal IPdfExternalFontProvider Provider { get; }

    /// <summary>
    /// Opens the provider bytes only through the mandatory sanitizer and
    /// returns an immutable trusted font program.
    /// </summary>
    public PdfSanitizedExternalFontProgram SanitizeFontProgram(
        IPdfExternalFontSanitizer sanitizer,
        PdfExternalFontSanitizationOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(sanitizer);

        using Stream raw =
            Provider.OpenFontProgram(Face) ?? throw new InvalidOperationException(
                $"External font provider '{Provider.Id}' returned a null font-program stream.");

        return sanitizer.Sanitize(
            raw,
            options,
            cancellationToken);
    }
}
