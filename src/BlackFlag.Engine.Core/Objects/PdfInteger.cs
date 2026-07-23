namespace BlackFlag.Engine.Core.Objects;

/// <summary>
/// Represents a PDF integer object.
/// </summary>
public sealed record PdfInteger(long Value) : PdfObject
{
    /// <inheritdoc />
    public override PdfObjectKind Kind => PdfObjectKind.IntegerNumber;

    /// <inheritdoc />
    public override string ToString()
    {
        return Value.ToString(
            System.Globalization.CultureInfo.InvariantCulture);
    }
}