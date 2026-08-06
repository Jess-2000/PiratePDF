namespace BlackFlag.Engine.Parsing.Loading;

/// <summary>
/// Identifies how an indirect PDF object is stored.
/// </summary>
public enum PdfObjectLocationKind
{
    /// <summary>The object is free and cannot be loaded.</summary>
    Free,

    /// <summary>The object begins at an absolute source offset.</summary>
    Direct,

    /// <summary>The object is stored inside a PDF object stream.</summary>
    Compressed
}
