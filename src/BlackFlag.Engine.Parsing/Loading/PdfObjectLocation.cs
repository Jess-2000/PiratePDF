using BlackFlag.Engine.Parsing.CrossReferences;

namespace BlackFlag.Engine.Parsing.Loading;

/// <summary>
/// Describes the effective storage location of one indirect PDF object.
/// </summary>
public sealed record PdfObjectLocation
{
    private PdfObjectLocation(
        PdfObjectId objectId,
        PdfObjectLocationKind kind,
        long offset,
        int objectStreamNumber,
        int objectStreamIndex)
    {
        ObjectId = objectId;
        Kind = kind;
        Offset = offset;
        ObjectStreamNumber = objectStreamNumber;
        ObjectStreamIndex = objectStreamIndex;
    }

    /// <summary>Gets the indirect object identifier.</summary>
    public PdfObjectId ObjectId { get; }

    /// <summary>Gets the storage kind.</summary>
    public PdfObjectLocationKind Kind { get; }

    /// <summary>Gets the absolute source offset for a direct object.</summary>
    public long Offset { get; }

    /// <summary>Gets the containing object-stream number.</summary>
    public int ObjectStreamNumber { get; }

    /// <summary>Gets the zero-based index inside the object stream.</summary>
    public int ObjectStreamIndex { get; }

    /// <summary>Creates a free-object location.</summary>
    public static PdfObjectLocation CreateFree(PdfObjectId objectId) =>
        new(objectId, PdfObjectLocationKind.Free, 0, 0, 0);

    /// <summary>Creates a direct-object location.</summary>
    public static PdfObjectLocation CreateDirect(
        PdfObjectId objectId,
        long offset)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(offset, nameof(offset));
        return new PdfObjectLocation(
            objectId,
            PdfObjectLocationKind.Direct,
            offset,
            0,
            0);
    }

    /// <summary>Creates a compressed-object location.</summary>
    public static PdfObjectLocation CreateCompressed(
        int objectNumber,
        int objectStreamNumber,
        int objectStreamIndex)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(
            objectNumber,
            nameof(objectNumber));
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(
            objectStreamNumber,
            nameof(objectStreamNumber));
        ArgumentOutOfRangeException.ThrowIfNegative(
            objectStreamIndex,
            nameof(objectStreamIndex));

        return new PdfObjectLocation(
            new PdfObjectId(objectNumber, generationNumber: 0),
            PdfObjectLocationKind.Compressed,
            0,
            objectStreamNumber,
            objectStreamIndex);
    }

    /// <summary>Converts a traditional cross-reference entry.</summary>
    public static PdfObjectLocation From(PdfCrossReferenceEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);
        return entry.IsInUse
            ? CreateDirect(entry.ObjectId, entry.Offset)
            : CreateFree(entry.ObjectId);
    }

    /// <summary>Converts a cross-reference-stream entry.</summary>
    public static PdfObjectLocation From(PdfCrossReferenceStreamEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);

        return entry.Kind switch
        {
            PdfCrossReferenceStreamEntryKind.Free =>
                CreateFree(entry.ObjectId),
            PdfCrossReferenceStreamEntryKind.InUse =>
                CreateDirect(entry.ObjectId, entry.Field2),
            PdfCrossReferenceStreamEntryKind.Compressed =>
                CreateCompressed(
                    entry.ObjectId.ObjectNumber,
                    checked((int)entry.Field2),
                    entry.Field3),
            _ => throw new ArgumentOutOfRangeException(
                nameof(entry),
                entry.Kind,
                "The cross-reference entry kind is not supported.")
        };
    }
}
