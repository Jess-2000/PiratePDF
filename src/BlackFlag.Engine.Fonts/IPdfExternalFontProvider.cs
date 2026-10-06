using System.Text;

namespace BlackFlag.Engine.Fonts;

/// <summary>
/// Supplies external font faces, glyph-coverage checks, and font-program data
/// without exposing platform-specific font APIs to BlackFlag Engine.
/// </summary>
public interface IPdfExternalFontProvider
{
    /// <summary>Gets the stable provider identity.</summary>
    string Id { get; }

    /// <summary>Returns an immutable snapshot of available faces.</summary>
    IReadOnlyList<PdfExternalFontFace> GetFaces(
        CancellationToken cancellationToken = default);

    /// <summary>Determines whether one face contains a glyph for a Unicode scalar.</summary>
    bool SupportsRune(
        PdfExternalFontFace face,
        Rune rune);

    /// <summary>
    /// Opens a readable font-program stream for a face. The caller owns the
    /// returned stream.
    /// </summary>
    Stream OpenFontProgram(
        PdfExternalFontFace face);
}
