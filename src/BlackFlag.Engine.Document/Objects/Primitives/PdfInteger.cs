using System.Globalization;

namespace BlackFlag.Engine.Document.Objects.Primitives;

/// <summary>
/// Represents a PDF integer object.
/// </summary>
public sealed class PdfInteger :
    PdfObject,
    IEquatable<PdfInteger>
{
    public PdfInteger(long value)
        : base(PdfObjectKind.WholeNumber)
    {
        Value = value;
    }

    public long Value { get; }

    public bool Equals(PdfInteger? other)
    {
        return other is not null &&
               Value == other.Value;
    }

    public override bool Equals(object? obj)
    {
        return obj is PdfInteger other &&
               Equals(other);
    }

    public override int GetHashCode()
    {
        return Value.GetHashCode();
    }

    public override string ToString()
    {
        return Value.ToString(
            CultureInfo.InvariantCulture);
    }

    public static bool operator ==(
        PdfInteger? left,
        PdfInteger? right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(
        PdfInteger? left,
        PdfInteger? right)
    {
        return !Equals(left, right);
    }
}
