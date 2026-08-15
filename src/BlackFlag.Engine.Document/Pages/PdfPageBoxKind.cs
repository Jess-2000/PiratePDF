namespace BlackFlag.Engine.Document.Pages;

/// <summary>Identifies one standard PDF page boundary box.</summary>
public enum PdfPageBoxKind
{
    MediaBox = 0,
    CropBox = 1,
    BleedBox = 2,
    TrimBox = 3,
    ArtBox = 4
}
