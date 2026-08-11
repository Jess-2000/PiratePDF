namespace BlackFlag.Engine.Rendering.Raster;

/// <summary>Owns an immutable raster result produced by a page renderer.</summary>
public sealed class PdfRasterSurface
{
    private readonly byte[] pixels;

    /// <summary>Initializes a raster surface from owned pixel bytes.</summary>
    public PdfRasterSurface(
        int width,
        int height,
        int stride,
        PdfPixelFormat pixelFormat,
        byte[] pixels)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(width);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(height);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(stride);
        ArgumentNullException.ThrowIfNull(pixels);

        int requiredLength = checked(stride * height);
        if (pixels.Length != requiredLength)
        {
            throw new ArgumentException(
                $"Pixel buffer length must be exactly {requiredLength} bytes.",
                nameof(pixels));
        }

        Width = width;
        Height = height;
        Stride = stride;
        PixelFormat = pixelFormat;
        this.pixels = pixels;
    }

    /// <summary>Gets the surface width in pixels.</summary>
    public int Width { get; }

    /// <summary>Gets the surface height in pixels.</summary>
    public int Height { get; }

    /// <summary>Gets the byte stride for one raster row.</summary>
    public int Stride { get; }

    /// <summary>Gets the pixel format.</summary>
    public PdfPixelFormat PixelFormat { get; }

    /// <summary>Gets immutable pixel bytes.</summary>
    public ReadOnlyMemory<byte> Pixels => pixels;
}
