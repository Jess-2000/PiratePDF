using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using BlackFlag.Engine.Fonts.Platform.Windows;

namespace BlackFlag.Engine.Fonts.Platform.Tests;

public sealed partial class PdfFileSystemExternalFontProviderTests :
    IDisposable
{
    private readonly string directory;

    public PdfFileSystemExternalFontProviderTests()
    {
        directory =
            Path.Combine(
                Path.GetTempPath(),
                "PiratePDF-FontProviderTests-" +
                Guid.NewGuid().ToString("N"));

        Directory.CreateDirectory(
            directory);
    }

    [Fact]
    public void ProviderDiscoversSanitizedStandaloneTrueTypeFace()
    {
        string path =
            Path.Combine(
                directory,
                "Test-Regular.ttf");

        File.WriteAllBytes(
            path,
            BuildFont());

        var provider =
            new PdfWindowsExternalFontProvider(
                [directory]);

        PdfExternalFontFace face =
            Assert.Single(
                provider.GetFaces());

        Assert.Equal(
            "Test Family",
            face.FamilyName);

        Assert.Equal(
            "Test Family Regular",
            face.FaceName);

        Assert.Equal(
            400,
            face.Weight);

        Assert.Equal(
            PdfExternalFontTechnology.TrueType,
            face.Technology);

        Assert.True(
            provider.SupportsRune(
                face,
                new Rune('A')));

        Assert.False(
            provider.SupportsRune(
                face,
                new Rune('B')));
    }

    [Fact]
    public void InvalidFontIsOmittedRatherThanExposed()
    {
        File.WriteAllBytes(
            Path.Combine(
                directory,
                "Broken.ttf"),
            [1, 2, 3, 4]);

        var provider =
            new PdfWindowsExternalFontProvider(
                [directory]);

        Assert.Empty(
            provider.GetFaces());
    }

    [Fact]
    public void NonFontExtensionsAreIgnored()
    {
        File.WriteAllText(
            Path.Combine(
                directory,
                "NotAFont.txt"),
            "not a font");

        var provider =
            new PdfWindowsExternalFontProvider(
                [directory]);

        Assert.Empty(
            provider.GetFaces());
    }

    public void Dispose()
    {
        try
        {
            Directory.Delete(
                directory,
                recursive: true);
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }

    private static byte[] BuildFont(byte[] cmap = null, ushort version = 0, ushort fsType = 0)
    {
        byte[] glyph0 =
            SimpleGlyph(16);

        byte[] glyph1 =
            SimpleGlyph(20);

        byte[] glyf =
            BuildGlyf(
                [glyph0, glyph1],
                out uint[] offsets);

        byte[] loca =
            new byte[
                offsets.Length * 4];

        for (int index = 0;
            index < offsets.Length;
            index++)
        {
            W32(
                loca,
                index * 4,
                offsets[index]);
        }

        var tables =
            new Dictionary<string, byte[]>(
                StringComparer.Ordinal)
            {
                ["OS/2"] = Os2(version, fsType),
                ["cmap"] = cmap ?? Cmap(),
                ["glyf"] = glyf,
                ["head"] = Head(),
                ["hhea"] = Hhea(),
                ["hmtx"] = Hmtx(),
                ["loca"] = loca,
                ["maxp"] = Maxp(),
                ["name"] = Name()
            };

        return BuildSfnt(
            tables);
    }

    private static byte[] Name()
    {
        byte[] family =
            Encoding.BigEndianUnicode.GetBytes(
                "Test Family");

        byte[] subfamily =
            Encoding.BigEndianUnicode.GetBytes(
                "Regular");

        byte[] full =
            Encoding.BigEndianUnicode.GetBytes(
                "Test Family Regular");

        byte[][] strings =
        [
            family,
            subfamily,
            full
        ];

        const int count = 3;
        const int storage =
            6 +
            count * 12;

        int length =
            storage +
            strings.Sum(
                static value =>
                    value.Length);

        byte[] table =
            new byte[length];

        W16(table, 0, 0);
        W16(table, 2, count);
        W16(table, 4, storage);

        int stringOffset = 0;

        for (int index = 0;
            index < count;
            index++)
        {
            int record =
                6 +
                index * 12;

            W16(table, record, 3);
            W16(table, record + 2, 1);
            W16(table, record + 4, 0x0409);
            W16(
                table,
                record + 6,
                checked(
                    (ushort)(
                        index + 1)));

            W16(
                table,
                record + 8,
                checked(
                    (ushort)strings[index].Length));

            W16(
                table,
                record + 10,
                checked(
                    (ushort)stringOffset));

            strings[index].CopyTo(
                table.AsSpan(
                    storage +
                    stringOffset));

            stringOffset +=
                strings[index].Length;
        }

        // Record IDs above are 1, 2, 3; convert third to full-name ID 4.
        W16(
            table,
            6 + 2 * 12 + 6,
            4);

        return table;
    }

    private static byte[] Os2(ushort version, ushort fsType)
    {
        byte[] value =
            new byte[version switch { 0 => 78, 1 => 86, 5 => 100, _ => 96 }];

        W16(value, 0, version);
        W16(value, 4, 400);
        W16(value, 6, 5);
        W16(value, 8, fsType);
        W16(value, 62, 0x0040);

        return value;
    }

    private static byte[] Cmap()
    {
        byte[] format0 =
            new byte[262];

        W16(format0, 0, 0);
        W16(format0, 2, 262);
        format0[6 + 65] = 1;

        byte[] cmap =
            new byte[
                12 +
                format0.Length];

        W16(cmap, 0, 0);
        W16(cmap, 2, 1);
        W16(cmap, 4, 3);
        W16(cmap, 6, 1);
        W32(cmap, 8, 12);

        format0.CopyTo(
            cmap.AsSpan(12));

        return cmap;
    }

    private static byte[] Head()
    {
        byte[] value =
            new byte[54];

        W32(value, 0, 0x00010000);
        W32(value, 12, 0x5F0F3CF5);
        W16(value, 18, 1000);
        W16(value, 50, 1);

        return value;
    }

    private static byte[] Hhea()
    {
        byte[] value =
            new byte[36];

        W32(value, 0, 0x00010000);
        W16(value, 4, 800);
        W16(value, 6, unchecked((ushort)-200));
        W16(value, 34, 2);

        return value;
    }

    private static byte[] Hmtx()
    {
        byte[] value =
            new byte[8];

        W16(value, 0, 500);
        W16(value, 4, 600);

        return value;
    }

    private static byte[] Maxp()
    {
        byte[] value =
            new byte[32];

        W32(value, 0, 0x00010000);
        W16(value, 4, 2);

        return value;
    }

    private static byte[] BuildGlyf(
        IReadOnlyList<byte[]> glyphs,
        out uint[] offsets)
    {
        using var destination =
            new MemoryStream();

        offsets =
            new uint[
                glyphs.Count + 1];

        for (int index = 0;
            index < glyphs.Count;
            index++)
        {
            offsets[index] =
                checked(
                    (uint)destination.Length);

            destination.Write(
                glyphs[index]);

            while ((destination.Length & 3) != 0)
            {
                destination.WriteByte(0);
            }
        }

        offsets[glyphs.Count] =
            checked(
                (uint)destination.Length);

        return destination.ToArray();
    }

    private static byte[] SimpleGlyph(
        int length)
    {
        byte[] value =
            new byte[
                Math.Max(
                    10,
                    length)];

        W16(value, 0, 1);
        return value;
    }

    private static byte[] BuildSfnt(
        IReadOnlyDictionary<string, byte[]> tables)
    {
        KeyValuePair<string, byte[]>[] ordered =
        [
            .. tables.OrderBy(
                static pair =>
                    pair.Key,
                StringComparer.Ordinal)
        ];

        int directory =
            12 +
            ordered.Length * 16;

        int cursor =
            Align4(
                directory);

        var offsets =
            new Dictionary<string, int>(
                StringComparer.Ordinal);

        foreach (KeyValuePair<string, byte[]> table in ordered)
        {
            cursor =
                Align4(
                    cursor);

            offsets[table.Key] =
                cursor;

            cursor +=
                Align4(
                    table.Value.Length);
        }

        byte[] output =
            new byte[cursor];

        W32(output, 0, 0x00010000);
        W16(
            output,
            4,
            checked(
                (ushort)ordered.Length));

        int power = 1;
        int selector = 0;

        while ((power << 1) <=
            ordered.Length)
        {
            power <<= 1;
            selector++;
        }

        int range =
            power * 16;

        W16(
            output,
            6,
            checked(
                (ushort)range));

        W16(
            output,
            8,
            checked(
                (ushort)selector));

        W16(
            output,
            10,
            checked(
                (ushort)(
                    ordered.Length * 16 -
                    range)));

        for (int index = 0;
            index < ordered.Length;
            index++)
        {
            KeyValuePair<string, byte[]> table =
                ordered[index];

            int record =
                12 +
                index * 16;

            int offset =
                offsets[table.Key];

            Encoding.ASCII
                .GetBytes(
                    table.Key)
                .CopyTo(
                    output.AsSpan(
                        record,
                        4));

            table.Value.CopyTo(
                output.AsSpan(
                    offset));

            if (table.Key == "head")
            {
                output.AsSpan(
                        offset + 8,
                        4)
                    .Clear();
            }

            W32(
                output,
                record + 4,
                Checksum(
                    output.AsSpan(
                        offset,
                        table.Value.Length),
                    table.Key == "head"));

            W32(
                output,
                record + 8,
                checked(
                    (uint)offset));

            W32(
                output,
                record + 12,
                checked(
                    (uint)table.Value.Length));
        }

        int headOffset =
            offsets["head"];

        W32(
            output,
            headOffset + 8,
            unchecked(
                0xB1B0AFBAu -
                Checksum(
                    output,
                    false)));

        return output;
    }

    private static uint Checksum(
        ReadOnlySpan<byte> data,
        bool zeroHead)
    {
        uint sum = 0;
        int padded =
            Align4(
                data.Length);

        Span<byte> word =
            stackalloc byte[4];

        for (int offset = 0;
            offset < padded;
            offset += 4)
        {
            word.Clear();

            if (offset <
                data.Length)
            {
                data.Slice(
                        offset,
                        Math.Min(
                            4,
                            data.Length -
                            offset))
                    .CopyTo(
                        word);
            }

            if (zeroHead &&
                offset == 8)
            {
                word.Clear();
            }

            sum =
                unchecked(
                    sum +
                    BinaryPrimitives
                        .ReadUInt32BigEndian(
                            word));
        }

        return sum;
    }

    private static int Align4(
        int value) =>
        (value + 3) &
        ~3;

    private static void W16(
        Span<byte> data,
        int offset,
        ushort value) =>
        BinaryPrimitives
            .WriteUInt16BigEndian(
                data.Slice(
                    offset,
                    2),
                value);

    private static void W32(
        Span<byte> data,
        int offset,
        uint value) =>
        BinaryPrimitives
            .WriteUInt32BigEndian(
                data.Slice(
                    offset,
                    4),
                value);
}
