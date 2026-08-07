using BlackFlag.Engine.Objects;

namespace BlackFlag.Engine.Rendering.Resources;

/// <summary>
/// Represents a resolved image XObject without eagerly decoding its pixels.
/// </summary>
public sealed class PdfResolvedImageXObject : PdfResolvedXObject
{
    internal PdfResolvedImageXObject(
        PdfResourceIdentity identity,
        PdfStream stream,
        int width,
        int height,
        int? bitsPerComponent,
        bool isImageMask,
        bool interpolate,
        PdfObject? colorSpace,
        PdfObject? decode,
        PdfObject? mask,
        PdfObject? softMask,
        PdfObject? filters)
        : base(identity, stream)
    {
        Width = width;
        Height = height;
        BitsPerComponent = bitsPerComponent;
        IsImageMask = isImageMask;
        Interpolate = interpolate;
        ColorSpace = colorSpace;
        Decode = decode;
        Mask = mask;
        SoftMask = softMask;
        Filters = filters;
    }

    /// <summary>Gets the image width in samples.</summary>
    public int Width { get; }

    /// <summary>Gets the image height in samples.</summary>
    public int Height { get; }

    /// <summary>Gets the number of bits per color component when applicable.</summary>
    public int? BitsPerComponent { get; }

    /// <summary>Gets a value indicating whether the image is an image mask.</summary>
    public bool IsImageMask { get; }

    /// <summary>Gets the image interpolation hint.</summary>
    public bool Interpolate { get; }

    /// <summary>Gets the source color-space object when present.</summary>
    public PdfObject? ColorSpace { get; }

    /// <summary>Gets the source decode array when present.</summary>
    public PdfObject? Decode { get; }

    /// <summary>Gets the explicit mask object when present.</summary>
    public PdfObject? Mask { get; }

    /// <summary>Gets the soft-mask object when present.</summary>
    public PdfObject? SoftMask { get; }

    /// <summary>Gets the source filter name or filter array when present.</summary>
    public PdfObject? Filters { get; }
}
