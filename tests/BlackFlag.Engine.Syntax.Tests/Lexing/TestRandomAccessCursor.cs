using BlackFlag.Engine.IO.RandomAccess;

namespace BlackFlag.Engine.Syntax.Tests.Lexing;

internal sealed class TestRandomAccessCursor :
    IRandomAccessCursor
{
    private readonly byte[] source;

    public TestRandomAccessCursor(
        byte[] source)
    {
        ArgumentNullException.ThrowIfNull(
            source);

        this.source =
            source;
    }

    public long Length =>
        source.LongLength;

    public long Position { get; private set; }

    public bool EndOfSource =>
        Position >= Length;

    public void Seek(
        long position)
    {
        if (
            position < 0 ||
            position > Length)
        {
            throw new ArgumentOutOfRangeException(
                nameof(position));
        }

        Position =
            position;
    }

    public void Skip(
        long byteCount)
    {
        if (byteCount < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(byteCount));
        }

        Seek(
            checked(
                Position + byteCount));
    }

    public byte ReadByte()
    {
        if (!TryReadByte(out byte value))
        {
            throw new EndOfStreamException();
        }

        return value;
    }

    public bool TryReadByte(
        out byte value)
    {
        if (EndOfSource)
        {
            value =
                default;

            return false;
        }

        value =
            source[
                checked(
                    (int)Position)];

        Position++;

        return true;
    }

    public byte PeekByte()
    {
        if (!TryPeekByte(out byte value))
        {
            throw new EndOfStreamException();
        }

        return value;
    }

    public bool TryPeekByte(
        out byte value)
    {
        if (EndOfSource)
        {
            value =
                default;

            return false;
        }

        value =
            source[
                checked(
                    (int)Position)];

        return true;
    }

    public int Read(
        Span<byte> destination)
    {
        int available =
            checked(
                (int)Math.Min(
                    destination.Length,
                    Length - Position));

        if (available == 0)
        {
            return 0;
        }

        source
            .AsSpan(
                checked(
                    (int)Position),
                available)
            .CopyTo(
                destination);

        Position +=
            available;

        return available;
    }
}