namespace BlackFlag.Engine.Core.Objects;

/// <summary>
/// Represents a decoded PDF name object.
/// </summary>
public sealed record PdfName : PdfObject
{
    /// <summary>
    /// Initializes a PDF name.
    /// </summary>
    public PdfName(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        Value = value;
    }

    /// <summary>
    /// Gets the decoded name without the leading slash.
    /// </summary>
    public string Value { get; }

    /// <inheritdoc />
    public override PdfObjectKind Kind => PdfObjectKind.Name;

    /// <inheritdoc />
    public override string ToString()
    {
        return $"/{Value}";
    }
}