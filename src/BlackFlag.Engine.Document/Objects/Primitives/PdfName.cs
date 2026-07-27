using System.Text;

namespace BlackFlag.Engine.Document.Objects.Primitives;

/// <summary>
/// Represents the decoded byte content of a PDF name object.
/// </summary>
public sealed class PdfName :
    PdfObject,
    IEquatable<PdfName>
{
    private readonly byte[] bytes;

    public PdfName(ReadOnlySpan<byte> value)
        : base(PdfObjectKind.NameValue)
    {
        if (value.IsEmpty)
        {
            throw new ArgumentException(
                "A PDF name cannot be empty.",
                nameof(value));
        }

        bytes = value.ToArray();
    }

    /// <summary>
    /// Creates a PDF name from Latin-1 text.
    /// </summary>
    public static PdfName FromLatin1(string value)
    {
        ArgumentException.ThrowIfNullOrEmpty(value);

        return new PdfName(
            Encoding.Latin1.GetBytes(value));
    }

    /// <summary>
    /// Gets a defensive copy of the decoded name bytes.
    /// </summary>
    public byte[] ToByteArray()
    {
        return bytes.ToArray();
    }

    /// <summary>
    /// Decodes the name bytes using Latin-1.
    /// </summary>
    public string GetLatin1Text()
    {
        return Encoding.Latin1.GetString(bytes);
    }

    public bool Equals(PdfName? other)
    {
        return other is not null &&
               bytes.AsSpan().SequenceEqual(
                   other.bytes);
    }

    public override bool Equals(object? obj)
    {
        return obj is PdfName other &&
               Equals(other);
    }

    public override int GetHashCode()
    {
        var hash = new HashCode();

        foreach (var value in bytes)
        {
            hash.Add(value);
        }

        return hash.ToHashCode();
    }

    public override string ToString()
    {
        return string.Concat(
            "/",
            GetLatin1Text());
    }

    public static bool operator ==(
        PdfName? left,
        PdfName? right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(
        PdfName? left,
        PdfName? right)
    {
        return !Equals(left, right);
    }
}