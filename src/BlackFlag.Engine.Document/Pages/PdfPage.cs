namespace BlackFlag.Engine.Document.Pages;

/// <summary>
/// Represents a high-level page in a PDF document.
/// </summary>
public sealed class PdfPage
{
    public PdfPage(
        double width,
        double height,
        PdfPageRotation rotation = PdfPageRotation.None)
    {
        ValidateDimension(
            width,
            nameof(width));

        ValidateDimension(
            height,
            nameof(height));

        ValidateRotation(rotation);

        Id = Guid.NewGuid();
        Width = width;
        Height = height;
        Rotation = rotation;
    }

    public Guid Id { get; }

    public double Width { get; }

    public double Height { get; }

    public PdfPageRotation Rotation { get; private set; }

    public bool IsLandscape
    {
        get
        {
            var swapsDimensions =
                Rotation is
                    PdfPageRotation.Clockwise90 or
                    PdfPageRotation.Clockwise270;

            return swapsDimensions
                ? Height > Width
                : Width > Height;
        }
    }

    public void SetRotation(
        PdfPageRotation rotation)
    {
        ValidateRotation(rotation);
        Rotation = rotation;
    }

    private static void ValidateDimension(
        double value,
        string parameterName)
    {
        if (
            !double.IsFinite(value) ||
            value <= 0)
        {
            throw new ArgumentOutOfRangeException(
                parameterName,
                value,
                "A PDF page dimension must be finite and greater than zero.");
        }
    }

    private static void ValidateRotation(
        PdfPageRotation rotation)
    {
        if (!Enum.IsDefined(rotation))
        {
            throw new ArgumentOutOfRangeException(
                nameof(rotation),
                rotation,
                "The PDF page rotation is not supported.");
        }
    }
}