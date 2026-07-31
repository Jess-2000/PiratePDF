using System.Collections.ObjectModel;
using BlackFlag.Engine.Objects;

namespace BlackFlag.Engine.Parsing.CrossReferences;

/// <summary>
/// Represents one physical traditional cross-reference table and trailer.
/// </summary>
public sealed class PdfCrossReferenceTable
{
    private readonly ReadOnlyCollection<PdfCrossReferenceSection> sections;

    /// <summary>
    /// Initializes a traditional cross-reference table.
    /// </summary>
    /// <param name="offset">The table's absolute source offset.</param>
    /// <param name="sections">The physical subsections in source order.</param>
    /// <param name="trailer">The parsed trailer dictionary.</param>
    public PdfCrossReferenceTable(
        long offset,
        IEnumerable<PdfCrossReferenceSection> sections,
        PdfDictionary trailer)
    {
        if (offset < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(offset),
                offset,
                "A cross-reference table offset cannot be negative.");
        }

        ArgumentNullException.ThrowIfNull(sections);
        ArgumentNullException.ThrowIfNull(trailer);

        PdfCrossReferenceSection[] copiedSections =
            sections.ToArray();

        if (copiedSections.Length == 0)
        {
            throw new ArgumentException(
                "A traditional cross-reference table must contain at least one subsection.",
                nameof(sections));
        }

        var identifiers =
            new HashSet<PdfObjectId>();

        foreach (PdfCrossReferenceSection section in copiedSections)
        {
            if (section is null)
            {
                throw new ArgumentException(
                    "A cross-reference table cannot contain a null subsection.",
                    nameof(sections));
            }

            foreach (PdfCrossReferenceEntry entry in section.Entries)
            {
                if (!identifiers.Add(entry.ObjectId))
                {
                    throw new ArgumentException(
                        $"The table contains duplicate entry {entry.ObjectId}.",
                        nameof(sections));
                }
            }
        }

        Offset = offset;
        this.sections = Array.AsReadOnly(copiedSections);
        Trailer = trailer;
    }

    /// <summary>
    /// Gets the table's absolute source offset.
    /// </summary>
    public long Offset { get; }

    /// <summary>
    /// Gets the physical subsections in source order.
    /// </summary>
    public IReadOnlyList<PdfCrossReferenceSection> Sections =>
        sections;

    /// <summary>
    /// Gets the table's trailer dictionary.
    /// </summary>
    public PdfDictionary Trailer { get; }
}
