namespace BlackFlag.Engine.Document.Annotations;

/// <summary>Represents a normalized annotation rectangle in PDF user space.</summary>
public readonly record struct PdfAnnotationRectangle
{
    public PdfAnnotationRectangle(
        double left,
        double bottom,
        double right,
        double top)
    {
        if (!double.IsFinite(left) ||
            !double.IsFinite(bottom) ||
            !double.IsFinite(right) ||
            !double.IsFinite(top))
        {
            throw new ArgumentOutOfRangeException(
                nameof(left),
                "Annotation rectangle coordinates must be finite.");
        }

        if (right <= left || top <= bottom)
        {
            throw new ArgumentException(
                "An annotation rectangle must have positive width and height.");
        }

        Left = left;
        Bottom = bottom;
        Right = right;
        Top = top;
    }

    public double Left { get; }
    public double Bottom { get; }
    public double Right { get; }
    public double Top { get; }
    public double Width => Right - Left;
    public double Height => Top - Bottom;
}
