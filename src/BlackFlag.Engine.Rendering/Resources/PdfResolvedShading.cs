using BlackFlag.Engine.Objects;

namespace BlackFlag.Engine.Rendering.Resources;

/// <summary>
/// Represents a resolved PDF shading resource.
/// </summary>
public sealed class PdfResolvedShading : PdfResolvedResource
{
    internal PdfResolvedShading(
        PdfResourceIdentity identity,
        PdfObject source,
        int shadingType,
        PdfObject? colorSpace,
        PdfObject? function)
        : base(PdfResourceKind.Shading, identity, source)
    {
        ShadingType = shadingType;
        ColorSpace = colorSpace;
        Function = function;
    }

    /// <summary>Gets the PDF ShadingType value.</summary>
    public int ShadingType { get; }

    /// <summary>Gets the source color-space object when present.</summary>
    public PdfObject? ColorSpace { get; }

    /// <summary>Gets the source function object when present.</summary>
    public PdfObject? Function { get; }
}
