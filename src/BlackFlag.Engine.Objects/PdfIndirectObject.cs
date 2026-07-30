namespace BlackFlag.Engine.Objects;

/// <summary>
/// Associates a PDF object with an indirect object number and generation.
/// </summary>
/// <remarks>
/// This type represents the document-level <c>obj</c>/<c>endobj</c> wrapper.
/// It is not itself a <see cref="PdfObject"/>.
/// </remarks>
public sealed class PdfIndirectObject :
    IEquatable<PdfIndirectObject>
{
    /// <summary>
    /// Initializes a new PDF indirect object.
    /// </summary>
    /// <param name="objectNumber">
    /// The positive object number identifying this indirect object.
    /// </param>
    /// <param name="generationNumber">
    /// The generation number in the inclusive range from 0 through 65535.
    /// </param>
    /// <param name="value">The PDF object stored by this indirect object.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="objectNumber"/> is less than 1, or
    /// <paramref name="generationNumber"/> is outside the inclusive range
    /// from 0 through 65535.
    /// </exception>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="value"/> is <see langword="null"/>.
    /// </exception>
    public PdfIndirectObject(
        int objectNumber,
        int generationNumber,
        PdfObject value)
    {
        ValidateObjectNumber(objectNumber);
        ValidateGenerationNumber(generationNumber);
        ArgumentNullException.ThrowIfNull(value);

        ObjectNumber = objectNumber;
        GenerationNumber = generationNumber;
        Value = value;
    }

    /// <summary>
    /// Gets the object number identifying this indirect object.
    /// </summary>
    public int ObjectNumber { get; }

    /// <summary>
    /// Gets the generation number of this indirect object.
    /// </summary>
    public int GenerationNumber { get; }

    /// <summary>
    /// Gets the PDF object stored by this indirect object.
    /// </summary>
    public PdfObject Value { get; }

    /// <inheritdoc />
    public bool Equals(PdfIndirectObject? other) =>
        other is not null
        && ObjectNumber == other.ObjectNumber
        && GenerationNumber == other.GenerationNumber
        && Value.Equals(other.Value);

    /// <inheritdoc />
    public override bool Equals(object? obj) =>
        obj is PdfIndirectObject other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode() =>
        HashCode.Combine(ObjectNumber, GenerationNumber, Value);

    /// <summary>
    /// Determines whether two PDF indirect objects are equal.
    /// </summary>
    /// <param name="left">The first indirect object to compare.</param>
    /// <param name="right">The second indirect object to compare.</param>
    /// <returns>
    /// <see langword="true"/> when the indirect objects are equal; otherwise,
    /// <see langword="false"/>.
    /// </returns>
    public static bool operator ==(
        PdfIndirectObject? left,
        PdfIndirectObject? right) =>
        Equals(left, right);

    /// <summary>
    /// Determines whether two PDF indirect objects are unequal.
    /// </summary>
    /// <param name="left">The first indirect object to compare.</param>
    /// <param name="right">The second indirect object to compare.</param>
    /// <returns>
    /// <see langword="true"/> when the indirect objects are unequal; otherwise,
    /// <see langword="false"/>.
    /// </returns>
    public static bool operator !=(
        PdfIndirectObject? left,
        PdfIndirectObject? right) =>
        !Equals(left, right);

    private static void ValidateObjectNumber(int objectNumber)
    {
        if (objectNumber < 1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(objectNumber),
                objectNumber,
                "A PDF indirect object number must be at least 1.");
        }
    }

    private static void ValidateGenerationNumber(int generationNumber)
    {
        if (generationNumber is < 0 or > 65535)
        {
            throw new ArgumentOutOfRangeException(
                nameof(generationNumber),
                generationNumber,
                "A PDF generation number must be between 0 and 65535.");
        }
    }
}
