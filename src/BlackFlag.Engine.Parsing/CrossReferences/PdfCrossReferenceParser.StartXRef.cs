using System.Buffers;
using System.Globalization;

namespace BlackFlag.Engine.Parsing.CrossReferences;

public sealed partial class PdfCrossReferenceParser
{
    private long ParseStartXRef()
    {
        if (source.Length == 0)
        {
            throw new FormatException(
                "An empty source does not contain a startxref section.");
        }

        int searchLength =
            checked(
                (int)Math.Min(
                    source.Length,
                    MaximumStartXRefSearchLength));

        long searchOffset =
            source.Length - searchLength;

        byte[] buffer =
            ArrayPool<byte>.Shared.Rent(searchLength);

        try
        {
            Span<byte> destination =
                buffer.AsSpan(0, searchLength);

            ReadExactly(
                searchOffset,
                destination);

            ReadOnlySpan<byte> bytes =
                destination;

            int endOfFileIndex =
                LastIndexOf(
                    bytes,
                    "%%EOF"u8);

            if (endOfFileIndex < 0)
            {
                throw new FormatException(
                    "The final PDF %%EOF marker was not found.");
            }

            int startXRefIndex =
                LastIndexOf(
                    bytes[..endOfFileIndex],
                    "startxref"u8);

            if (startXRefIndex < 0)
            {
                throw new FormatException(
                    "The final PDF startxref keyword was not found.");
            }

            int index =
                startXRefIndex + "startxref".Length;

            SkipWhiteSpace(
                bytes,
                ref index,
                endOfFileIndex);

            int numberStart =
                index;

            while (
                index < endOfFileIndex &&
                IsAsciiDigit(bytes[index]))
            {
                index++;
            }

            if (numberStart == index)
            {
                throw new FormatException(
                    "The startxref keyword is not followed by an offset.");
            }

            if (!long.TryParse(
                    System.Text.Encoding.ASCII.GetString(
                        bytes[numberStart..index]),
                    NumberStyles.None,
                    CultureInfo.InvariantCulture,
                    out long offset))
            {
                throw new FormatException(
                    "The startxref offset is outside the supported range.");
            }

            SkipWhiteSpace(
                bytes,
                ref index,
                endOfFileIndex);

            if (index != endOfFileIndex)
            {
                throw new FormatException(
                    "Unexpected data occurs between startxref and %%EOF.");
            }

            if (offset >= source.Length)
            {
                throw new FormatException(
                    $"The startxref offset {offset} is outside the source.");
            }

            return offset;
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(
                buffer,
                clearArray: false);
        }
    }

    private static int LastIndexOf(
        ReadOnlySpan<byte> sourceBytes,
        ReadOnlySpan<byte> value)
    {
        if (
            value.IsEmpty ||
            value.Length > sourceBytes.Length)
        {
            return -1;
        }

        for (
            int index = sourceBytes.Length - value.Length;
            index >= 0;
            index--)
        {
            if (sourceBytes.Slice(index, value.Length).SequenceEqual(value))
            {
                return index;
            }
        }

        return -1;
    }
}
