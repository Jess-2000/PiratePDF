using System.Diagnostics.CodeAnalysis;

namespace BlackFlag.Engine.Document.Objects;

/// <summary>
/// Represents a PDF stream dictionary and its encoded stream bytes.
/// </summary>
[SuppressMessage(
    "Naming",
    "CA1711:Identifiers should not have incorrect suffix",
    Justification =
        "PDF stream is the official domain term defined by the PDF specification.")]
public sealed class PdfStream : PdfObject
{
    private byte[] data;

    public PdfStream(
        PdfDictionary dictionary,
        ReadOnlySpan<byte> data)
        : base(PdfObjectKind.StreamValue)
    {
        ArgumentNullException.ThrowIfNull(dictionary);

        Dictionary = dictionary.CreateCopy();
        this.data = data.ToArray();
    }

    /// <summary>
    /// Gets the stream dictionary owned by this PDF stream.
    /// </summary>
    public PdfDictionary Dictionary { get; }

    /// <summary>
    /// Gets the encoded stream-data length.
    /// </summary>
    public int Length =>
        data.Length;

    /// <summary>
    /// Gets whether the encoded stream data is empty.
    /// </summary>
    public bool IsEmpty =>
        data.Length == 0;

    /// <summary>
    /// Gets the instance-local revision of the encoded data. Each successful
    /// replacement advances it, including replacement with identical bytes.
    /// This does not track dictionary changes or provide thread synchronization.
    /// </summary>
    public long DataRevision { get; private set; }

    /// <summary>
    /// Returns a defensive copy of the encoded stream bytes.
    /// </summary>
    public byte[] ToByteArray()
    {
        return data.ToArray();
    }

    /// <summary>
    /// Replaces the encoded stream bytes.
    /// </summary>
    public void ReplaceData(
        ReadOnlySpan<byte> replacement)
    {
        long nextRevision = checked(DataRevision + 1);
        byte[] snapshot = replacement.ToArray();
        data = snapshot;
        DataRevision = nextRevision;
    }
}
