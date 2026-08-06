using BlackFlag.Engine.Objects;

namespace BlackFlag.Engine.Filters;

/// <summary>
/// Stores immutable mappings from canonical PDF filter names to decoders.
/// </summary>
public sealed class PdfFilterRegistry : IPdfFilterRegistry
{
    private readonly Dictionary<PdfName, IPdfFilterDecoder> decoders;

    /// <summary>
    /// Initializes an empty registry.
    /// </summary>
    public PdfFilterRegistry()
        : this([])
    {
    }

    /// <summary>
    /// Initializes a registry containing the supplied decoders.
    /// </summary>
    /// <param name="decoders">The decoders to register.</param>
    public PdfFilterRegistry(
        IEnumerable<IPdfFilterDecoder> decoders)
    {
        ArgumentNullException.ThrowIfNull(decoders);

        var registrations =
            new Dictionary<PdfName, IPdfFilterDecoder>();

        foreach (IPdfFilterDecoder decoder in decoders)
        {
            ArgumentNullException.ThrowIfNull(decoder);

            if (!registrations.TryAdd(
                    decoder.Name,
                    decoder))
            {
                throw new ArgumentException(
                    $"A decoder is already registered for /{decoder.Name.Value}.",
                    nameof(decoders));
            }
        }

        this.decoders = registrations;
    }

    /// <inheritdoc />
    public bool TryGet(
        PdfName name,
        out IPdfFilterDecoder? decoder)
    {
        ArgumentNullException.ThrowIfNull(name);

        return decoders.TryGetValue(
            name,
            out decoder);
    }

    /// <inheritdoc />
    public IPdfFilterDecoder GetRequired(
        PdfName name)
    {
        ArgumentNullException.ThrowIfNull(name);

        if (
            TryGet(
                name,
                out IPdfFilterDecoder? decoder) &&
            decoder is not null)
        {
            return decoder;
        }

        throw new PdfFilterDecodeException(
            PdfFilterDecodeFailure.UnsupportedFilter,
            $"No decoder is registered for PDF filter /{name.Value}.");
    }
}

