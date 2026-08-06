using System.Text;
using BlackFlag.Engine.IO.RandomAccess;

namespace BlackFlag.Engine.Parsing.CrossReferences;

public sealed partial class PdfCrossReferenceParser
{
    private void ReadExactly(
        long offset,
        Span<byte> destination)
    {
        int totalBytesRead = 0;

        while (totalBytesRead < destination.Length)
        {
            int bytesRead =
                source.Read(
                    checked(offset + totalBytesRead),
                    destination[totalBytesRead..]);

            if (
                bytesRead < 0 ||
                bytesRead > destination.Length - totalBytesRead)
            {
                throw new InvalidDataException(
                    "The random-access source returned an invalid byte count.");
            }

            if (bytesRead == 0)
            {
                throw new EndOfStreamException(
                    $"The source ended while reading {destination.Length} "
                    + $"bytes at offset {offset}.");
            }

            totalBytesRead += bytesRead;
        }
    }

    private static void SkipWhiteSpace(
        BufferedRandomAccessCursor cursor)
    {
        while (
            cursor.TryPeekByte(out byte value) &&
            IsPdfWhiteSpace(value))
        {
            cursor.Skip(1);
        }
    }

    private static void SkipWhiteSpace(
        ReadOnlySpan<byte> bytes,
        ref int index,
        int end)
    {
        while (
            index < end &&
            IsPdfWhiteSpace(bytes[index]))
        {
            index++;
        }
    }

    private static void ExpectAscii(
        BufferedRandomAccessCursor cursor,
        string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);

        long startOffset =
            cursor.Position;

        foreach (char character in value)
        {
            if (
                !cursor.TryReadByte(out byte sourceByte) ||
                sourceByte != (byte)character)
            {
                throw new FormatException(
                    $"Expected '{value}' at offset {startOffset}.");
            }
        }
    }

    private static void RequireLineEnd(
        BufferedRandomAccessCursor cursor)
    {
        while (
            cursor.TryPeekByte(out byte value) &&
            IsHorizontalWhiteSpace(value))
        {
            cursor.Skip(1);
        }

        if (!cursor.TryReadByte(out byte lineEnd))
        {
            throw new EndOfStreamException(
                "The source ended before the xref keyword line was terminated.");
        }

        if (lineEnd == 0x0A)
        {
            return;
        }

        if (lineEnd == 0x0D)
        {
            if (
                cursor.TryPeekByte(out byte next) &&
                next == 0x0A)
            {
                cursor.Skip(1);
            }

            return;
        }

        throw new FormatException(
            "Unexpected data follows the xref keyword.");
    }

    private static string ReadNextNonEmptyLine(
        BufferedRandomAccessCursor cursor,
        out long lineOffset)
    {
        while (true)
        {
            string line =
                ReadLine(
                    cursor,
                    out lineOffset);

            string trimmed =
                line.Trim();

            if (trimmed.Length != 0)
            {
                return trimmed;
            }
        }
    }

    private static string ReadLine(
        BufferedRandomAccessCursor cursor,
        out long lineOffset)
    {
        lineOffset =
            cursor.Position;

        var bytes =
            new List<byte>(32);

        while (cursor.TryReadByte(out byte value))
        {
            if (value == 0x0A)
            {
                return Encoding.ASCII.GetString(
                    bytes.ToArray());
            }

            if (value == 0x0D)
            {
                if (
                    cursor.TryPeekByte(out byte next) &&
                    next == 0x0A)
                {
                    cursor.Skip(1);
                }

                return Encoding.ASCII.GetString(
                    bytes.ToArray());
            }

            bytes.Add(value);
        }

        if (bytes.Count == 0)
        {
            throw new EndOfStreamException(
                $"The source ended while a line was expected at offset "
                + $"{lineOffset}.");
        }

        return Encoding.ASCII.GetString(
            bytes.ToArray());
    }

    private static bool IsAsciiDigit(
        byte value)
    {
        return value is >= (byte)'0' and <= (byte)'9';
    }

    private static bool IsHorizontalWhiteSpace(
        byte value)
    {
        return value is
            0x00 or
            0x09 or
            0x0C or
            0x20;
    }

    private static bool IsPdfWhiteSpace(
        byte value)
    {
        return value is
            0x00 or
            0x09 or
            0x0A or
            0x0C or
            0x0D or
            0x20;
    }
}

