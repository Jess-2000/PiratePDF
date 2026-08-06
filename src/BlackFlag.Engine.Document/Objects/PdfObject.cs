namespace BlackFlag.Engine.Document.Objects;

/// <summary>
/// Base type for every object in the BlackFlag PDF document model.
/// </summary>
public abstract class PdfObject
{
    protected PdfObject(PdfObjectKind kind)
    {
        Kind = kind;
    }

    /// <summary>
    /// Gets the semantic kind of this object.
    /// </summary>
    public PdfObjectKind Kind { get; }
}