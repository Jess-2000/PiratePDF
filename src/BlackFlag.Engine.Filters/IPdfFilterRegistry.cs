using BlackFlag.Engine.Objects;

namespace BlackFlag.Engine.Filters;

/// <summary>
/// Resolves PDF filter decoders by canonical PDF name.
/// </summary>
public interface IPdfFilterRegistry
{
    /// <summary>
    /// Attempts to resolve a registered decoder.
    /// </summary>
    /// <param name="name">The canonical PDF filter name.</param>
    /// <param name="decoder">
    /// The resolved decoder, or <see langword="null"/> when unavailable.
    /// </param>
    /// <returns>
    /// <see langword="true"/> when a decoder is registered.
    /// </returns>
    bool TryGet(
        PdfName name,
        out IPdfFilterDecoder? decoder);

    /// <summary>
    /// Resolves a decoder or throws a classified exception.
    /// </summary>
    /// <param name="name">The canonical PDF filter name.</param>
    /// <returns>The registered decoder.</returns>
    IPdfFilterDecoder GetRequired(PdfName name);
}
