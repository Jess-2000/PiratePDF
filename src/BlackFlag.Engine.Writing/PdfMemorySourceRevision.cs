namespace BlackFlag.Engine.Writing;

/// <summary>Owns an immutable in-memory snapshot of a preceding PDF revision.</summary>
public sealed class PdfMemorySourceRevision : IPdfOriginalSourceRevision
{
    private const int CopyBufferSize = 64 * 1024;

    private readonly byte[] bytes;

    public PdfMemorySourceRevision(
        ReadOnlySpan<byte> bytes,
        long crossReferenceOffset,
        PdfCrossReferenceWriteMode crossReferenceMode)
    {
        if (bytes.IsEmpty)
        {
            throw new ArgumentException(
                "An original PDF source revision cannot be empty.",
                nameof(bytes));
        }

        if (crossReferenceOffset < 0 || crossReferenceOffset >= bytes.Length)
        {
            throw new ArgumentOutOfRangeException(
                nameof(crossReferenceOffset),
                crossReferenceOffset,
                "The active cross-reference offset must identify a byte inside the source revision.");
        }

        if (crossReferenceMode == PdfCrossReferenceWriteMode.PreserveSource ||
            !Enum.IsDefined(crossReferenceMode))
        {
            throw new ArgumentOutOfRangeException(
                nameof(crossReferenceMode),
                crossReferenceMode,
                "The source revision must identify its concrete cross-reference representation.");
        }

        this.bytes = bytes.ToArray();
        CrossReferenceOffset = crossReferenceOffset;
        CrossReferenceMode = crossReferenceMode;
    }

    public long Length => bytes.LongLength;

    public long CrossReferenceOffset { get; }

    public PdfCrossReferenceWriteMode CrossReferenceMode { get; }

    public void CopyTo(Stream destination)
    {
        ArgumentNullException.ThrowIfNull(destination);

        if (!destination.CanWrite)
        {
            throw new ArgumentException(
                "The source-revision destination must be writable.",
                nameof(destination));
        }

        // Caller-controlled Write callbacks must never receive retained revision storage.
        // Reuse one independently owned, bounded buffer only within this copy operation.
        var buffer = new byte[CopyBufferSize];
        int offset = 0;
        while (offset < bytes.Length)
        {
            int count = Math.Min(buffer.Length, bytes.Length - offset);
            bytes.AsSpan(offset, count).CopyTo(buffer);
            destination.Write(buffer.AsSpan(0, count));
            offset += count;
        }
    }
}
