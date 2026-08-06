using System.Text;

namespace BlackFlag.Engine.Document.Objects.Primitives;

/// <summary>
/// Represents the decoded bytes of a PDF string object.
/// </summary>
public sealed class PdfString :
    PdfObject,
    IEquatable<PdfString>
{
    private readonly byte[] bytes;

    public PdfString(
        ReadOnlySpan<byte> value,
        PdfStringFormat format = PdfStringFormat.Literal)
        : base(PdfObjectKind.ByteString)
    {
        bytes = value.ToArray();
        Format = format;
    }

    public PdfStringFormat Format { get; }

    public int Length =>
        bytes.Length;

    public bool IsEmpty =>
        bytes.Length == 0;

    /// <summary>
    /// Creates a PDF string from Latin-1 text.
    /// </summary>
    public static PdfString FromLatin1(
        string value,
        PdfStringFormat format = PdfStringFormat.Literal)
    {
        ArgumentNullException.ThrowIfNull(value);

        return new PdfString(
            Encoding.Latin1.GetBytes(value),
            format);
    }

    /// <summary>
    /// Gets a defensive copy of the decoded string bytes.
    /// </summary>
    public byte[] ToByteArray()
    {
        return bytes.ToArray();
    }

    /// <summary>
    /// Decodes the string bytes using Latin-1.
    /// </summary>
    public string GetLatin1Text()
    {
        return Encoding.Latin1.GetString(bytes);
    }

    public bool Equals(PdfString? other)
    {
        return other is not null &&
               Format == other.Format &&
               bytes.AsSpan().SequenceEqual(
                   other.bytes);
    }

    public override bool Equals(object? obj)
    {
        return obj is PdfString other &&
               Equals(other);
    }

    public override int GetHashCode()
    {
        var hash = new HashCode();

        hash.Add(Format);

        foreach (var value in bytes)
        {
            hash.Add(value);
        }

        return hash.ToHashCode();
    }

    public override string ToString()
    {
        return GetLatin1Text();
    }

    public static bool operator ==(
        PdfString? left,
        PdfString? right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(
        PdfString? left,
        PdfString? right)
    {
        return !Equals(left, right);
    }
}
