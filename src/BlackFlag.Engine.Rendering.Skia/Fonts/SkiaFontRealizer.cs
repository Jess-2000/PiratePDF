using BlackFlag.Engine.Rendering.Fonts;
using BlackFlag.Engine.Rendering.Resources;
using BlackFlag.Engine.Rendering.Raster;
using SkiaSharp;

namespace BlackFlag.Engine.Rendering.Skia.Fonts;

/// <summary>Creates Skia font objects from canonical PDF font resources.</summary>
internal sealed class SkiaFontRealizer : IDisposable
{
    private readonly PdfFontDecoder decoder;
    private readonly Dictionary<PdfResourceIdentity, SKTypeface> typefaces = [];

    public SkiaFontRealizer(PdfFontDecoder decoder)
    {
        this.decoder = decoder;
    }

    public SKTypeface GetTypeface(
        PdfResolvedFont font,
        List<PdfRenderDiagnostic> diagnostics,
        int displayItemIndex,
        CancellationToken cancellationToken)
    {
        if (typefaces.TryGetValue(font.Identity, out SKTypeface? cached))
        {
            return cached;
        }

        SKTypeface? typeface = null;
        if (font.EmbeddedFontProgram is not null)
        {
            try
            {
                ReadOnlyMemory<byte>? decoded = decoder.DecodeEmbeddedProgram(font, cancellationToken);
                if (decoded is { } program && !program.IsEmpty)
                {
                    var stream = new MemoryStream(program.ToArray(), writable: false);
                    typeface = SKTypeface.FromStream(stream);
                }
            }
            catch (Exception exception) when (
                exception is InvalidDataException or
                BlackFlag.Engine.Filters.PdfFilterDecodeException)
            {
                diagnostics.Add(new PdfRenderDiagnostic(
                    "FONT_PROGRAM_INVALID",
                    $"Embedded font '{font.FaceName}' could not be decoded: {exception.Message}",
                    displayItemIndex));
            }
        }

        if (typeface is null)
        {
            typeface = SKTypeface.FromFamilyName(font.FamilyName) ??
                SKTypeface.FromFamilyName(font.FaceName) ??
                SKTypeface.Default;

            diagnostics.Add(new PdfRenderDiagnostic(
                "FONT_SUBSTITUTED",
                $"Font '{font.FaceName}' was rendered using a system fallback because no usable embedded program was available.",
                displayItemIndex));
        }

        typefaces.Add(font.Identity, typeface);
        return typeface;
    }

    public void Dispose()
    {
        foreach (SKTypeface typeface in typefaces.Values)
        {
            if (!ReferenceEquals(typeface, SKTypeface.Default))
            {
                typeface.Dispose();
            }
        }

        typefaces.Clear();
    }
}
