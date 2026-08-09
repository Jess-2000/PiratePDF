using BlackFlag.Engine.Objects;
using BlackFlag.Engine.Rendering.Images;
using SkiaSharp;

namespace BlackFlag.Engine.Rendering.Skia.Images;

/// <summary>Decodes PDF DCTDecode payloads through the Skia backend.</summary>
public sealed class SkiaDctImageCodec : IPdfImageCodec
{
    /// <inheritdoc />
    public string FilterName => "DCTDecode";

    /// <inheritdoc />
    public PdfDecodedImage Decode(
        ReadOnlyMemory<byte> encodedBytes,
        PdfDictionary dictionary,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using SKData data = SKData.CreateCopy(encodedBytes.Span);
        using SKCodec? codec = SKCodec.Create(data);
        if (codec is null)
        {
            throw new InvalidDataException("DCTDecode payload is not a supported JPEG image.");
        }

        SKImageInfo info = new(
            codec.Info.Width,
            codec.Info.Height,
            SKColorType.Rgba8888,
            SKAlphaType.Unpremul);

        using SKBitmap bitmap = new(info);
        SKCodecResult result = codec.GetPixels(info, bitmap.GetPixels());
        if (result is not (SKCodecResult.Success or SKCodecResult.IncompleteInput))
        {
            throw new InvalidDataException($"JPEG decoding failed with result '{result}'.");
        }

        int byteCount = checked(info.RowBytes * info.Height);
        byte[] rowPixels = new byte[byteCount];
        System.Runtime.InteropServices.Marshal.Copy(
            bitmap.GetPixels(),
            rowPixels,
            0,
            byteCount);

        byte[] packed = new byte[checked(info.Width * info.Height * 4)];
        for (int row = 0; row < info.Height; row++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Buffer.BlockCopy(
                rowPixels,
                row * info.RowBytes,
                packed,
                row * info.Width * 4,
                info.Width * 4);
        }

        return new PdfDecodedImage(info.Width, info.Height, packed);
    }
}
