using BlackFlag.Engine.Rendering.DisplayList;
using BlackFlag.Engine.Rendering.Resources;

namespace BlackFlag.Engine.Rendering.Fonts;

/// <summary>Contains the renderer-neutral decoded representation of a display text run.</summary>
public sealed class PdfDecodedTextRun
{
    private readonly PdfDecodedGlyph[] glyphs;
    private readonly PdfFontDecodeDiagnostic[] diagnostics;

    internal PdfDecodedTextRun(
        PdfResolvedFont font,
        PdfDisplayTextRun source,
        IEnumerable<PdfDecodedGlyph> glyphs,
        IEnumerable<PdfFontDecodeDiagnostic> diagnostics)
    {
        Font = font;
        Source = source;
        this.glyphs = glyphs.ToArray();
        this.diagnostics = diagnostics.ToArray();
    }

    /// <summary>Gets the resolved PDF font.</summary>
    public PdfResolvedFont Font { get; }

    /// <summary>Gets the source display-list text run.</summary>
    public PdfDisplayTextRun Source { get; }

    /// <summary>Gets decoded glyphs in display order.</summary>
    public IReadOnlyList<PdfDecodedGlyph> Glyphs => glyphs;

    /// <summary>Gets decoding diagnostics.</summary>
    public IReadOnlyList<PdfFontDecodeDiagnostic> Diagnostics => diagnostics;
}
