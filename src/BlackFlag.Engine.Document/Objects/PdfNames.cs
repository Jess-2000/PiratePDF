using BlackFlag.Engine.Document.Objects.Primitives;

namespace BlackFlag.Engine.Document.Objects;

/// <summary>
/// Provides commonly used PDF dictionary names.
/// </summary>
public static class PdfNames
{
    public static PdfName Type { get; } =
        PdfName.FromLatin1("Type");

    public static PdfName Catalog { get; } =
        PdfName.FromLatin1("Catalog");

    public static PdfName Pages { get; } =
        PdfName.FromLatin1("Pages");

    public static PdfName Page { get; } =
        PdfName.FromLatin1("Page");

    public static PdfName Kids { get; } =
        PdfName.FromLatin1("Kids");

    public static PdfName Count { get; } =
        PdfName.FromLatin1("Count");

    public static PdfName Parent { get; } =
        PdfName.FromLatin1("Parent");

    public static PdfName MediaBox { get; } =
        PdfName.FromLatin1("MediaBox");

    public static PdfName CropBox { get; } =
        PdfName.FromLatin1("CropBox");

    public static PdfName BleedBox { get; } =
        PdfName.FromLatin1("BleedBox");

    public static PdfName TrimBox { get; } =
        PdfName.FromLatin1("TrimBox");

    public static PdfName ArtBox { get; } =
        PdfName.FromLatin1("ArtBox");

    public static PdfName Resources { get; } =
        PdfName.FromLatin1("Resources");

    public static PdfName Contents { get; } =
        PdfName.FromLatin1("Contents");

    public static PdfName Language { get; } =
        PdfName.FromLatin1("Lang");

    public static PdfName Rotate { get; } =
        PdfName.FromLatin1("Rotate");

    public static PdfName Length { get; } =
        PdfName.FromLatin1("Length");

    public static PdfName Filter { get; } =
        PdfName.FromLatin1("Filter");

    public static PdfName Root { get; } =
        PdfName.FromLatin1("Root");

    public static PdfName Info { get; } =
        PdfName.FromLatin1("Info");

    public static PdfName Encrypt { get; } =
        PdfName.FromLatin1("Encrypt");

    public static PdfName Size { get; } =
        PdfName.FromLatin1("Size");

    public static PdfName Previous { get; } =
        PdfName.FromLatin1("Prev");
}