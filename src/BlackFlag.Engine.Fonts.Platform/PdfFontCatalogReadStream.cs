namespace BlackFlag.Engine.Fonts.Platform;

/// <summary>
/// Charges actual reads even if file length changes or a sanitizer seeks/rereads.
/// The build owns and disposes the underlying stream separately.
/// </summary>
internal sealed class PdfFontCatalogReadStream(
    Stream source,
    PdfFontCatalogBuildBudget budget,
    CancellationToken cancellationToken) : Stream
{
    public override bool CanRead => source.CanRead;
    public override bool CanSeek => source.CanSeek;
    public override bool CanWrite => false;
    public override long Length => source.Length;
    public override long Position { get => source.Position; set => source.Position = value; }

    public override int Read(byte[] buffer, int offset, int count)
    {
        ValidateBufferArguments(buffer, offset, count);
        return Read(buffer.AsSpan(offset, count));
    }

    public override int Read(Span<byte> buffer)
    {
        cancellationToken.ThrowIfCancellationRequested();
        budget.ThrowIfExceeded();
        long remaining = budget.RemainingSourceBytes;
        // An EOF probe distinguishes an exact fit from changed/underreported length.
        // Widened subtraction and conditional narrowing avoid max-value overflow.
        int allowed = remaining >= buffer.Length ? buffer.Length : (int)remaining + 1;
        int read = source.Read(buffer[..allowed]);
        budget.RecordRead(read);
        cancellationToken.ThrowIfCancellationRequested();
        return read;
    }

    public override int ReadByte()
    {
        Span<byte> value = stackalloc byte[1];
        return Read(value) == 0 ? -1 : value[0];
    }

    public override long Seek(long offset, SeekOrigin origin) => source.Seek(offset, origin);
    public override void Flush() => source.Flush();
    public override void SetLength(long value) => throw new NotSupportedException();
    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
}
