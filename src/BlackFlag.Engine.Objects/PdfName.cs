namespace BlackFlag.Engine.Objects;

/// <summary>
/// Represents a PDF name object.
/// </summary>
public sealed class PdfName : PdfObject, IEquatable<PdfName>
{
    /// <summary>
    /// Initializes a new PDF name object.
    /// </summary>
    /// <param name="value">
    /// The decoded name value without the leading slash.
    /// </param>
    public PdfName(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        Value = value;
    }

    /// <summary>
    /// Gets the decoded name value without the leading slash.
    /// </summary>
    public string Value { get; }

    /// <inheritdoc />
    public override PdfObjectType Type => PdfObjectType.Name;

    /// <inheritdoc />
    public override TResult Accept<TResult>(PdfObjectVisitor<TResult> visitor)
    {
        ArgumentNullException.ThrowIfNull(visitor);
        return visitor.Visit(this);
    }

    /// <inheritdoc />
    public bool Equals(PdfName? other) =>
        other is not null
        && string.Equals(Value, other.Value, StringComparison.Ordinal);

    /// <inheritdoc />
    public override bool Equals(object? obj) =>
        obj is PdfName other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode() =>
        StringComparer.Ordinal.GetHashCode(Value);

    /// <summary>
    /// Determines whether two PDF name objects are equal.
    /// </summary>
    public static bool operator ==(PdfName? left, PdfName? right) =>
        Equals(left, right);

    /// <summary>
    /// Determines whether two PDF name objects are unequal.
    /// </summary>
    public static bool operator !=(PdfName? left, PdfName? right) =>
        !Equals(left, right);
}

