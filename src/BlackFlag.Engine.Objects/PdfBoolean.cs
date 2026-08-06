namespace BlackFlag.Engine.Objects;

/// <summary>
/// Represents a PDF Boolean object.
/// </summary>
public sealed class PdfBoolean : PdfObject, IEquatable<PdfBoolean>
{
    /// <summary>
    /// Initializes a new PDF Boolean object.
    /// </summary>
    /// <param name="value">The Boolean value.</param>
    public PdfBoolean(bool value)
    {
        Value = value;
    }

    /// <summary>
    /// Gets the Boolean value.
    /// </summary>
    public bool Value { get; }

    /// <inheritdoc />
    public override PdfObjectType Type => PdfObjectType.Boolean;

    /// <inheritdoc />
    public override TResult Accept<TResult>(PdfObjectVisitor<TResult> visitor)
    {
        ArgumentNullException.ThrowIfNull(visitor);
        return visitor.Visit(this);
    }

    /// <inheritdoc />
    public bool Equals(PdfBoolean? other) =>
        other is not null && Value == other.Value;

    /// <inheritdoc />
    public override bool Equals(object? obj) =>
        obj is PdfBoolean other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode() => Value.GetHashCode();

    /// <summary>
    /// Determines whether two PDF Boolean objects are equal.
    /// </summary>
    public static bool operator ==(PdfBoolean? left, PdfBoolean? right) =>
        Equals(left, right);

    /// <summary>
    /// Determines whether two PDF Boolean objects are unequal.
    /// </summary>
    public static bool operator !=(PdfBoolean? left, PdfBoolean? right) =>
        !Equals(left, right);
}
