namespace BlackFlag.Engine.Rendering.DisplayList;

/// <summary>Identifies the geometry represented by an immutable clipping node.</summary>
public enum PdfClipKind
{
    /// <summary>The clip is defined by path geometry.</summary>
    Path,

    /// <summary>The clip is defined by accumulated text glyph outlines.</summary>
    Text
}
