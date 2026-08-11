using BlackFlag.Engine.Objects;

namespace BlackFlag.Engine.Rendering.Images;

/// <summary>Decodes an image-specific PDF stream codec.</summary>
public interface IPdfImageCodec
{
    /// <summary>Gets the canonical PDF filter name handled by this codec.</summary>
    string FilterName { get; }

    /// <summary>Decodes one codec payload to RGBA pixels.</summary>
    PdfDecodedImage Decode(
        ReadOnlyMemory<byte> encodedBytes,
        PdfDictionary dictionary,
        CancellationToken cancellationToken = default);
}
