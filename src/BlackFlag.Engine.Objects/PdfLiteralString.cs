namespace BlackFlag.Engine.Objects;

/// <summary>
/// Represents a decoded PDF literal-string object.
/// </summary>
public sealed class PdfLiteralString : PdfObject, IEquatable<PdfLiteralString>
{
    /// <summary>
    /// Initializes a new PDF literal-string object.
    /// </summary>
    /// <param name="value">The decoded string value.</param>
    public PdfLiteralString(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        Value = value;
    }

    /// <summary>
    /// Gets the decoded string value.
    /// </summary>
    public string Value { get; }

    /// <inheritdoc />
    public override PdfObjectType Type => PdfObjectType.LiteralString;

    /// <inheritdoc />
    public override TResult Accept<TResult>(PdfObjectVisitor<TResult> visitor)
    {
        ArgumentNullException.ThrowIfNull(visitor);
        return visitor.Visit(this);
    }

    /// <inheritdoc />
    public bool Equals(PdfLiteralString? other) =>
        other is not null
        && string.Equals(Value, other.Value, StringComparison.Ordinal);

    /// <inheritdoc />
    public override bool Equals(object? obj) =>
        obj is PdfLiteralString other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode() =>
        StringComparer.Ordinal.GetHashCode(Value);

    /// <summary>
    /// Determines whether two PDF literal-string objects are equal.
    /// </summary>
    public static bool operator ==(
        PdfLiteralString? left,
        PdfLiteralString? right) =>
        Equals(left, right);

    /// <summary>
    /// Determines whether two PDF literal-string objects are unequal.
    /// </summary>
    public static bool operator !=(
        PdfLiteralString? left,
        PdfLiteralString? right) =>
        !Equals(left, right);
}
