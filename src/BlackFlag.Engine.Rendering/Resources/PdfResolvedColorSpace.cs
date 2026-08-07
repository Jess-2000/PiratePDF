using BlackFlag.Engine.Objects;

namespace BlackFlag.Engine.Rendering.Resources;

/// <summary>
/// Represents canonical metadata for a resolved PDF color space.
/// </summary>
public sealed class PdfResolvedColorSpace : PdfResolvedResource
{
    internal PdfResolvedColorSpace(
        PdfResourceIdentity identity,
        PdfObject source,
        PdfColorSpaceKind colorSpaceKind,
        int componentCount,
        IReadOnlyList<string> colorantNames,
        PdfStream? iccProfile,
        PdfObject? alternateSpace,
        PdfObject? tintTransform)
        : base(PdfResourceKind.ColorSpace, identity, source)
    {
        ColorSpaceKind = colorSpaceKind;
        ComponentCount = componentCount;
        ColorantNames = colorantNames.ToArray();
        IccProfile = iccProfile;
        AlternateSpace = alternateSpace;
        TintTransform = tintTransform;
    }

    /// <summary>Gets the canonical color-space family.</summary>
    public PdfColorSpaceKind ColorSpaceKind { get; }

    /// <summary>Gets the number of color components when it is known.</summary>
    public int ComponentCount { get; }

    /// <summary>Gets spot or DeviceN colorant names.</summary>
    public IReadOnlyList<string> ColorantNames { get; }

    /// <summary>Gets the resolved ICC profile stream for ICCBased spaces.</summary>
    public PdfStream? IccProfile { get; }

    /// <summary>Gets the source alternate color-space object when applicable.</summary>
    public PdfObject? AlternateSpace { get; }

    /// <summary>Gets the source tint-transform function when applicable.</summary>
    public PdfObject? TintTransform { get; }
}
