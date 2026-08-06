namespace BlackFlag.Engine.Parsing.CrossReferences;

/// Represents one decoded entry from a PDF cross-reference stream.
public sealed record PdfCrossReferenceStreamEntry

{
    private PdfCrossReferenceStreamEntry(

        PdfObjectId objectId,

        PdfCrossReferenceStreamEntryKind kind,

        long field2,

        int field3)

    {

        ObjectId = objectId;

        Kind = kind;

        Field2 = field2;

        Field3 = field3;

    }

    /// Gets the object identifier represented by the entry.
    public PdfObjectId ObjectId { get; }

    /// Gets the entry kind.
    public PdfCrossReferenceStreamEntryKind Kind { get; }

    /// Gets the second decoded field.
    /// For free entries, this is the next free object number. For in-use
    /// entries, this is the absolute source offset. For compressed entries, this is the containing object-stream number.
    public long Field2 { get; }

    /// Gets the third decoded field.
    /// For free and in-use entries, this is the generation number. For
    /// compressed entries, this is the zero-based index inside the object stream.
    public int Field3 { get; }

    /// Creates a free entry.
    public static PdfCrossReferenceStreamEntry CreateFree(

        int objectNumber,

        long nextFreeObjectNumber,

        int generationNumber)

    {

        ValidateNonNegative(

            nextFreeObjectNumber,

            nameof(nextFreeObjectNumber));



        ValidateGeneration(

            generationNumber);

        return new PdfCrossReferenceStreamEntry(

            new PdfObjectId(

                objectNumber,

                generationNumber),

            PdfCrossReferenceStreamEntryKind.Free,

            nextFreeObjectNumber,

            generationNumber);

    }

    /// Creates an in-use entry.
    public static PdfCrossReferenceStreamEntry CreateInUse(

        int objectNumber,

        long offset,

        int generationNumber)

    {

        ValidateNonNegative(

            offset,

            nameof(offset));

        ValidateGeneration(

            generationNumber);

        return new PdfCrossReferenceStreamEntry(

            new PdfObjectId(

                objectNumber,

                generationNumber),

            PdfCrossReferenceStreamEntryKind.InUse,

            offset,

            generationNumber);
    }

    /// Creates a compressed-object entry.
    public static PdfCrossReferenceStreamEntry CreateCompressed(

        int objectNumber,

        long objectStreamNumber,

        int objectStreamIndex)

    {

        ValidateNonNegative(

            objectStreamNumber,

            nameof(objectStreamNumber));

        if (objectStreamNumber > int.MaxValue)

        {
            throw new ArgumentOutOfRangeException(

                nameof(objectStreamNumber),

                objectStreamNumber,

                "An object-stream number cannot exceed Int32.MaxValue.");
        }

        ArgumentOutOfRangeException.ThrowIfNegative(
            objectStreamIndex,
            nameof(objectStreamIndex));

        return new PdfCrossReferenceStreamEntry(

            new PdfObjectId(

                objectNumber,

                generationNumber: 0),

            PdfCrossReferenceStreamEntryKind.Compressed,

            objectStreamNumber,

            objectStreamIndex);

    }

    private static void ValidateGeneration(

        int generationNumber)

    {

        if (generationNumber is < 0 or > 65535)

        {

            throw new ArgumentOutOfRangeException(

                nameof(generationNumber));

        }

    }

    private static void ValidateNonNegative(

        long value,

        string parameterName)

    {

        if (value < 0)

        {

            throw new ArgumentOutOfRangeException(

                parameterName);

        }

    }

}


