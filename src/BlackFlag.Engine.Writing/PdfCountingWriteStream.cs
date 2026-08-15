namespace BlackFlag.Engine.Writing;

/// <summary>Counts bytes written while forwarding them to a caller-owned stream.</summary>
internal sealed class PdfCountingWriteStream : Stream
{
    private readonly Stream destination;

    internal PdfCountingWriteStream(Stream destination)
    {
        ArgumentNullException.ThrowIfNull(destination);
        this.destination = destination;
    }

    internal long BytesWritten { get; private set; }

    public override bool CanRead => false;

    public override bool CanSeek => false;

    public override bool CanWrite => destination.CanWrite;

    public override long Length => throw new NotSupportedException();

    public override long Position
    {
        get => throw new NotSupportedException();
        set => throw new NotSupportedException();
    }

    public override void Flush() => destination.Flush();

    public override int Read(byte[] buffer, int offset, int count) =>
        throw new NotSupportedException();

    public override long Seek(long offset, SeekOrigin origin) =>
        throw new NotSupportedException();

    public override void SetLength(long value) =>
        throw new NotSupportedException();

    public override void Write(byte[] buffer, int offset, int count)
    {
        destination.Write(buffer, offset, count);
        BytesWritten = checked(BytesWritten + count);
    }

    public override void Write(ReadOnlySpan<byte> buffer)
    {
        destination.Write(buffer);
        BytesWritten = checked(BytesWritten + buffer.Length);
    }

    public override void WriteByte(byte value)
    {
        destination.WriteByte(value);
        BytesWritten = checked(BytesWritten + 1);
    }

    protected override void Dispose(bool disposing)
    {
        // The caller owns the destination stream. This wrapper never closes it.
        base.Dispose(disposing);
    }
}
