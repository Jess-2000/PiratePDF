using BlackFlag.Engine.Filters;
using BlackFlag.Engine.Objects;

namespace BlackFlag.Engine.Rendering.Fonts;

/// <summary>Decodes font and CMap streams through the BlackFlag filter pipeline.</summary>
public sealed class PdfFontStreamDecoder
{
    private static readonly PdfName FilterKey = new("Filter");
    private static readonly PdfName DecodeParmsKey = new("DecodeParms");
    private readonly IPdfFilterPipeline pipeline;
    private readonly PdfFilterDecodeLimits limits;

    /// <summary>Initializes a font-stream decoder using production filter registrations.</summary>
    public PdfFontStreamDecoder()
        : this(
            new PdfFilterPipeline(PdfFilterRegistrations.CreateDefault()),
            new PdfFilterDecodeLimits())
    {
    }

    /// <summary>Initializes a font-stream decoder with explicit dependencies.</summary>
    public PdfFontStreamDecoder(
        IPdfFilterPipeline pipeline,
        PdfFilterDecodeLimits limits)
    {
        ArgumentNullException.ThrowIfNull(pipeline);
        ArgumentNullException.ThrowIfNull(limits);
        this.pipeline = pipeline;
        this.limits = limits;
    }

    /// <summary>Decodes one PDF stream.</summary>
    public ReadOnlyMemory<byte> Decode(
        PdfStream stream,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stream);

        PdfName[] filters = ReadFilters(stream.Dictionary);
        PdfDictionary?[]? parameters = ReadDecodeParameters(
            stream.Dictionary,
            filters.Length);

        return pipeline.Decode(
            stream.Data,
            filters,
            parameters,
            limits,
            cancellationToken);
    }

    private static PdfName[] ReadFilters(PdfDictionary dictionary)
    {
        if (!dictionary.TryGetValue(FilterKey, out PdfObject value))
        {
            return [];
        }

        return value switch
        {
            PdfName name => [name],
            PdfArray array => array.Select(item =>
                item as PdfName ??
                throw new InvalidDataException("Every font-stream /Filter entry must be a name.")).ToArray(),
            _ => throw new InvalidDataException("A font-stream /Filter entry must be a name or array.")
        };
    }

    private static PdfDictionary?[]? ReadDecodeParameters(
        PdfDictionary dictionary,
        int filterCount)
    {
        if (!dictionary.TryGetValue(DecodeParmsKey, out PdfObject value))
        {
            return null;
        }

        if (value is PdfNull)
        {
            return Enumerable.Repeat<PdfDictionary?>(null, filterCount).ToArray();
        }

        if (value is PdfDictionary single)
        {
            if (filterCount != 1)
            {
                throw new InvalidDataException("A single font-stream /DecodeParms dictionary requires one filter.");
            }

            return [single];
        }

        if (value is not PdfArray array || array.Count != filterCount)
        {
            throw new InvalidDataException("A font-stream /DecodeParms array must align with /Filter.");
        }

        return array.Select(item => item switch
        {
            PdfDictionary parameters => parameters,
            PdfNull => null,
            _ => throw new InvalidDataException("Font-stream /DecodeParms entries must be dictionaries or null.")
        }).ToArray();
    }
}
