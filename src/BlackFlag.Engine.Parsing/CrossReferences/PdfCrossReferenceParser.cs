using BlackFlag.Engine.Abstractions.IO;
using BlackFlag.Engine.Objects;

namespace BlackFlag.Engine.Parsing.CrossReferences;

/// <summary>
/// Parses traditional PDF cross-reference tables and incremental-update chains.
/// </summary>
public sealed partial class PdfCrossReferenceParser
{
    private const int MaximumStartXRefSearchLength = 1024 * 1024;

    private static readonly PdfName PreviousOffsetKey =
        new("Prev");

    private readonly IRandomAccessSource source;

    /// <summary>
    /// Initializes a traditional cross-reference parser.
    /// </summary>
    /// <param name="source">The random-access PDF source.</param>
    public PdfCrossReferenceParser(
        IRandomAccessSource source)
    {
        ArgumentNullException.ThrowIfNull(source);
        this.source = source;
    }

    /// <summary>
    /// Parses the complete traditional cross-reference chain.
    /// </summary>
    /// <returns>
    /// A chain containing physical tables oldest-to-newest and the effective
    /// newest-wins lookup.
    /// </returns>
    public PdfCrossReferenceChain Parse()
    {
        long currentOffset =
            ParseStartXRef();

        var visitedOffsets =
            new HashSet<long>();

        var newestToOldest =
            new List<PdfCrossReferenceTable>();

        while (true)
        {
            if (!visitedOffsets.Add(currentOffset))
            {
                throw new FormatException(
                    $"The trailer /Prev chain contains a cycle at offset "
                    + $"{currentOffset}.");
            }

            PdfCrossReferenceTable table =
                ParseTable(currentOffset);

            newestToOldest.Add(table);

            if (!TryReadPreviousOffset(
                    table.Trailer,
                    out long previousOffset))
            {
                break;
            }

            if (previousOffset >= source.Length)
            {
                throw new FormatException(
                    $"The trailer /Prev offset {previousOffset} is outside "
                    + "the source.");
            }

            currentOffset = previousOffset;
        }

        newestToOldest.Reverse();

        var builder =
            new PdfCrossReferenceChainBuilder();

        foreach (PdfCrossReferenceTable table in newestToOldest)
        {
            builder.AddTable(table);
        }

        return builder.Build();
    }

    private static bool TryReadPreviousOffset(
        PdfDictionary trailer,
        out long offset)
    {
        if (!trailer.TryGetValue(
                PreviousOffsetKey,
                out PdfObject value))
        {
            offset = default;
            return false;
        }

        if (value is not PdfInteger integer)
        {
            throw new FormatException(
                "A trailer /Prev value must be a PDF integer.");
        }

        if (integer.Value < 0)
        {
            throw new FormatException(
                "A trailer /Prev value cannot be negative.");
        }

        offset = integer.Value;
        return true;
    }
}
