using System.Globalization;

namespace BlackFlag.Engine.Document.Objects;

/// <summary>
/// Identifies an indirect PDF object by object number and generation number.
/// </summary>
public readonly record struct PdfObjectIdentifier
{
    public PdfObjectIdentifier(
        int objectNumber,
        int generationNumber = 0)
    {
        if (objectNumber <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(objectNumber),
                objectNumber,
                "A PDF object number must be greater than zero.");
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

    public int ObjectNumber { get; }

    public int GenerationNumber { get; }

    public override string ToString()
    {
        return string.Create(
            CultureInfo.InvariantCulture,
            $"{ObjectNumber} {GenerationNumber}");
    }
}
