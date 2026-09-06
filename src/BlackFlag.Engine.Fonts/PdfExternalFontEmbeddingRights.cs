namespace BlackFlag.Engine.Fonts;

/// <summary>Describes embedding permission derived from an external font program.</summary>
public enum PdfExternalFontEmbeddingRights
{
    Unknown = 0,
    Allowed = 1,
    SubsetOnly = 2,
    Restricted = 3
}
