using BlackFlag.Engine.Objects;

namespace BlackFlag.Engine.Rendering.Resources;

/// <summary>
/// Represents canonical metadata for a resolved PDF font resource.
/// </summary>
public sealed class PdfResolvedFont : PdfResolvedResource
{
    internal PdfResolvedFont(
        PdfResourceIdentity identity,
        PdfDictionary dictionary,
        string subtype,
        string familyName,
        string faceName,
        int weight,
        int stretch,
        PdfFontSlant slant,
        PdfFontTechnology technology,
        PdfFontEmbeddingRights embeddingPermission,
        bool isSubset,
        PdfObject? encoding,
        PdfStream? toUnicode,
        PdfDictionary? descriptor,
        PdfArray? descendantFonts,
        PdfStream? embeddedFontProgram,
        IReadOnlyList<PdfFontVariationAxis> variationAxes)
        : base(PdfResourceKind.Font, identity, dictionary)
    {
        Dictionary = dictionary;
        Subtype = subtype;
        FamilyName = familyName;
        FaceName = faceName;
        Weight = weight;
        Stretch = stretch;
        Slant = slant;
        Technology = technology;
        EmbeddingPermission = embeddingPermission;
        IsSubset = isSubset;
        Encoding = encoding;
        ToUnicode = toUnicode;
        Descriptor = descriptor;
        DescendantFonts = descendantFonts;
        EmbeddedFontProgram = embeddedFontProgram;
        VariationAxes = variationAxes.ToArray();
    }

    /// <summary>Gets the source font dictionary.</summary>
    public PdfDictionary Dictionary { get; }

    /// <summary>Gets the PDF font subtype name.</summary>
    public string Subtype { get; }

    /// <summary>Gets the normalized font family name.</summary>
    public string FamilyName { get; }

    /// <summary>Gets the normalized face name without a PDF subset prefix.</summary>
    public string FaceName { get; }

    /// <summary>
    /// Gets the canonical font weight on the OpenType-compatible 1 through
    /// 1000 scale.
    /// </summary>
    public int Weight { get; }

    /// <summary>
    /// Gets the canonical font stretch value from 1 (ultra-condensed) through
    /// 9 (ultra-expanded), with 5 representing normal width.
    /// </summary>
    public int Stretch { get; }

    /// <summary>Gets the canonical font slant.</summary>
    public PdfFontSlant Slant { get; }

    /// <summary>Gets the detected font technology.</summary>
    public PdfFontTechnology Technology { get; }

    /// <summary>Gets known font-embedding permission information.</summary>
    public PdfFontEmbeddingRights EmbeddingPermission { get; }

    /// <summary>Gets a value indicating whether the PDF embeds a subset font.</summary>
    public bool IsSubset { get; }

    /// <summary>Gets the source encoding object when one is present.</summary>
    public PdfObject? Encoding { get; }

    /// <summary>Gets the resolved ToUnicode CMap stream when one is present.</summary>
    public PdfStream? ToUnicode { get; }

    /// <summary>Gets the resolved font descriptor when one is present.</summary>
    public PdfDictionary? Descriptor { get; }

    /// <summary>Gets the Type 0 descendant-font array when one is present.</summary>
    public PdfArray? DescendantFonts { get; }

    /// <summary>
    /// Gets the embedded font-program stream when one is present. The stream
    /// remains encoded according to its PDF filter chain.
    /// </summary>
    public PdfStream? EmbeddedFontProgram { get; }

    /// <summary>
    /// Gets OpenType variable-font axes discovered from the font program.
    /// BF-1046 preserves the contract while deeper font-program table parsing
    /// remains lazy for a later font subsystem.
    /// </summary>
    public IReadOnlyList<PdfFontVariationAxis> VariationAxes { get; }

    /// <summary>
    /// Gets a value indicating whether the PDF font supplies a ToUnicode CMap.
    /// </summary>
    public bool HasToUnicodeMap => ToUnicode is not null;
}

