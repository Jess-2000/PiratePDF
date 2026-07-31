namespace BlackFlag.Engine.Parsing.CrossReferences;

/// <summary>
/// Identifies an entry in a PDF cross-reference structure.
/// </summary>
public readonly record struct PdfObjectId
{
    /// <summary>
    /// Initializes a cross-reference object identifier.
    /// </summary>
    /// <param name="objectNumber">The zero-based object number.</param>
    /// <param name="generationNumber">The generation number.</param>
    public PdfObjectId(
        int objectNumber,
        int generationNumber)
    {
        if (objectNumber < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(objectNumber),
                objectNumber,
                "A cross-reference object number cannot be negative.");
        }

        if (generationNumber is < 0 or > 65535)
        {
            throw new ArgumentOutOfRangeException(
                nameof(generationNumber),
                generationNumber,
                "A PDF generation number must be between 0 and 65535.");
        }

        ObjectNumber = objectNumber;
        GenerationNumber = generationNumber;
    }

    /// <summary>
    /// Gets the object number.
    /// </summary>
    public int ObjectNumber { get; }

    /// <summary>
    /// Gets the generation number.
    /// </summary>
    public int GenerationNumber { get; }

    /// <inheritdoc />
    public override string ToString()
    {
        return string.Concat(
            ObjectNumber,
            " ",
            GenerationNumber);
    }
}
