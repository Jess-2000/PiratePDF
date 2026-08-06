using System.Collections.ObjectModel;

namespace BlackFlag.Engine.Parsing.CrossReferences;

/// <summary>
/// Represents a traditional cross-reference revision chain and its effective
/// entry lookup.
/// </summary>
public sealed class PdfCrossReferenceChain
{
    private readonly ReadOnlyCollection<PdfCrossReferenceTable> tables;
    private readonly ReadOnlyDictionary<PdfObjectId, PdfCrossReferenceEntry>
        entries;

    /// <summary>
    /// Initializes an immutable cross-reference chain.
    /// </summary>
    /// <param name="tables">
    /// Physical tables ordered from oldest to newest.
    /// </param>
    /// <param name="entries">
    /// The effective newest-wins entry lookup.
    /// </param>
    public PdfCrossReferenceChain(
        IEnumerable<PdfCrossReferenceTable> tables,
        IEnumerable<KeyValuePair<PdfObjectId, PdfCrossReferenceEntry>> entries)
    {
        ArgumentNullException.ThrowIfNull(tables);
        ArgumentNullException.ThrowIfNull(entries);

        PdfCrossReferenceTable[] copiedTables =
            tables.ToArray();

        if (copiedTables.Length == 0)
        {
            throw new ArgumentException(
                "A cross-reference chain must contain at least one table.",
                nameof(tables));
        }

        if (copiedTables.Any(static table => table is null))
        {
            throw new ArgumentException(
                "A cross-reference chain cannot contain a null table.",
                nameof(tables));
        }

        var copiedEntries =
            new Dictionary<PdfObjectId, PdfCrossReferenceEntry>();

        foreach (
            KeyValuePair<PdfObjectId, PdfCrossReferenceEntry> pair
            in entries)
        {
            ArgumentNullException.ThrowIfNull(pair.Value);

            if (pair.Key != pair.Value.ObjectId)
            {
                throw new ArgumentException(
                    "A lookup key must match its cross-reference entry identifier.",
                    nameof(entries));
            }

            if (!copiedEntries.TryAdd(pair.Key, pair.Value))
            {
                throw new ArgumentException(
                    $"The effective lookup contains duplicate entry {pair.Key}.",
                    nameof(entries));
            }
        }

        this.tables = Array.AsReadOnly(copiedTables);
        this.entries =
            new ReadOnlyDictionary<PdfObjectId, PdfCrossReferenceEntry>(
                copiedEntries);
    }

    /// <summary>
    /// Gets physical tables ordered from oldest to newest.
    /// </summary>
    public IReadOnlyList<PdfCrossReferenceTable> Tables =>
        tables;

    /// <summary>
    /// Gets the effective newest-wins entry lookup.
    /// </summary>
    public IReadOnlyDictionary<PdfObjectId, PdfCrossReferenceEntry> Entries =>
        entries;
}

