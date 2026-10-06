namespace BlackFlag.Engine.Editing.Text;

/// <summary>
/// Identifies the authoritative content-source kind that produced existing
/// selectable PDF text.
/// </summary>
public enum PdfExistingTextSourceKind
{
    /// <summary>The text is defined by page-owned content.</summary>
    PageContent = 0,

    /// <summary>The text is defined by a Form XObject.</summary>
    FormXObject = 1
}
