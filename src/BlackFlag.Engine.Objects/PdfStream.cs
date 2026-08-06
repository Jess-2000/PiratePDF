namespace BlackFlag.Engine.Objects;

/// <summary>
/// Represents a PDF stream object composed of an immutable dictionary and an
/// immutable snapshot of stream bytes.
/// </summary>
/// <remarks>
/// This object-model type does not parse, encode, decode, normalize, or
/// synchronize the stream dictionary's <c>/Length</c> entry.
/// </remarks>
public sealed class PdfStream :
    PdfObject,
    IEquatable<PdfStream>
{
    private static readonly byte[] EmptyData = [];
    private readonly byte[] data;

    /// <summary>
    /// Initializes a new PDF stream.
    /// </summary>
    /// <param name="dictionary">The immutable stream dictionary.</param>
    /// <param name="data">The bytes stored by the stream.</param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="dictionary"/> is <see langword="null"/>.
    /// </exception>
    public PdfStream(
        PdfDictionary dictionary,
        ReadOnlyMemory<byte> data)
    {
        ArgumentNullException.ThrowIfNull(dictionary);

        Dictionary = dictionary;
        this.data = data.IsEmpty
            ? EmptyData
            : data.Span.ToArray();
    }

    /// <summary>
    /// Gets the immutable stream dictionary.
    /// </summary>
    public PdfDictionary Dictionary { get; }

    /// <summary>
    /// Gets a read-only view of the stream bytes.
    /// </summary>
    public ReadOnlyMemory<byte> Data => data;

    /// <inheritdoc />
    public override PdfObjectType Type => PdfObjectType.Stream;

    /// <inheritdoc />
    public override TResult Accept<TResult>(PdfObjectVisitor<TResult> visitor)
    {
        ArgumentNullException.ThrowIfNull(visitor);
        return visitor.Visit(this);
    }

    /// <inheritdoc />
    public bool Equals(PdfStream? other) =>
        other is not null
        && Dictionary.Equals(other.Dictionary)
        && data.AsSpan().SequenceEqual(other.data);

    /// <inheritdoc />
    public override bool Equals(object? obj) =>
        obj is PdfStream other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode()
    {
        HashCode hash = new();
        hash.Add(Dictionary);

        foreach (byte value in data)
        {
            hash.Add(value);
        }

        return hash.ToHashCode();
    }

    /// <summary>
    /// Determines whether two PDF streams are equal.
    /// </summary>
    /// <param name="left">The first stream to compare.</param>
    /// <param name="right">The second stream to compare.</param>
    /// <returns>
    /// <see langword="true"/> when the streams are equal; otherwise,
    /// <see langword="false"/>.
    /// </returns>
    public static bool operator ==(
        PdfStream? left,
        PdfStream? right) =>
        Equals(left, right);

    /// <summary>
    /// Determines whether two PDF streams are unequal.
    /// </summary>
    /// <param name="left">The first stream to compare.</param>
    /// <param name="right">The second stream to compare.</param>
    /// <returns>
    /// <see langword="true"/> when the streams are unequal; otherwise,
    /// <see langword="false"/>.
    /// </returns>
    public static bool operator !=(
        PdfStream? left,
        PdfStream? right) =>
        !Equals(left, right);
}
