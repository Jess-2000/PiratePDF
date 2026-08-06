using System.Globalization;

namespace BlackFlag.Engine.Core.Objects;

/// <summary>
/// Represents a PDF real-number object.
/// </summary>
public sealed record PdfReal(double Value) : PdfObject
{
    /// <inheritdoc />
    public override PdfObjectKind Kind => PdfObjectKind.RealNumber;

    /// <inheritdoc />
    public override string ToString()
    {
        return Value.ToString(
            "G17",
            CultureInfo.InvariantCulture);
    }
}
