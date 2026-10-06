namespace BlackFlag.Engine.Fonts;

/// <summary>Describes the font-program technology supplied by an external face.</summary>
public enum PdfExternalFontTechnology
{
    Unknown = 0,
    TrueType = 1,
    OpenType = 2,
    Cff = 3,
    Type1 = 4
}
