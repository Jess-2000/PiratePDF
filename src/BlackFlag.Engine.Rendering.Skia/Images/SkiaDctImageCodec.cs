using BlackFlag.Engine.Objects;
using BlackFlag.Engine.Rendering.Images;
using SkiaSharp;

namespace BlackFlag.Engine.Rendering.Skia.Images;

/// <summary>Decodes PDF DCTDecode payloads through the Skia backend.</summary>
public sealed class SkiaDctImageCodec : IBoundedPdfImageCodec
{
    private readonly Func<SKImageInfo, SKBitmap?> allocateBitmap;

    /// <summary>Initializes the Skia JPEG codec.</summary>
    public SkiaDctImageCodec()
        : this(AllocateBitmap)
    {
    }

    internal SkiaDctImageCodec(Func<SKImageInfo, SKBitmap?> allocateBitmap)
    {
        ArgumentNullException.ThrowIfNull(allocateBitmap);
        this.allocateBitmap = allocateBitmap;
    }

    /// <inheritdoc />
    public string FilterName => "DCTDecode";

    /// <inheritdoc />
    public PdfDecodedImage Decode(
        ReadOnlyMemory<byte> encodedBytes,
        PdfDictionary dictionary,
        CancellationToken cancellationToken = default) =>
        Decode(encodedBytes, PdfImageDecodeLimits.Default, cancellationToken);

    PdfDecodedImage IBoundedPdfImageCodec.Decode(
        ReadOnlyMemory<byte> encodedBytes,
        PdfDictionary dictionary,
        PdfImageDecodeLimits limits,
        CancellationToken cancellationToken) =>
        Decode(encodedBytes, limits, cancellationToken);

    private PdfDecodedImage Decode(
        ReadOnlyMemory<byte> encodedBytes,
        PdfImageDecodeLimits limits,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        limits.Validate();
        PdfImageDecoder.ValidateEncodedLength(encodedBytes.Length, limits);

        using SKData data = SKData.CreateCopy(encodedBytes.Span);
        using SKCodec? codec = SKCodec.Create(data);
        if (codec is null)
        {
            throw new InvalidDataException("DCTDecode payload is not a supported JPEG image.");
        }

        int width = codec.Info.Width;
        int height = codec.Info.Height;
        int byteCount = PdfImageDecoder.ValidateDimensions(width, height, limits);
        int rowBytes = checked(width * 4);
        SKImageInfo info = new(
            width,
            height,
            SKColorType.Rgba8888,
            SKAlphaType.Unpremul);

        cancellationToken.ThrowIfCancellationRequested();
        using SKBitmap? bitmap = allocateBitmap(info);
        if (bitmap is null)
        {
            throw new InvalidDataException("JPEG pixel allocation failed.");
        }

        IntPtr pixels = bitmap.GetPixels(out IntPtr pixelLength);
        if (pixels == IntPtr.Zero || pixelLength.ToInt64() < byteCount ||
            bitmap.Width != width || bitmap.Height != height || bitmap.RowBytes != rowBytes)
        {
            throw new InvalidDataException("JPEG pixel allocation did not provide the required RGBA buffer.");
        }

        SKCodecResult result = codec.GetPixels(info, pixels);
        if (result is not (SKCodecResult.Success or SKCodecResult.IncompleteInput))
        {
            throw new InvalidDataException($"JPEG decoding failed with result '{result}'.");
        }

        cancellationToken.ThrowIfCancellationRequested();
        byte[] rowPixels = new byte[byteCount];
        System.Runtime.InteropServices.Marshal.Copy(
            pixels,
            rowPixels,
            0,
            byteCount);

        byte[] packed = new byte[byteCount];
        for (int row = 0; row < info.Height; row++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Buffer.BlockCopy(
                rowPixels,
                row * rowBytes,
                packed,
                row * rowBytes,
                rowBytes);
        }

        return new PdfDecodedImage(info.Width, info.Height, packed);
    }

    private static SKBitmap? AllocateBitmap(SKImageInfo info)
    {
        var bitmap = new SKBitmap();
        if (bitmap.TryAllocPixels(info, info.RowBytes))
        {
            return bitmap;
        }

        bitmap.Dispose();
        return null;
    }
}
