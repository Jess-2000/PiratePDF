using System.Globalization;

namespace BlackFlag.Engine.Document.Objects.Primitives;

/// <summary>
/// Represents a PDF real-number object.
/// </summary>
public sealed class PdfReal :
    PdfObject,
    IEquatable<PdfReal>
{
    public PdfReal(double value)
        : base(PdfObjectKind.RealNumber)
    {
        if (!double.IsFinite(value))
        {
            throw new ArgumentOutOfRangeException(
                nameof(value),
                value,
                "A PDF real number must be finite.");
        }

        Value = value;
    }

    public double Value { get; }

    public bool Equals(PdfReal? other)
    {
        return other is not null &&
               Value.Equals(other.Value);
    }

    public override bool Equals(object? obj)
    {
        return obj is PdfReal other &&
               Equals(other);
    }

    public override int GetHashCode()
    {
        return Value.GetHashCode();
    }

    public override string ToString()
    {
        return Value.ToString(
            "G17",
            CultureInfo.InvariantCulture);
    }

    public static bool operator ==(
        PdfReal? left,
        PdfReal? right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(
        PdfReal? left,
        PdfReal? right)
    {
        return !Equals(left, right);
    }
}