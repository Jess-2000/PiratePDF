using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using BlackFlag.Engine.Fonts.Embedding;
using BlackFlag.Engine.Fonts.Sanitization;
using Xunit;

namespace BlackFlag.Engine.Fonts.Tests;

public sealed class PdfTrueTypeGlyphSubsetterCompositeTests
{
    [Fact]
    public void NestedCompositeClosureIsRetainedAndResanitized()
    {
        PdfSanitizedExternalFontProgram program =
            CreateProgram(
                [
                    SimpleGlyph(),
                    CompositeGlyph(
                        (0x0002, 2)),
                    CompositeGlyph(
                        (0x0002, 3)),
                    SimpleGlyph(96),
                    SimpleGlyph(320)
                ]);

        var subsetter =
            new PdfTrueTypeGlyphSubsetter();

        PdfTrueTypeSubsetResult result =
            subsetter.CreateSubset(
                program,
                "A");

        Assert.Equal(
            PdfExternalFontTechnology.TrueType,
            result.Program.Technology);

        Assert.True(
            result.Program.Bytes.Length <
            program.Bytes.Length);
    }

    [Fact]
    public void CompositeCycleIsRejected()
    {
        PdfSanitizedExternalFontProgram program =
            CreateProgram(
                [
                    SimpleGlyph(),
                    CompositeGlyph(
                        (0x0002, 2)),
                    CompositeGlyph(
                        (0x0002, 1))
                ]);

        var subsetter =
            new PdfTrueTypeGlyphSubsetter();

        InvalidDataException exception =
            Assert.Throws<InvalidDataException>(
                () =>
                    subsetter.CreateSubset(
                        program,
                        "A"));

        Assert.Contains(
            "cycle",
            exception.Message,
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void CompositeReferenceOutsideGlyphRangeIsRejected()
    {
        PdfSanitizedExternalFontProgram program =
            CreateProgram(
                [
                    SimpleGlyph(),
                    CompositeGlyph(
                        (0x0002, 99)),
                    SimpleGlyph()
                ]);

        var subsetter =
            new PdfTrueTypeGlyphSubsetter();

        InvalidDataException exception =
            Assert.Throws<InvalidDataException>(
                () =>
                    subsetter.CreateSubset(
                        program,
                        "A"));

        Assert.Contains(
            "outside",
            exception.Message,
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void CompositeDepthLimitIsEnforced()
    {
        PdfSanitizedExternalFontProgram program =
            CreateProgram(
                [
                    SimpleGlyph(),
                    CompositeGlyph((0x0002, 2)),
                    CompositeGlyph((0x0002, 3)),
                    CompositeGlyph((0x0002, 4)),
                    SimpleGlyph()
                ]);

        var subsetter =
            new PdfTrueTypeGlyphSubsetter();

        Assert.Throws<InvalidDataException>(
            () =>
                subsetter.CreateSubset(
                    program,
                    "A",
                    new PdfTrueTypeSubsetOptions
                    {
                        MaximumCompositeDepth = 2
                    }));
    }

    [Fact]
    public void ComponentCountLimitIsEnforced()
    {
        PdfSanitizedExternalFontProgram program =
            CreateProgram(
                [
                    SimpleGlyph(),
                    CompositeGlyph(
                        (0x0022, 2),
                        (0x0002, 3)),
                    SimpleGlyph(),
                    SimpleGlyph()
                ]);

        var subsetter =
            new PdfTrueTypeGlyphSubsetter();

        Assert.Throws<InvalidDataException>(
            () =>
                subsetter.CreateSubset(
                    program,
                    "A",
                    new PdfTrueTypeSubsetOptions
                    {
                        MaximumComponentsPerGlyph = 1
                    }));
    }

    [Fact]
    public void RetainedGlyphLimitCountsDependencies()
    {
        PdfSanitizedExternalFontProgram program =
            CreateProgram(
                [
                    SimpleGlyph(),
                    CompositeGlyph((0x0002, 2)),
                    SimpleGlyph()
                ]);

        var subsetter =
            new PdfTrueTypeGlyphSubsetter();

        Assert.Throws<InvalidDataException>(
            () =>
                subsetter.CreateSubset(
                    program,
                    "A",
                    new PdfTrueTypeSubsetOptions
                    {
                        MaximumRetainedGlyphs = 2
                    }));
    }

    private static PdfSanitizedExternalFontProgram CreateProgram(
        params byte[][] glyphs)
    {
        byte[] glyf =
            BuildGlyf(
                glyphs,
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
                ["OS/2"] = Os2(),
                ["cmap"] = Cmap(),
                ["glyf"] = glyf,
                ["head"] = Head(),
                ["hhea"] = Hhea(glyphs.Length),
                ["hmtx"] = Hmtx(glyphs.Length),
                ["loca"] = loca,
                ["maxp"] = Maxp(glyphs.Length),
                ["name"] = Name()
            };

        byte[] raw =
            BuildSfnt(
                tables);

        using var stream =
            new MemoryStream(
                raw,
                writable: false);

        return new PdfOpenTypeFontSanitizer()
            .Sanitize(
                stream);
    }

    private static byte[] BuildGlyf(
        byte[][] glyphs,
        out uint[] offsets)
    {
        using var destination =
            new MemoryStream();

        offsets =
            new uint[
                glyphs.Length + 1];

        for (int index = 0;
            index < glyphs.Length;
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

        offsets[glyphs.Length] =
            checked(
                (uint)destination.Length);

        return destination.ToArray();
    }

    private static byte[] SimpleGlyph(
        int length = 32)
    {
        byte[] value =
            new byte[
                Math.Max(
                    10,
                    length)];

        W16(
            value,
            0,
            1);

        return value;
    }

    private static byte[] CompositeGlyph(
        params (ushort Flags, ushort Glyph)[] components)
    {
        if (components.Length == 0)
        {
            throw new ArgumentException(
                "A composite glyph requires at least one component.",
                nameof(components));
        }

        using var destination =
            new MemoryStream();

        byte[] header =
            new byte[10];

        W16(
            header,
            0,
            unchecked((ushort)-1));

        destination.Write(
            header);

        Span<byte> record =
            stackalloc byte[6];

        for (int index = 0;
    index < components.Length;
    index++)
        {
            (ushort requestedFlags, ushort glyph) =
                components[index];

            ushort flags =
                requestedFlags;

            if (index <
                components.Length - 1)
            {
                flags |= 0x0020;
            }
            else
            {
                flags =
                    checked(
                        (ushort)(
                            flags &
                            ~0x0020));
            }

            W16(
                record,
                0,
                flags);

            W16(
                record,
                2,
                glyph);

            // Byte-sized arguments: x = 0, y = 0.
            record[4] = 0;
            record[5] = 0;

            destination.Write(
                record);
        }

        return destination.ToArray();
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

    private static byte[] Hhea(
        int glyphCount)
    {
        byte[] value =
            new byte[36];

        W32(value, 0, 0x00010000);
        W16(value, 4, 800);
        W16(
            value,
            6,
            unchecked((ushort)-200));

        W16(
            value,
            34,
            checked((ushort)glyphCount));

        return value;
    }

    private static byte[] Hmtx(
        int glyphCount)
    {
        byte[] value =
            new byte[
                glyphCount * 4];

        for (int index = 0;
            index < glyphCount;
            index++)
        {
            W16(
                value,
                index * 4,
                checked(
                    (ushort)(
                        500 +
                        index * 10)));
        }

        return value;
    }

    private static byte[] Maxp(
        int glyphCount)
    {
        byte[] value =
            new byte[32];

        W32(value, 0, 0x00010000);
        W16(
            value,
            4,
            checked((ushort)glyphCount));

        return value;
    }

    private static byte[] Cmap()
    {
        byte[] format0 =
            new byte[262];

        W16(format0, 0, 0);
        W16(format0, 2, 262);

        // U+0041 -> glyph 1.
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

    private static byte[] Name()
    {
        byte[] value =
            new byte[6];

        W16(
            value,
            4,
            6);

        return value;
    }

    private static byte[] Os2()
    {
        byte[] value =
            new byte[78];

        W16(
            value,
            8,
            0x0100);

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

        int directoryLength =
            checked(
                12 +
                ordered.Length * 16);

        int cursor =
            Align4(
                directoryLength);

        var offsets =
            new Dictionary<string, int>(
                StringComparer.Ordinal);

        foreach (KeyValuePair<string, byte[]> table
                in ordered)
        {
            cursor =
                Align4(
                    cursor);

            offsets[table.Key] =
                cursor;

            cursor =
                checked(
                    cursor +
                    Align4(
                        table.Value.Length));
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

        int searchRange =
            power * 16;

        W16(
            output,
            6,
            checked(
                (ushort)searchRange));

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
                    searchRange)));

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

            Encoding.ASCII.GetBytes(
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
        checked(
            (value + 3) &
            ~3);

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
