using BlackFlag.Engine.Objects;

namespace BlackFlag.Engine.Rendering.Images;

/// <summary>
/// Carries existing per-call limits to repository codecs that validate
/// intrinsic image dimensions before allocating decoded pixels.
/// </summary>
internal interface IBoundedPdfImageCodec : IPdfImageCodec
{
    PdfDecodedImage Decode(
        ReadOnlyMemory<byte> encodedBytes,
        PdfDictionary dictionary,
        PdfImageDecodeLimits limits,
        CancellationToken cancellationToken);
}
