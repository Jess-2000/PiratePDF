namespace BlackFlag.Engine.Rendering.DisplayList;

/// <summary>
/// Represents conservative page-space bounds for a display-list item.
/// </summary>
public readonly record struct PdfDisplayBounds
{
    /// <summary>Initializes page-space bounds.</summary>
    public PdfDisplayBounds(
        double left,
        double bottom,
        double right,
        double top)
    {
        Left = left;
        Bottom = bottom;
        Right = right;
        Top = top;
    }

    /// <summary>Gets the left coordinate.</summary>
    public double Left { get; }

    /// <summary>Gets the bottom coordinate.</summary>
    public double Bottom { get; }

    /// <summary>Gets the right coordinate.</summary>
    public double Right { get; }

    /// <summary>Gets the top coordinate.</summary>
    public double Top { get; }

    /// <summary>Creates bounds from two arbitrary corners.</summary>
    public static PdfDisplayBounds FromCorners(PdfPoint first, PdfPoint second) =>
        new(
            Math.Min(first.X, second.X),
            Math.Min(first.Y, second.Y),
            Math.Max(first.X, second.X),
            Math.Max(first.Y, second.Y));

    /// <summary>Gets the width of the bounds.</summary>
    public double Width => Right - Left;

    /// <summary>Gets the height of the bounds.</summary>
    public double Height => Top - Bottom;

    /// <summary>Returns bounds that include another set of bounds.</summary>
    public PdfDisplayBounds Union(PdfDisplayBounds other) =>
        new(
            Math.Min(Left, other.Left),
            Math.Min(Bottom, other.Bottom),
            Math.Max(Right, other.Right),
            Math.Max(Top, other.Top));
}
