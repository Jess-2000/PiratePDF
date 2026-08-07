namespace BlackFlag.Engine.Rendering.Resources;

/// <summary>
/// Identifies the primary technology used by a resolved PDF font.
/// </summary>
public enum PdfFontTechnology
{
    /// <summary>The font technology could not be determined.</summary>
    Unknown,

    /// <summary>A Type 1 font.</summary>
    Type1,

    /// <summary>A TrueType font.</summary>
    TrueType,

    /// <summary>An OpenType font program.</summary>
    OpenType,

    /// <summary>A CFF or CID-keyed CFF font.</summary>
    Cff,

    /// <summary>A Type 0 composite font.</summary>
    Composite,

    /// <summary>A PDF Type 3 font.</summary>
    Type3
}
