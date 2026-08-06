namespace BlackFlag.Engine.Document.Pages;

/// <summary>Provides effective, prepress-ready geometry for one PDF page.</summary>
public sealed class PdfPageGeometry
{
    public PdfPageGeometry(
        PdfEffectiveValue<PdfPageBox> mediaBox,
        PdfEffectiveValue<PdfPageBox> cropBox,
        PdfEffectiveValue<PdfPageBox> bleedBox,
        PdfEffectiveValue<PdfPageBox> trimBox,
        PdfEffectiveValue<PdfPageBox> artBox,
        PdfEffectiveValue<int> rotation,
        PdfEffectiveValue<double> userUnit)
    {
        ValidateRotation(rotation.Value);
        if (!double.IsFinite(userUnit.Value) || userUnit.Value <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(userUnit),
                "The effective /UserUnit must be finite and greater than zero.");
        }

        MediaBox = mediaBox;
        CropBox = cropBox;
        BleedBox = bleedBox;
        TrimBox = trimBox;
        ArtBox = artBox;
        Rotation = rotation;
        UserUnit = userUnit;
    }

    public PdfEffectiveValue<PdfPageBox> MediaBox { get; }
    public PdfEffectiveValue<PdfPageBox> CropBox { get; }
    public PdfEffectiveValue<PdfPageBox> BleedBox { get; }
    public PdfEffectiveValue<PdfPageBox> TrimBox { get; }
    public PdfEffectiveValue<PdfPageBox> ArtBox { get; }
    public PdfEffectiveValue<int> Rotation { get; }
    public PdfEffectiveValue<double> UserUnit { get; }
    public double DisplayWidth =>
        Rotation.Value is 90 or 270
            ? CropBox.Value.Height * UserUnit.Value
            : CropBox.Value.Width * UserUnit.Value;
    public double DisplayHeight =>
        Rotation.Value is 90 or 270
            ? CropBox.Value.Width * UserUnit.Value
            : CropBox.Value.Height * UserUnit.Value;

    private static void ValidateRotation(int rotation)
    {
        if (rotation % 90 != 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(rotation),
                rotation,
                "A PDF page rotation must be a multiple of 90 degrees.");
        }
    }
}
