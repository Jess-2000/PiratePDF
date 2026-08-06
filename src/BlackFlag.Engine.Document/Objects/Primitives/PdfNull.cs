namespace BlackFlag.Engine.Document.Objects.Primitives;

/// <summary>
/// Represents the PDF null object.
/// </summary>
public sealed class PdfNull : PdfObject
{
    private PdfNull()
        : base(PdfObjectKind.NullValue)
    {
    }

    /// <summary>
    /// Gets the single PDF null instance.
    /// </summary>
    public static PdfNull Instance { get; } =
        new();

    public override string ToString()
    {
        return "null";
    }
}
