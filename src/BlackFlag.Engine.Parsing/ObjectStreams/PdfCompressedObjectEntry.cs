namespace BlackFlag.Engine.Parsing.ObjectStreams;

/// <summary>
/// Identifies one compressed object stored in a PDF object stream.
/// </summary>
public sealed record PdfCompressedObjectEntry
{
    /// <summary>
    /// Initializes a compressed-object index entry.
    /// </summary>
    public PdfCompressedObjectEntry(
        int objectNumber,
        int index,
        int relativeOffset)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(
            objectNumber,
            nameof(objectNumber));

        ArgumentOutOfRangeException.ThrowIfNegative(
            index,
            nameof(index));

        ArgumentOutOfRangeException.ThrowIfNegative(
            relativeOffset,
            nameof(relativeOffset));

        ObjectNumber = objectNumber;
        Index = index;
        RelativeOffset = relativeOffset;
    }

    /// <summary>
    /// Gets the compressed object's indirect object number.
    /// </summary>
    public int ObjectNumber { get; }

    /// <summary>
    /// Gets the zero-based position in the object stream index.
    /// </summary>
    public int Index { get; }

    /// <summary>
    /// Gets the byte offset relative to the first object-data byte.
    /// </summary>
    public int RelativeOffset { get; }
}

