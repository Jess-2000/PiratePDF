namespace BlackFlag.Engine.Rendering.Images;

/// <summary>Defines bounded resource limits for image decoding.</summary>
public sealed record PdfImageDecodeLimits
{
    /// <summary>Gets production defaults.</summary>
    public static PdfImageDecodeLimits Default { get; } = new();

    /// <summary>Gets the maximum decoded pixel count.</summary>
    public long MaximumDecodedPixels { get; init; } = 100_000_000;

    /// <summary>Gets the maximum encoded image bytes.</summary>
    public long MaximumEncodedBytes { get; init; } = 256L * 1024 * 1024;

    /// <summary>Gets the maximum image dimension.</summary>
    public int MaximumDimension { get; init; } = 100_000;

    /// <summary>Validates limits.</summary>
    public void Validate()
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(MaximumDecodedPixels);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(MaximumEncodedBytes);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(MaximumDimension);
    }
}
