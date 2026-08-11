namespace BlackFlag.Engine.Rendering.Images;

/// <summary>Represents an immutable decoded image in straight RGBA byte order.</summary>
public sealed class PdfDecodedImage
{
    private readonly byte[] pixels;

    /// <summary>Initializes decoded image pixels.</summary>
    public PdfDecodedImage(int width, int height, ReadOnlySpan<byte> rgbaPixels)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(width);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(height);

        int required = checked(width * height * 4);
        if (rgbaPixels.Length != required)
        {
            throw new ArgumentException(
                $"RGBA data must contain exactly {required} bytes.",
                nameof(rgbaPixels));
        }

        Width = width;
        Height = height;
        pixels = rgbaPixels.ToArray();
    }

    /// <summary>Gets the pixel width.</summary>
    public int Width { get; }

    /// <summary>Gets the pixel height.</summary>
    public int Height { get; }

    /// <summary>Gets immutable straight-alpha RGBA bytes.</summary>
    public ReadOnlyMemory<byte> Pixels => pixels;
}
