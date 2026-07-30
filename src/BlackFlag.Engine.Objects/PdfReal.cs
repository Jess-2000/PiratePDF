namespace BlackFlag.Engine.Objects;

/// <summary>
/// Represents a PDF real-number object.
/// </summary>
public sealed class PdfReal : PdfObject, IEquatable<PdfReal>
{
    /// <summary>
    /// Initializes a new PDF real-number object.
    /// </summary>
    /// <param name="value">The real-number value.</param>
    public PdfReal(double value)
    {
        Value = value;
    }

    /// <summary>
    /// Gets the real-number value.
    /// </summary>
    public double Value { get; }

    /// <inheritdoc />
    public override PdfObjectType Type => PdfObjectType.RealNumber;

    /// <inheritdoc />
    public override TResult Accept<TResult>(PdfObjectVisitor<TResult> visitor)
    {
        ArgumentNullException.ThrowIfNull(visitor);
        return visitor.Visit(this);
    }

    /// <inheritdoc />
    public bool Equals(PdfReal? other) =>
        other is not null && Value.Equals(other.Value);

    /// <inheritdoc />
    public override bool Equals(object? obj) =>
        obj is PdfReal other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode() => Value.GetHashCode();

    /// <summary>
    /// Determines whether two PDF real-number objects are equal.
    /// </summary>
    public static bool operator ==(PdfReal? left, PdfReal? right) =>
        Equals(left, right);

    /// <summary>
    /// Determines whether two PDF real-number objects are unequal.
    /// </summary>
    public static bool operator !=(PdfReal? left, PdfReal? right) =>
        !Equals(left, right);
}
