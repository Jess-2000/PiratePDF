namespace BlackFlag.Engine.Rendering.DisplayList;

/// <summary>Represents an immutable point in PDF user space.</summary>
public readonly record struct PdfPoint
{
    /// <summary>Initializes a PDF point.</summary>
    public PdfPoint(double x, double y)
    {
        X = x;
        Y = y;
    }

    /// <summary>Gets the horizontal coordinate.</summary>
    public double X { get; }

    /// <summary>Gets the vertical coordinate.</summary>
    public double Y { get; }
}
