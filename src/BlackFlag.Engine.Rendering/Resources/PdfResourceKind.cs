namespace BlackFlag.Engine.Rendering.Resources;

/// <summary>
/// Identifies a canonical PDF resource category.
/// </summary>
public enum PdfResourceKind
{
    /// <summary>A font resource.</summary>
    Font,

    /// <summary>An external object resource.</summary>
    XObject,

    /// <summary>A color-space resource.</summary>
    ColorSpace,

    /// <summary>An extended graphics-state resource.</summary>
    ExtGState,

    /// <summary>A pattern resource.</summary>
    Pattern,

    /// <summary>A shading resource.</summary>
    Shading,

    /// <summary>A marked-content properties resource.</summary>
    Properties
}
