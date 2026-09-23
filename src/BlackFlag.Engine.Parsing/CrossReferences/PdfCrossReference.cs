using System.Collections.ObjectModel;
using BlackFlag.Engine.Objects;

namespace BlackFlag.Engine.Parsing.CrossReferences;

/// <summary>
/// Represents one decoded PDF cross-reference stream.
/// </summary>
public sealed class PdfCrossReference
{
    private readonly ReadOnlyCollection<PdfCrossReferenceStreamSection> sections;

    /// <summary>
    /// Initializes a decoded cross-reference stream.
    /// </summary>
    public PdfCrossReference(
        long offset,
        PdfDictionary dictionary,
        IEnumerable<PdfCrossReferenceStreamSection> sections)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(
            offset,
            nameof(offset));

        ArgumentNullException.ThrowIfNull(dictionary);
        ArgumentNullException.ThrowIfNull(sections);

        PdfCrossReferenceStreamSection[] copiedSections =
            sections.ToArray();

        if (copiedSections.Length == 0)
        {
            throw new ArgumentException(
                "A cross-reference stream must contain at least one section.",
                nameof(sections));
        }

        var objectNumbers =
            new HashSet<int>();

        foreach (PdfCrossReferenceStreamSection section in copiedSections)
        {
            if (section is null)
            {
                throw new ArgumentException(
                    "A cross-reference stream cannot contain a null section.",
                    nameof(sections));
            }

            foreach (PdfCrossReferenceStreamEntry entry in section.Entries)
            {
                if (!objectNumbers.Add(
                        entry.ObjectId.ObjectNumber))
                {
                    throw new ArgumentException(
                        $"The cross-reference stream contains duplicate object "
                        + $"{entry.ObjectId.ObjectNumber}.",
                        nameof(sections));
                }
            }
        }

        Offset = offset;
        Dictionary = dictionary;
        this.sections =
            Array.AsReadOnly(
                copiedSections);
    }

    /// <summary>
    /// Gets the absolute source offset of the indirect stream object.
    /// </summary>
    public long Offset { get; }

    /// <summary>
    /// Gets the cross-reference stream dictionary.
    /// </summary>
    public PdfDictionary Dictionary { get; }

    /// <summary>
    /// Gets the decoded logical sections.
    /// </summary>
    public IReadOnlyList<PdfCrossReferenceStreamSection> Sections =>
        sections;
}

