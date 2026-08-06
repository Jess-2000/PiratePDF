namespace BlackFlag.Engine.Objects;

/// <summary>
/// Represents a PDF hexadecimal-string object as an immutable byte sequence.
/// </summary>
public sealed class PdfHexString : PdfObject, IEquatable<PdfHexString>
{
    private readonly byte[] value;

    /// <summary>
    /// Initializes a new PDF hexadecimal-string object.
    /// </summary>
    /// <param name="value">The decoded byte sequence.</param>
    public PdfHexString(ReadOnlyMemory<byte> value)
    {
        this.value = value.ToArray();
    }

    /// <summary>
    /// Gets the decoded byte sequence.
    /// </summary>
    public ReadOnlyMemory<byte> Value => value;

    /// <inheritdoc />
    public override PdfObjectType Type => PdfObjectType.HexString;

    /// <inheritdoc />
    public override TResult Accept<TResult>(PdfObjectVisitor<TResult> visitor)
    {
        ArgumentNullException.ThrowIfNull(visitor);
        return visitor.Visit(this);
    }

    /// <inheritdoc />
    public bool Equals(PdfHexString? other) =>
        other is not null
        && value.AsSpan().SequenceEqual(other.value);

    /// <inheritdoc />
    public override bool Equals(object? obj) =>
        obj is PdfHexString other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode()
    {
        var hash = new HashCode();

        foreach (byte item in value)
        {
            hash.Add(item);
        }

        return hash.ToHashCode();
    }

    /// <summary>
    /// Determines whether two PDF hexadecimal-string objects are equal.
    /// </summary>
    public static bool operator ==(PdfHexString? left, PdfHexString? right) =>
        Equals(left, right);

    /// <summary>
    /// Determines whether two PDF hexadecimal-string objects are unequal.
    /// </summary>
    public static bool operator !=(PdfHexString? left, PdfHexString? right) =>
        !Equals(left, right);
}

