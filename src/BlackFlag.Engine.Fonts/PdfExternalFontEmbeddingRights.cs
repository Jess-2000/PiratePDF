namespace BlackFlag.Engine.Fonts;

/// <summary>Describes a base program permission or a provider embedding policy.</summary>
public enum PdfExternalFontEmbeddingRights
{
    Unknown = 0,
    /// <summary>Installable program embedding, or general provider permission; independent restrictions still apply.</summary>
    Allowed = 1,
    /// <summary>A provider policy requiring a subset; not the OpenType editable-embedding bit.</summary>
    SubsetOnly = 2,
    Restricted = 3,
    PreviewAndPrint = 4,
    Editable = 5
}
