using BlackFlag.Engine.Objects;

namespace BlackFlag.Engine.Rendering.Resources;

/// <summary>
/// Represents canonical metadata for an extended graphics-state resource.
/// </summary>
public sealed class PdfResolvedExtGState : PdfResolvedResource
{
    internal PdfResolvedExtGState(
        PdfResourceIdentity identity,
        PdfDictionary dictionary,
        double? strokingAlpha,
        double? nonstrokingAlpha,
        bool? overprintStroke,
        bool? overprintFill,
        int? overprintMode,
        PdfObject? blendMode,
        PdfObject? softMask)
        : base(PdfResourceKind.ExtGState, identity, dictionary)
    {
        Dictionary = dictionary;
        StrokingAlpha = strokingAlpha;
        NonstrokingAlpha = nonstrokingAlpha;
        OverprintStroke = overprintStroke;
        OverprintFill = overprintFill;
        OverprintMode = overprintMode;
        BlendMode = blendMode;
        SoftMask = softMask;
    }

    /// <summary>Gets the source graphics-state dictionary.</summary>
    public PdfDictionary Dictionary { get; }

    /// <summary>Gets the stroking alpha constant when present.</summary>
    public double? StrokingAlpha { get; }

    /// <summary>Gets the nonstroking alpha constant when present.</summary>
    public double? NonstrokingAlpha { get; }

    /// <summary>Gets the stroking overprint flag when present.</summary>
    public bool? OverprintStroke { get; }

    /// <summary>Gets the nonstroking overprint flag when present.</summary>
    public bool? OverprintFill { get; }

    /// <summary>Gets the overprint mode when present.</summary>
    public int? OverprintMode { get; }

    /// <summary>Gets the blend-mode object when present.</summary>
    public PdfObject? BlendMode { get; }

    /// <summary>Gets the soft-mask object when present.</summary>
    public PdfObject? SoftMask { get; }
}
