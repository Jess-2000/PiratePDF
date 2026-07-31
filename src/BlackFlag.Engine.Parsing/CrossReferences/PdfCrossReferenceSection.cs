using System.Collections.ObjectModel;

namespace BlackFlag.Engine.Parsing.CrossReferences;

/// <summary>
/// Represents one physical subsection of a traditional cross-reference table.
/// </summary>
public sealed class PdfCrossReferenceSection
{
    private readonly ReadOnlyCollection<PdfCrossReferenceEntry> entries;

    /// <summary>
    /// Initializes a cross-reference subsection.
    /// </summary>
    /// <param name="firstObjectNumber">
    /// The first object number declared by the subsection.
    /// </param>
    /// <param name="entries">The entries in source order.</param>
    public PdfCrossReferenceSection(
        int firstObjectNumber,
        IEnumerable<PdfCrossReferenceEntry> entries)
    {
        if (firstObjectNumber < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(firstObjectNumber),
                firstObjectNumber,
                "The first object number cannot be negative.");
        }

        ArgumentNullException.ThrowIfNull(entries);

        PdfCrossReferenceEntry[] copiedEntries =
            entries.ToArray();

        for (int index = 0; index < copiedEntries.Length; index++)
        {
            PdfCrossReferenceEntry entry =
                copiedEntries[index] ??
                throw new ArgumentException(
                    "A cross-reference subsection cannot contain a null entry.",
                    nameof(entries));

            int expectedObjectNumber =
                checked(firstObjectNumber + index);

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
        this.entries = Array.AsReadOnly(copiedEntries);
    }

    /// <summary>
    /// Gets the first object number declared by the subsection.
    /// </summary>
    public int FirstObjectNumber { get; }

    /// <summary>
    /// Gets the entries in source order.
    /// </summary>
    public IReadOnlyList<PdfCrossReferenceEntry> Entries =>
        entries;
}
