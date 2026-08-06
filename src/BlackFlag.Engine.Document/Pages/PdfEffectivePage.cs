using BlackFlag.Engine.Objects;

namespace BlackFlag.Engine.Document.Pages;

/// <summary>Represents an immutable page loaded from a PDF page tree.</summary>
public sealed class PdfEffectivePage
{
    public PdfEffectivePage(
        int objectNumber,
        int generationNumber,
        PdfDictionary dictionary,
        PdfPageGeometry geometry,
        PdfEffectiveValue<PdfDictionary> resources)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(objectNumber);
        ArgumentOutOfRangeException.ThrowIfNegative(generationNumber);
        ArgumentNullException.ThrowIfNull(dictionary);
        ArgumentNullException.ThrowIfNull(geometry);
        ArgumentNullException.ThrowIfNull(resources.Value);

        ObjectNumber = objectNumber;
        GenerationNumber = generationNumber;
        Dictionary = dictionary;
        Geometry = geometry;
        Resources = resources;
    }

    public int ObjectNumber { get; }
    public int GenerationNumber { get; }
    public PdfDictionary Dictionary { get; }
    public PdfPageGeometry Geometry { get; }
    public PdfEffectiveValue<PdfDictionary> Resources { get; }
}
