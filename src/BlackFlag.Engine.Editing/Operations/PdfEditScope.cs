namespace BlackFlag.Engine.Editing.Operations;

/// <summary>
/// Identifies the logical document area targeted by an editing operation.
/// </summary>
public enum PdfEditScope
{
    Document = 0,
    Page = 1,
    Content = 2,
    Resource = 3
}
