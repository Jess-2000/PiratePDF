using BlackFlag.Engine.Objects;

namespace BlackFlag.Engine.Rendering.Graphics;

/// <summary>Preserves an inline image dictionary and its encoded bytes.</summary>
public sealed class PdfInlineImage
{
    private readonly byte[] encodedBytes;

    /// <summary>Initializes an immutable inline-image value.</summary>
    public PdfInlineImage(PdfDictionary dictionary, ReadOnlySpan<byte> encodedBytes)
    {
        ArgumentNullException.ThrowIfNull(dictionary);
        Dictionary = dictionary;
        this.encodedBytes = encodedBytes.ToArray();
    }

    /// <summary>Gets the inline-image parameter dictionary.</summary>
    public PdfDictionary Dictionary { get; }

    /// <summary>Gets the encoded image bytes.</summary>
    public ReadOnlyMemory<byte> EncodedBytes => encodedBytes;
}
