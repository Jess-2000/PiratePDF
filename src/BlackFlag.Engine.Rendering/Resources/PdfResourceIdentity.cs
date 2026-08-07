namespace BlackFlag.Engine.Rendering.Resources;

/// <summary>
/// Identifies the underlying PDF object represented by a resolved resource.
/// </summary>
public readonly record struct PdfResourceIdentity
{
    private PdfResourceIdentity(
        int? objectNumber,
        int? generationNumber,
        long localId)
    {
        ObjectNumber = objectNumber;
        GenerationNumber = generationNumber;
        LocalId = localId;
    }

    /// <summary>
    /// Gets the indirect object number when the resource originates from an
    /// indirect PDF object.
    /// </summary>
    public int? ObjectNumber { get; }

    /// <summary>
    /// Gets the generation number when the resource originates from an
    /// indirect PDF object.
    /// </summary>
    public int? GenerationNumber { get; }

    /// <summary>
    /// Gets the process-local identifier used for a direct PDF resource.
    /// </summary>
    public long LocalId { get; }

    /// <summary>
    /// Gets a value indicating whether the identity represents an indirect
    /// PDF object.
    /// </summary>
    public bool IsIndirect => ObjectNumber.HasValue;

    /// <summary>
    /// Creates an identity for an indirect PDF object.
    /// </summary>
    public static PdfResourceIdentity ForIndirect(
        int objectNumber,
        int generationNumber)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(objectNumber);
        ArgumentOutOfRangeException.ThrowIfNegative(generationNumber);

        return new PdfResourceIdentity(
            objectNumber,
            generationNumber,
            localId: 0);
    }

    /// <summary>
    /// Creates an identity for a direct resource that has no PDF object
    /// number.
    /// </summary>
    public static PdfResourceIdentity ForLocal(long localId)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(localId);
        return new PdfResourceIdentity(null, null, localId);
    }
}
