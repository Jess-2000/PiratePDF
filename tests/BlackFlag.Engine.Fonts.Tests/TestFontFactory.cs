using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace BlackFlag.Engine.Fonts.Tests;

internal static class TestFontFactory
{
    public static MemoryStream CreateMinimalTrueType()
    {
        var tables =
            new Dictionary<string, byte[]>(StringComparer.Ordinal)
            {
                ["cmap"] = Cmap(),
                ["glyf"] = [],
                ["head"] = Head(),
                ["hhea"] = Hhea(),
                ["hmtx"] = Hmtx(),
                ["loca"] = Loca(),
                ["maxp"] = Maxp(),
                ["name"] = Name()
            };

        return new MemoryStream(
            BuildSfnt(
                0x00010000,
                tables),
            writable: false);
    }

    public static byte[] BuildSfnt(
        uint scalar,
        IReadOnlyDictionary<string, byte[]> tables)
    {
        KeyValuePair<string, byte[]>[] ordered =
        [
            .. tables.OrderBy(
                static pair => pair.Key,
                StringComparer.Ordinal)
        ];

        int directoryLength =
            12 + (ordered.Length * 16);
        int cursor = Align4(directoryLength);

        var offsets =
            new Dictionary<string, int>(
                StringComparer.Ordinal);

        foreach (KeyValuePair<string, byte[]> table in ordered)
        {
            cursor = Align4(cursor);
            offsets[table.Key] = cursor;
            cursor += Align4(table.Value.Length);
        }

        byte[] result = new byte[cursor];

        WriteU32(result, 0, scalar);
        WriteU16(result, 4, checked((ushort)ordered.Length));
        WriteSearch(result, ordered.Length);

        for (int index = 0; index < ordered.Length; index++)
        {
            KeyValuePair<string, byte[]> table = ordered[index];
            int record = 12 + (index * 16);
            int offset = offsets[table.Key];

            Encoding.ASCII.GetBytes(table.Key)
                .CopyTo(result.AsSpan(record, 4));

            table.Value.CopyTo(
                result.AsSpan(offset));

            if (table.Key == "head")
            {
                result.AsSpan(offset + 8, 4).Clear();
            }

            WriteU32(
                result,
                record + 4,
                Checksum(
                    result.AsSpan(offset, table.Value.Length),
                    table.Key == "head"));

            WriteU32(result, record + 8, checked((uint)offset));
            WriteU32(result, record + 12, checked((uint)table.Value.Length));
        }

        int headOffset = offsets["head"];
        uint total = Checksum(result, false);
        WriteU32(
            result,
            headOffset + 8,
            unchecked(0xB1B0AFBAu - total));

        return result;
    }

    private static byte[] Head()
    {
        byte[] value = new byte[54];
        WriteU32(value, 0, 0x00010000);
        WriteU32(value, 12, 0x5F0F3CF5);
        WriteU16(value, 18, 1000);
        WriteU16(value, 50, 0);
        return value;
    }

    private static byte[] Maxp()
    {
        byte[] value = new byte[32];
        WriteU32(value, 0, 0x00010000);
        WriteU16(value, 4, 1);
        return value;
    }

    private static byte[] Hhea()
    {
        byte[] value = new byte[36];
        WriteU32(value, 0, 0x00010000);
        WriteU16(value, 34, 1);
        return value;
    }

    private static byte[] Hmtx()
    {
        byte[] value = new byte[4];
        WriteU16(value, 0, 500);
        return value;
    }

    private static byte[] Loca() =>
        new byte[4];

    private static byte[] Cmap()
    {
        // One format-0 subtable with an otherwise empty glyph map.
        byte[] value = new byte[4 + 8 + 262];
        WriteU16(value, 0, 0);
        WriteU16(value, 2, 1);
        WriteU16(value, 4, 3);
        WriteU16(value, 6, 1);
        WriteU32(value, 8, 12);
        WriteU16(value, 12, 0);
        WriteU16(value, 14, 262);
        WriteU16(value, 16, 0);
        return value;
    }

    private static byte[] Name()
    {
        byte[] value = new byte[6];
        WriteU16(value, 0, 0);
        WriteU16(value, 2, 0);
        WriteU16(value, 4, 6);
        return value;
    }

    private static void WriteSearch(
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
        WriteU16(output, 6, checked((ushort)searchRange));
        WriteU16(output, 8, checked((ushort)selector));
        WriteU16(output, 10, checked((ushort)((count * 16) - searchRange)));
    }

    private static uint Checksum(
        ReadOnlySpan<byte> data,
        bool zeroHead)
    {
        uint sum = 0;
        int length = Align4(data.Length);
        Span<byte> word = stackalloc byte[4];

        for (int offset = 0; offset < length; offset += 4)
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

    private static int Align4(int value) =>
        (value + 3) & ~3;

    private static void WriteU16(
        Span<byte> data,
        int offset,
        ushort value) =>
        BinaryPrimitives.WriteUInt16BigEndian(
            data.Slice(offset, 2),
            value);

    private static void WriteU32(
        Span<byte> data,
        int offset,
        uint value) =>
        BinaryPrimitives.WriteUInt32BigEndian(
            data.Slice(offset, 4),
            value);
}
