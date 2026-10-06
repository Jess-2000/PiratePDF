namespace BlackFlag.Engine.Writing;

/// <summary>Identifies the physical revision strategy requested for PDF output.</summary>
public enum PdfWriteMode
{
    /// <summary>Writes a complete replacement PDF revision.</summary>
    FullRewrite = 0,

    /// <summary>Appends a new revision while preserving the preceding source revision.</summary>
    IncrementalUpdate = 1
}
