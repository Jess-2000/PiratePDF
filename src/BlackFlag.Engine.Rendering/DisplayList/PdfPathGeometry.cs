namespace BlackFlag.Engine.Rendering.DisplayList;

/// <summary>Represents an immutable canonical path geometry.</summary>
public sealed class PdfPathGeometry
{
    private readonly PdfPathSegment[] segments;

    /// <summary>Initializes immutable path geometry.</summary>
    public PdfPathGeometry(IEnumerable<PdfPathSegment> segments)
    {
        ArgumentNullException.ThrowIfNull(segments);
        this.segments = segments.ToArray();
    }

    /// <summary>Gets path segments in source order.</summary>
    public IReadOnlyList<PdfPathSegment> Segments => segments;

    /// <summary>Gets conservative bounds over all segment points.</summary>
    public PdfDisplayBounds? GetBounds()
    {
        if (segments.Length == 0)
        {
            return null;
        }

        bool hasPoint = false;
        double left = 0;
        double right = 0;
        double bottom = 0;
        double top = 0;

        foreach (PdfPathSegment segment in segments)
        {
            Include(segment.EndPoint);
            if (segment.ControlPoint1 is PdfPoint control1)
            {
                Include(control1);
            }

            if (segment.ControlPoint2 is PdfPoint control2)
            {
                Include(control2);
            }
        }

        return hasPoint
            ? new PdfDisplayBounds(left, bottom, right, top)
            : null;

        void Include(PdfPoint point)
        {
            if (!hasPoint)
            {
                left = right = point.X;
                bottom = top = point.Y;
                hasPoint = true;
                return;
            }

            left = Math.Min(left, point.X);
            right = Math.Max(right, point.X);
            bottom = Math.Min(bottom, point.Y);
            top = Math.Max(top, point.Y);
        }
    }
}
