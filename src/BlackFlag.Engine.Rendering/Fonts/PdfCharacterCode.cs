namespace BlackFlag.Engine.Rendering.Fonts;

/// <summary>Represents an immutable variable-length PDF character code.</summary>
public sealed class PdfCharacterCode : IEquatable<PdfCharacterCode>
{
    private readonly byte[] bytes;

    /// <summary>Initializes a PDF character code.</summary>
    public PdfCharacterCode(ReadOnlySpan<byte> bytes)
    {
        if (bytes.IsEmpty)
        {
            throw new ArgumentException("A PDF character code cannot be empty.", nameof(bytes));
        }

        this.bytes = bytes.ToArray();
    }

    /// <summary>Gets the encoded bytes.</summary>
    public ReadOnlyMemory<byte> Bytes => bytes;

    /// <inheritdoc />
    public bool Equals(PdfCharacterCode? other) =>
        other is not null && bytes.AsSpan().SequenceEqual(other.bytes);

    /// <inheritdoc />
    public override bool Equals(object? obj) =>
        obj is PdfCharacterCode other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode()
    {
        HashCode hash = new();
        foreach (byte value in bytes)
        {
            hash.Add(value);
        }

        return hash.ToHashCode();
    }

    /// <inheritdoc />
    public override string ToString() =>
        Convert.ToHexString(bytes);
}
