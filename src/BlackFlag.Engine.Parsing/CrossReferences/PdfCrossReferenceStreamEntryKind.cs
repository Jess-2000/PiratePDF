namespace BlackFlag.Engine.Parsing.CrossReferences;

/// <summary>
/// Identifies the meaning of a decoded cross-reference stream entry.
/// </summary>
public enum PdfCrossReferenceStreamEntryKind
{
    /// <summary>
    /// The object is free.
    /// </summary>
    Free = 0,

    /// <summary>
    /// The object is stored directly in the PDF source.
    /// </summary>
    InUse = 1,

    /// <summary>
    /// The object is stored inside an object stream.
    /// </summary>
    Compressed = 2
}

