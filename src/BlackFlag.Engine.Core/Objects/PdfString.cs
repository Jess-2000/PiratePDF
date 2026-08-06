namespace BlackFlag.Engine.Core.Objects;

/// <summary>
/// Represents the decoded bytes of a PDF string object.
/// </summary>
public sealed record PdfString : PdfObject
{
    private readonly byte[] value;

    /// <summary>
    /// Initializes a PDF string.
    /// </summary>
    public PdfString(
        ReadOnlySpan<byte> value,
        PdfStringKind stringKind)
    {
        this.value = value.ToArray();
        StringKind = stringKind;
    }

    /// <summary>
    /// Gets the source representation kind.
    /// </summary>
    public PdfStringKind StringKind { get; }

    /// <summary>
    /// Gets the decoded string bytes.
    /// </summary>
    public ReadOnlyMemory<byte> Value => value;

    /// <inheritdoc />
    public override PdfObjectKind Kind => PdfObjectKind.StringValue;

    /// <inheritdoc />
    public override string ToString()
    {
        return StringKind == PdfStringKind.Literal
            ? $"({Convert.ToHexString(value)})"
            : $"<{Convert.ToHexString(value)}>";
    }
}
