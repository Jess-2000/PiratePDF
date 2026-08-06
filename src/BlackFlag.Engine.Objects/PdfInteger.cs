namespace BlackFlag.Engine.Objects;

/// <summary>
/// Represents a PDF integer-number object.
/// </summary>
public sealed class PdfInteger : PdfObject, IEquatable<PdfInteger>
{
    /// <summary>
    /// Initializes a new PDF integer-number object.
    /// </summary>
    /// <param name="value">The integer value.</param>
    public PdfInteger(long value)
    {
        Value = value;
    }

    /// <summary>
    /// Gets the integer value.
    /// </summary>
    public long Value { get; }

    /// <inheritdoc />
    public override PdfObjectType Type => PdfObjectType.IntegerNumber;

    /// <inheritdoc />
    public override TResult Accept<TResult>(PdfObjectVisitor<TResult> visitor)
    {
        ArgumentNullException.ThrowIfNull(visitor);
        return visitor.Visit(this);
    }

    /// <inheritdoc />
    public bool Equals(PdfInteger? other) =>
        other is not null && Value == other.Value;

    /// <inheritdoc />
    public override bool Equals(object? obj) =>
        obj is PdfInteger other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode() => Value.GetHashCode();

    /// <summary>
    /// Determines whether two PDF integer objects are equal.
    /// </summary>
    public static bool operator ==(PdfInteger? left, PdfInteger? right) =>
        Equals(left, right);

    /// <summary>
    /// Determines whether two PDF integer objects are unequal.
    /// </summary>
    public static bool operator !=(PdfInteger? left, PdfInteger? right) =>
        !Equals(left, right);
}

