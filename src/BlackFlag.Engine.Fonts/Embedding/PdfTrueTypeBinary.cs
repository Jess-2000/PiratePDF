using System.Buffers.Binary;
using System.Text;

namespace BlackFlag.Engine.Fonts.Embedding;

internal readonly record struct PdfTrueTypeTable(int Offset, int Length);

internal static class PdfTrueTypeBinary
{
    public static Dictionary<string, PdfTrueTypeTable> ReadTables(
        ReadOnlySpan<byte> bytes)
    {
        int count = U16(bytes, 4);
        var result =
            new Dictionary<string, PdfTrueTypeTable>(
                StringComparer.Ordinal);

        for (int index = 0; index < count; index++)
        {
            int record = checked(12 + index * 16);
            string tag =
                Encoding.ASCII.GetString(
                    bytes.Slice(record, 4));

            int offset =
                checked((int)U32(bytes, record + 8));

            int length =
                checked((int)U32(bytes, record + 12));

            if (!result.TryAdd(
                    tag,
                    new PdfTrueTypeTable(offset, length)))
            {
                throw new InvalidDataException(
                    "The sanitized font contains duplicate table tags.");
            }
        }

        return result;
    }

    public static uint[] ReadLoca(
        ReadOnlySpan<byte> loca,
        int glyphCount,
        int format,
        int glyfLength)
    {
        int count = checked(glyphCount + 1);
        var result = new uint[count];

        for (int index = 0; index < count; index++)
        {
            result[index] =
                format == 0
                    ? checked((uint)U16(loca, index * 2) * 2u)
                    : U32(loca, index * 4);

            if (result[index] > glyfLength ||
                index > 0 && result[index] < result[index - 1])
            {
                throw new InvalidDataException(
                    "The sanitized loca table is inconsistent.");
            }
        }

        return result;
    }

    public static PdfTrueTypeTable Require(
        IReadOnlyDictionary<string, PdfTrueTypeTable> tables,
        string tag) =>
        tables.TryGetValue(tag, out PdfTrueTypeTable table)
            ? table
            : throw new InvalidDataException(
                $"The sanitized font is missing required table '{tag}'.");

    public static ReadOnlySpan<byte> Slice(
        ReadOnlySpan<byte> bytes,
        PdfTrueTypeTable table) =>
        bytes.Slice(table.Offset, table.Length);

    public static int Align4(int value) =>
        checked((value + 3) & ~3);

    public static int U16(
        ReadOnlySpan<byte> data,
        int offset)
    {
        if (offset < 0 || offset > data.Length - 2)
        {
            throw new InvalidDataException(
                "A subsetter table read exceeded its boundary.");
        }

        return BinaryPrimitives.ReadUInt16BigEndian(
            data.Slice(offset, 2));
    }

    public static int I16(
        ReadOnlySpan<byte> data,
        int offset) =>
        unchecked((short)U16(data, offset));

    public static uint U32(
        ReadOnlySpan<byte> data,
        int offset)
    {
        if (offset < 0 || offset > data.Length - 4)
        {
            throw new InvalidDataException(
                "A subsetter table read exceeded its boundary.");
        }

        return BinaryPrimitives.ReadUInt32BigEndian(
            data.Slice(offset, 4));
    }

    public static void W16(
        Span<byte> data,
        int offset,
        ushort value) =>
        BinaryPrimitives.WriteUInt16BigEndian(
            data.Slice(offset, 2),
            value);

    public static void W32(
        Span<byte> data,
        int offset,
        uint value) =>
        BinaryPrimitives.WriteUInt32BigEndian(
            data.Slice(offset, 4),
            value);

    public static void WriteSearchFields(
        Span<byte> output,
        int count)
    {
        int power = 1;
        int selector = 0;

        while ((power << 1) <= count)
        {
            power <<= 1;
            selector++;
        }

        int searchRange = power * 16;

        W16(output, 6, checked((ushort)searchRange));
        W16(output, 8, checked((ushort)selector));
        W16(
            output,
            10,
            checked((ushort)(count * 16 - searchRange)));
    }

    public static uint Checksum(
        ReadOnlySpan<byte> data,
        bool zeroHead)
    {
        uint sum = 0;
        int padded = Align4(data.Length);
        Span<byte> word = stackalloc byte[4];

        for (int offset = 0; offset < padded; offset += 4)
        {
            word.Clear();

            if (offset < data.Length)
            {
                data.Slice(
                        offset,
                        Math.Min(4, data.Length - offset))
                    .CopyTo(word);
            }

            if (zeroHead && offset == 8)
            {
                word.Clear();
            }

            sum =
                unchecked(
                    sum +
                    BinaryPrimitives.ReadUInt32BigEndian(word));
        }

        return sum;
    }
}
