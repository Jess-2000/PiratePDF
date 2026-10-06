
using BlackFlag.Engine.Rendering.Resources;

namespace BlackFlag.Engine.Rendering.Nested;

/// <summary>
/// Supplies interpreted nested content for tiling patterns and Type 3 glyphs
/// without introducing a Rendering-to-Parsing dependency.
/// </summary>
public interface IPdfNestedGraphicsProvider
{
    /// <summary>Gets one interpreted tiling-pattern cell.</summary>
    PdfTilingPatternCell GetPatternCell(
        PdfResolvedPattern pattern,
        CancellationToken cancellationToken = default);

    /// <summary>Gets one interpreted Type 3 glyph program.</summary>
    PdfType3GlyphProgram GetType3Glyph(
        PdfResolvedFont font,
        ReadOnlyMemory<byte> sourceCode,
        CancellationToken cancellationToken = default);
}
