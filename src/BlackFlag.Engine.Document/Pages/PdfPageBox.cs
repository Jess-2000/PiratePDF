namespace BlackFlag.Engine.Document.Pages;

/// <summary>Represents a normalized PDF page rectangle in user-space units.</summary>
public readonly record struct PdfPageBox
{
    /// <summary>Initializes a normalized page rectangle.</summary>
    public PdfPageBox(double left, double bottom, double right, double top)
    {
        if (!double.IsFinite(left) || !double.IsFinite(bottom)
            || !double.IsFinite(right) || !double.IsFinite(top))
        {
            throw new ArgumentOutOfRangeException(
                nameof(left),
                "Page-box coordinates must be finite.");
        }

        if (right <= left || top <= bottom)
        {
            throw new ArgumentException(
                "A page box must have positive width and height.");
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
