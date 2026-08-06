namespace BlackFlag.Engine.Objects;

/// <summary>
/// Represents the PDF <c>null</c> object.
/// </summary>
public sealed class PdfNull : PdfObject, IEquatable<PdfNull>
{
    private PdfNull()
    {
    }

    /// <summary>
    /// Gets the canonical PDF null instance.
    /// </summary>
    public static PdfNull Instance { get; } = new();

    /// <inheritdoc />
    public override PdfObjectType Type => PdfObjectType.Null;

    /// <inheritdoc />
    public override TResult Accept<TResult>(PdfObjectVisitor<TResult> visitor)
    {
        ArgumentNullException.ThrowIfNull(visitor);
        return visitor.Visit(this);
    }

    /// <inheritdoc />
    public bool Equals(PdfNull? other) => other is not null;

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is PdfNull;

    /// <inheritdoc />
    public override int GetHashCode() => typeof(PdfNull).GetHashCode();

    /// <summary>
    /// Determines whether two PDF null values are equal.
    /// </summary>
    public static bool operator ==(PdfNull? left, PdfNull? right) =>
        ReferenceEquals(left, right) || (left is not null && right is not null);

    /// <summary>
    /// Determines whether two PDF null values are unequal.
    /// </summary>
    public static bool operator !=(PdfNull? left, PdfNull? right) => !(left == right);
}
