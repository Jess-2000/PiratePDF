namespace BlackFlag.Engine.Rendering.Resources;

/// <summary>
/// Describes known font-embedding permission information.
/// </summary>
public enum PdfFontEmbeddingRights
{
    /// <summary>The font program has not provided usable permission metadata.</summary>
    Unknown,

    /// <summary>Embedding is permitted.</summary>
    Allowed,

    /// <summary>Only subset embedding is permitted.</summary>
    SubsetOnly,

    /// <summary>Embedding is restricted.</summary>
    Restricted
}
