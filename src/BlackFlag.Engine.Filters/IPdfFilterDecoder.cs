using System.Buffers;
using BlackFlag.Engine.Objects;

namespace BlackFlag.Engine.Filters;

/// <summary>
/// Defines a pure transformation from encoded PDF stream bytes to decoded
/// bytes.
/// </summary>
/// <remarks>
/// Implementations perform no file or network I/O, parsing, rendering,
/// caching, or document interpretation.
/// </remarks>
public interface IPdfFilterDecoder
{
    /// <summary>
    /// Gets the canonical PDF filter name handled by this decoder.
    /// </summary>
    PdfName Name { get; }

    /// <summary>
    /// Decodes input bytes into the supplied destination.
    /// </summary>
    /// <param name="encodedData">The encoded filter input.</param>
    /// <param name="destination">The decoded-byte destination.</param>
    /// <param name="context">The bounded operation context.</param>
    void Decode(
        ReadOnlySpan<byte> encodedData,
        IBufferWriter<byte> destination,
        PdfFilterDecodeContext context);
}
