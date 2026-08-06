namespace BlackFlag.Engine.Document.Objects.Primitives;

/// <summary>
/// Represents a PDF boolean object.
/// </summary>
public sealed class PdfBoolean :
    PdfObject,
    IEquatable<PdfBoolean>
{
    public PdfBoolean(bool value)
        : base(PdfObjectKind.LogicalValue)
    {
        Value = value;
    }

    public bool Value { get; }

    public bool Equals(PdfBoolean? other)
    {
        return other is not null &&
               Value == other.Value;
    }

    public override bool Equals(object? obj)
    {
        return obj is PdfBoolean other &&
               Equals(other);
    }

    public override int GetHashCode()
    {
        return Value.GetHashCode();
    }

    public override string ToString()
    {
        return Value
            ? "true"
            : "false";
    }

    public static bool operator ==(
        PdfBoolean? left,
        PdfBoolean? right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(
        PdfBoolean? left,
        PdfBoolean? right)
    {
        return !Equals(left, right);
    }
}
