namespace BlackFlag.Engine.Core.Objects;

/// <summary>
/// Represents the base type for every logical PDF object.
/// </summary>
public abstract record PdfObject
{
    /// <summary>
    /// Gets the object's logical PDF kind.
    /// </summary>
    public abstract PdfObjectKind Kind { get; }
}
