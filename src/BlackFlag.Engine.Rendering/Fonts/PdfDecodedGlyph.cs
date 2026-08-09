namespace BlackFlag.Engine.Rendering.Fonts;

/// <summary>Preserves the source-code, CID, glyph, Unicode, and position relationship for one PDF glyph.</summary>
public sealed class PdfDecodedGlyph
{
    private readonly byte[] sourceCode;

    /// <summary>Initializes a decoded glyph.</summary>
    public PdfDecodedGlyph(
        ReadOnlySpan<byte> sourceCode,
        int? cid,
        ushort? glyphId,
        string? unicodeText,
        double x,
        double advance)
    {
        if (sourceCode.IsEmpty)
        {
            throw new ArgumentException("A decoded glyph requires a source character code.", nameof(sourceCode));
        }

        this.sourceCode = sourceCode.ToArray();
        Cid = cid;
        GlyphId = glyphId;
        UnicodeText = unicodeText;
        X = x;
        Advance = advance;
    }

    /// <summary>Gets the original encoded PDF character code.</summary>
    public ReadOnlyMemory<byte> SourceCode => sourceCode;

    /// <summary>Gets the CID when the font is composite.</summary>
    public int? Cid { get; }

    /// <summary>Gets a known font-program glyph identifier.</summary>
    public ushort? GlyphId { get; }

    /// <summary>Gets Unicode text when an authoritative mapping is available.</summary>
    public string? UnicodeText { get; }

    /// <summary>Gets the horizontal text-space origin before horizontal scaling.</summary>
    public double X { get; }

    /// <summary>Gets the horizontal text-space advance before horizontal scaling.</summary>
    public double Advance { get; }
}
