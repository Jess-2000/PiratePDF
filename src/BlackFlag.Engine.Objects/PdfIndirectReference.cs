namespace BlackFlag.Engine.Objects;

/// <summary>
/// Represents a PDF indirect reference.
/// </summary>
public sealed class PdfIndirectReference :
    PdfObject,
    IEquatable<PdfIndirectReference>
{
    /// <summary>
    /// Initializes a new PDF indirect reference.
    /// </summary>
    /// <param name="objectNumber">
    /// The positive object number identifying the referenced indirect object.
    /// </param>
    /// <param name="generationNumber">
    /// The generation number in the inclusive range from 0 through 65535.
    /// </param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="objectNumber"/> is less than 1, or
    /// <paramref name="generationNumber"/> is outside the inclusive range
    /// from 0 through 65535.
    /// </exception>
    public PdfIndirectReference(
        int objectNumber,
        int generationNumber)
    {
        ValidateObjectNumber(objectNumber);
        ValidateGenerationNumber(generationNumber);

        ObjectNumber = objectNumber;
        GenerationNumber = generationNumber;
    }

    /// <summary>
    /// Gets the object number identifying the referenced indirect object.
    /// </summary>
    public int ObjectNumber { get; }

    /// <summary>
    /// Gets the generation number of the referenced indirect object.
    /// </summary>
    public int GenerationNumber { get; }

    /// <inheritdoc />
    public override PdfObjectType Type => PdfObjectType.IndirectReference;

    /// <inheritdoc />
    public override TResult Accept<TResult>(PdfObjectVisitor<TResult> visitor)
    {
        ArgumentNullException.ThrowIfNull(visitor);
        return visitor.Visit(this);
    }

    /// <inheritdoc />
    public bool Equals(PdfIndirectReference? other) =>
        other is not null
        && ObjectNumber == other.ObjectNumber
        && GenerationNumber == other.GenerationNumber;

    /// <inheritdoc />
    public override bool Equals(object? obj) =>
        obj is PdfIndirectReference other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode() =>
        HashCode.Combine(ObjectNumber, GenerationNumber);

    /// <summary>
    /// Determines whether two PDF indirect references are equal.
    /// </summary>
    /// <param name="left">The first reference to compare.</param>
    /// <param name="right">The second reference to compare.</param>
    /// <returns>
    /// <see langword="true"/> when the references are equal; otherwise,
    /// <see langword="false"/>.
    /// </returns>
    public static bool operator ==(
        PdfIndirectReference? left,
        PdfIndirectReference? right) =>
        Equals(left, right);

    /// <summary>
    /// Determines whether two PDF indirect references are unequal.
    /// </summary>
    /// <param name="left">The first reference to compare.</param>
    /// <param name="right">The second reference to compare.</param>
    /// <returns>
    /// <see langword="true"/> when the references are unequal; otherwise,
    /// <see langword="false"/>.
    /// </returns>
    public static bool operator !=(
        PdfIndirectReference? left,
        PdfIndirectReference? right) =>
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
