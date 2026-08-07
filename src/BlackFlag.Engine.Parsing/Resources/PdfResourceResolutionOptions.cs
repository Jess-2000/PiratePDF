namespace BlackFlag.Engine.Parsing.Resources;

/// <summary>
/// Configures defensive limits for PDF resource resolution.
/// </summary>
public sealed class PdfResourceResolutionOptions
{
    /// <summary>Gets the maximum nested resource-scope depth.</summary>
    public int MaximumResourceDepth { get; init; } = 64;

    /// <summary>Gets the maximum number of descendant fonts in one font.</summary>
    public int MaximumFontDescendantCount { get; init; } = 32;

    /// <summary>Gets the maximum encoded embedded-font stream size.</summary>
    public long MaximumEmbeddedFontBytes { get; init; } = 256L * 1024 * 1024;

    /// <summary>Gets the maximum encoded image XObject stream size.</summary>
    public long MaximumImageEncodedBytes { get; init; } = 1024L * 1024 * 1024;

    /// <summary>Gets the maximum encoded ICC profile stream size.</summary>
    public long MaximumIccProfileBytes { get; init; } = 64L * 1024 * 1024;

    internal void Validate()
    {
        if (MaximumResourceDepth <= 0
            || MaximumFontDescendantCount <= 0
            || MaximumEmbeddedFontBytes <= 0
            || MaximumImageEncodedBytes <= 0
            || MaximumIccProfileBytes <= 0)
        {
            throw new InvalidOperationException(
                "All PDF resource-resolution limits must be greater than zero.");
        }
    }
}
