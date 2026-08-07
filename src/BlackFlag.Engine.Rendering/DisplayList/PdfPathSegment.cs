namespace BlackFlag.Engine.Rendering.DisplayList;

/// <summary>Represents one immutable segment in a canonical PDF path.</summary>
public sealed class PdfPathSegment
{
    /// <summary>Initializes a path segment.</summary>
    public PdfPathSegment(
        PdfPathSegmentKind kind,
        PdfPoint endPoint,
        PdfPoint? controlPoint1 = null,
        PdfPoint? controlPoint2 = null)
    {
        Kind = kind;
        EndPoint = endPoint;
        ControlPoint1 = controlPoint1;
        ControlPoint2 = controlPoint2;
    }

    /// <summary>Gets the segment kind.</summary>
    public PdfPathSegmentKind Kind { get; }

    /// <summary>Gets the segment end point.</summary>
    public PdfPoint EndPoint { get; }

    /// <summary>Gets the first Bézier control point when applicable.</summary>
    public PdfPoint? ControlPoint1 { get; }

    /// <summary>Gets the second Bézier control point when applicable.</summary>
    public PdfPoint? ControlPoint2 { get; }
}
