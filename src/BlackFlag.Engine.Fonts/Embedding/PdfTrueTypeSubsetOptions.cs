namespace BlackFlag.Engine.Fonts.Embedding;

/// <summary>
/// Bounds composite-glyph dependency traversal and generated subset size
/// independently of the outer external-font sanitizer.
/// </summary>
public sealed class PdfTrueTypeSubsetOptions
{
    /// <summary>Gets the maximum composite recursion depth.</summary>
    public int MaximumCompositeDepth { get; init; } = 32;

    /// <summary>Gets the maximum component records allowed in one composite glyph.</summary>
    public int MaximumComponentsPerGlyph { get; init; } = 256;

    /// <summary>Gets the maximum number of glyphs retained by one subset.</summary>
    public int MaximumRetainedGlyphs { get; init; } = 16384;

    /// <summary>Gets the maximum generated SFNT byte length before re-sanitization.</summary>
    public int MaximumGeneratedBytes { get; init; } = 64 * 1024 * 1024;

    internal void Validate()
    {
        ValidatePositive(
            MaximumCompositeDepth,
            nameof(MaximumCompositeDepth));

        ValidatePositive(
            MaximumComponentsPerGlyph,
            nameof(MaximumComponentsPerGlyph));

        ValidatePositive(
            MaximumRetainedGlyphs,
            nameof(MaximumRetainedGlyphs));

        ValidatePositive(
            MaximumGeneratedBytes,
            nameof(MaximumGeneratedBytes));
    }

    private static void ValidatePositive(
        int value,
        string name)
    {
        if (value <= 0)
        {
            throw new ArgumentOutOfRangeException(
                name,
                value,
                "TrueType subset limits must be positive.");
        }
    }
}
