namespace BlackFlag.Engine.Core.Objects;

/// <summary>
/// Represents the PDF null object.
/// </summary>
public sealed record PdfNull : PdfObject
{
    private PdfNull()
    {
    }

    /// <summary>
    /// Gets the shared PDF null instance.
    /// </summary>
    public static PdfNull Instance { get; } = new();

    /// <inheritdoc />
    public override PdfObjectKind Kind => PdfObjectKind.Null;

    /// <inheritdoc />
    public override string ToString()
    {
        return "null";
    }
}