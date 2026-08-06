namespace BlackFlag.Engine.Parsing.CrossReferences;

/// <summary>
/// Represents one entry in a traditional PDF cross-reference subsection.
/// </summary>
public sealed record PdfCrossReferenceEntry
{
    /// <summary>
    /// Initializes a cross-reference entry.
    /// </summary>
    /// <param name="objectId">The entry's object identifier.</param>
    /// <param name="offset">
    /// The source offset for an in-use entry or the next free object number
    /// for a free entry.
    /// </param>
    /// <param name="isInUse">
    /// Whether the entry is in use.
    /// </param>
    public PdfCrossReferenceEntry(
        PdfObjectId objectId,
        long offset,
        bool isInUse)
    {
        if (offset < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(offset),
                offset,
                "A cross-reference entry offset cannot be negative.");
        }

        ObjectId = objectId;
        Offset = offset;
        IsInUse = isInUse;
    }

    /// <summary>
    /// Gets the entry's object identifier.
    /// </summary>
    public PdfObjectId ObjectId { get; }

    /// <summary>
    /// Gets the source offset or next free object number.
    /// </summary>
    public long Offset { get; }

    /// <summary>
    /// Gets whether the entry is in use.
    /// </summary>
    public bool IsInUse { get; }
}

