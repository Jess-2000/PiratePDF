namespace BlackFlag.Engine.Document.Decorations;

/// <summary>Identifies the logical content represented by a page decoration.</summary>
public enum PdfPageDecorationKind
{
    /// <summary>A literal text header or footer.</summary>
    Text = 0,

    /// <summary>A generated page number.</summary>
    PageNumber = 1
}
