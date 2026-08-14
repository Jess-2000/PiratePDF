namespace BlackFlag.Engine.Editing.Pages;

/// <summary>
/// Identifies how an editable page entered an editing session.
/// </summary>
public enum PdfEditablePageOrigin
{
    /// <summary>
    /// The page existed in the source document when the editing session opened.
    /// </summary>
    SourceDocument = 0,

    /// <summary>
    /// The page was created by the active editing session and is not attached
    /// to the source page tree until a later mutation operation explicitly
    /// inserts it.
    /// </summary>
    SessionCreated = 1
}
