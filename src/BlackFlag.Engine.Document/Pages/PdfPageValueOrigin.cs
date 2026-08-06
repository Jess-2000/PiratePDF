namespace BlackFlag.Engine.Document.Pages;

/// <summary>Identifies how an effective page value was obtained.</summary>
public enum PdfPageValueOrigin
{
    /// <summary>The value is declared on the page dictionary.</summary>
    Explicit,
    /// <summary>The value is inherited from a page-tree ancestor.</summary>
    Inherited,
    /// <summary>The value is supplied by a PDF-defined fallback.</summary>
    Defaulted
}
