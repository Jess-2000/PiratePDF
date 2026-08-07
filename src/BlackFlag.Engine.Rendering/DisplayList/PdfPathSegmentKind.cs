namespace BlackFlag.Engine.Rendering.DisplayList;

/// <summary>Identifies one canonical path-segment kind.</summary>
public enum PdfPathSegmentKind
{
    /// <summary>Starts a new subpath.</summary>
    MoveTo,

    /// <summary>Adds a straight line.</summary>
    LineTo,

    /// <summary>Adds a cubic Bézier curve.</summary>
    CubicBezier,

    /// <summary>Closes the current subpath.</summary>
    ClosePath
}
