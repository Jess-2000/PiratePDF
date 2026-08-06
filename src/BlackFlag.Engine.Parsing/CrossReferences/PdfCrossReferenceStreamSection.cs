using System.Collections.ObjectModel;

namespace BlackFlag.Engine.Parsing.CrossReferences;

/// <summary>
/// Represents one logical subsection decoded from a cross-reference stream.
/// </summary>
public sealed class PdfCrossReferenceStreamSection
{
    private readonly ReadOnlyCollection<PdfCrossReferenceStreamEntry> entries;

    /// <summary>
    /// Initializes a decoded cross-reference stream subsection.
    /// </summary>
    public PdfCrossReferenceStreamSection(
        int firstObjectNumber,
        IEnumerable<PdfCrossReferenceStreamEntry> entries)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(
            firstObjectNumber,
            nameof(firstObjectNumber));

        ArgumentNullException.ThrowIfNull(entries);

        PdfCrossReferenceStreamEntry[] copiedEntries =
            entries.ToArray();

        for (int index = 0; index < copiedEntries.Length; index++)
        {
            PdfCrossReferenceStreamEntry entry =
                copiedEntries[index] ??
                throw new ArgumentException(
                    "A cross-reference stream section cannot contain a null entry.",
                    nameof(entries));

            int expectedObjectNumber =
                checked(
                    firstObjectNumber +
                    index);

            if (entry.ObjectId.ObjectNumber != expectedObjectNumber)
            {
                throw new ArgumentException(
                    $"Entry {index} identifies object "
                    + $"{entry.ObjectId.ObjectNumber}, but object "
                    + $"{expectedObjectNumber} was expected.",
                    nameof(entries));
            }
        }

        FirstObjectNumber = firstObjectNumber;
        this.entries =
            Array.AsReadOnly(
                copiedEntries);
    }

    /// <summary>
    /// Gets the first object number represented by the section.
    /// </summary>
    public int FirstObjectNumber { get; }

    /// <summary>
    /// Gets the decoded entries in source order.
    /// </summary>
    public IReadOnlyList<PdfCrossReferenceStreamEntry> Entries =>
        entries;
}

