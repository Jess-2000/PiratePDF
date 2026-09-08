using System.Buffers.Binary;
using System.IO;
using System.Text;
using BlackFlag.Engine.Document.Model;
using BlackFlag.Engine.Document.Objects;
using BlackFlag.Engine.Document.Objects.Primitives;
using BlackFlag.Engine.Fonts;
using BlackFlag.Engine.Fonts.Embedding;
using BlackFlag.Engine.Fonts.Sanitization;
using BlackFlag.Engine.Services.Fonts;
using BlackFlag.Engine.Writing;

namespace BlackFlag.Engine.Services.Tests.Fonts;

public sealed class PdfExternalType0TrueTypeResourceProjectorTests
{
    [Fact]
    public void UnicodeSubsetAssignsCidsAndEncodesIdentityH()
    {
        TestContext context =
            CreateContext();

        PdfUnicodeTrueTypeSubsetResult subset =
            new PdfUnicodeTrueTypeGlyphSubsetter()
                .CreateSubset(
                    context.Program,
                    "AΩ😀");

        Assert.Equal(
            3,
            subset.CidByUnicode.Count);

        byte[] encoded =
            subset.EncodeText(
                "😀AΩ");

        Assert.Equal(
            6,
            encoded.Length);

        Assert.NotEqual(
            encoded[1],
            encoded[3]);
    }

    [Fact]
    public void Type0ProjectionCreatesCidFontToUnicodeAndCidToGidMap()
    {
        TestContext context =
            CreateContext();

        PdfWritePlan plan =
            PdfWritePlanner.Create(
                context.Document);

        PdfExternalType0TrueTypeResourceProjection projection =
            PdfExternalType0TrueTypeResourceProjector.Create(
                plan,
                context.PageIdentifier,
                "FUnicode1",
                context.Face,
                context.Program,
                "AΩ😀");

        Assert.Equal(
            7,
            projection.Projections.Projections.Count);

        PdfObjectWriteProjection type0Projection =
            Assert.Single(
                projection.Projections.Projections,
                item =>
                    item.Identifier ==
                    projection.Type0FontIdentifier);

        PdfDictionary type0 =
            Assert.IsType<PdfDictionary>(
                type0Projection.Value);

        Assert.Equal(
            "/Type0",
            Assert.IsType<PdfName>(
                type0.Get(
                    PdfName.FromLatin1("Subtype")))
                .ToString());

        Assert.Equal(
            "/Identity-H",
            Assert.IsType<PdfName>(
                type0.Get(
                    PdfName.FromLatin1("Encoding")))
                .ToString());

        Assert.IsType<PdfIndirectReference>(
            type0.Get(
                PdfName.FromLatin1("ToUnicode")));
    }

    [Fact]
    public void Type0DescendantContainsWidthsAndCidToGidMap()
    {
        TestContext context =
            CreateContext();

        PdfExternalType0TrueTypeResourceProjection projection =
            PdfExternalType0TrueTypeResourceProjector.Create(
                PdfWritePlanner.Create(
                    context.Document),
                context.PageIdentifier,
                "FUnicode1",
                context.Face,
                context.Program,
                "AΩ");

        PdfObjectWriteProjection descendantProjection =
            Assert.Single(
                projection.Projections.Projections,
                item =>
                    item.Identifier ==
                    projection.DescendantFontIdentifier);

        PdfDictionary descendant =
            Assert.IsType<PdfDictionary>(
                descendantProjection.Value);

        Assert.Equal(
            "/CIDFontType2",
            Assert.IsType<PdfName>(
                descendant.Get(
                    PdfName.FromLatin1("Subtype")))
                .ToString());

        Assert.IsType<PdfIndirectReference>(
            descendant.Get(
                PdfName.FromLatin1("CIDToGIDMap")));

        PdfArray widths =
            Assert.IsType<PdfArray>(
                descendant.Get(
                    PdfName.FromLatin1("W")));

        Assert.NotEmpty(widths);
    }

    [Fact]
    public void FullRewriteSerializesType0UnicodeFont()
    {
        TestContext context =
            CreateContext();

        PdfWritePlan projected =
            PdfExternalFontEmbeddingWritingService
                .CreateUnicodeSubsetFullRewritePlan(
                    PdfWritePlanner.Create(
                        context.Document),
                    context.PageIdentifier,
                    "FUnicode1",
                    context.Face,
                    context.Program,
                    "AΩ😀");

        using var destination =
            new MemoryStream();

        PdfFullRewriteWriter.Write(
            destination,
            projected);

        string serialized =
            Encoding.Latin1.GetString(
                destination.ToArray());

        Assert.Contains(
            "/Subtype /Type0",
            serialized,
            StringComparison.Ordinal);

        Assert.Contains(
            "/Subtype /CIDFontType2",
            serialized,
            StringComparison.Ordinal);

        Assert.Contains(
            "/Identity-H",
            serialized,
            StringComparison.Ordinal);

        Assert.Contains(
            "/CIDToGIDMap",
            serialized,
            StringComparison.Ordinal);

        Assert.Contains(
            "/Registry (Adobe)",
            serialized,
            StringComparison.Ordinal);

        Assert.Contains(
            "/Ordering (Identity)",
            serialized,
            StringComparison.Ordinal);

        Assert.Contains(
            "/ToUnicode",
            serialized,
            StringComparison.Ordinal);
    }

    [Fact]
    public void MissingUnicodeGlyphIsRejectedBeforeProjection()
    {
        TestContext context =
            CreateContext();

        Assert.Throws<NotSupportedException>(
            () =>
                PdfExternalType0TrueTypeResourceProjector.Create(
                    PdfWritePlanner.Create(
                        context.Document),
                    context.PageIdentifier,
                    "FUnicode1",
                    context.Face,
                    context.Program,
                    "Ж"));
    }

    private static TestContext CreateContext()
    {
        PdfDocument document =
            PdfDocument.CreateEmpty(
                PdfVersion.Pdf20);

        var catalogId = new PdfObjectIdentifier(1);
        var pagesId = new PdfObjectIdentifier(2);
        var pageId = new PdfObjectIdentifier(3);

        document.BindCoreStructure(
            catalogId,
            pagesId);

        document.Objects.Add(
            catalogId,
            new PdfDictionary
            {
                { PdfName.FromLatin1("Type"), PdfName.FromLatin1("Catalog") },
                { PdfName.FromLatin1("Pages"), new PdfIndirectReference(pagesId) }
            });

        document.Objects.Add(
            pagesId,
            new PdfDictionary
            {
                { PdfName.FromLatin1("Type"), PdfName.FromLatin1("Pages") },
                {
                    PdfName.FromLatin1("Kids"),
                    new PdfArray(
                    [
                        new PdfIndirectReference(pageId)
                    ])
                },
                { PdfName.FromLatin1("Count"), new PdfInteger(1) }
            });

        document.Objects.Add(
            pageId,
            new PdfDictionary
            {
                { PdfName.FromLatin1("Type"), PdfName.FromLatin1("Page") },
                { PdfName.FromLatin1("Parent"), new PdfIndirectReference(pagesId) },
                {
                    PdfName.FromLatin1("MediaBox"),
                    new PdfArray(
                    [
                        new PdfInteger(0),
                        new PdfInteger(0),
                        new PdfInteger(612),
                        new PdfInteger(792)
                    ])
                }
            });

        byte[] raw =
            BuildFont();

        using var rawStream =
            new MemoryStream(
                raw,
                writable: false);

        PdfSanitizedExternalFontProgram program =
            new PdfOpenTypeFontSanitizer()
                .Sanitize(
                    rawStream);

        var face =
            new PdfExternalFontFace(
                "test",
                "unicode-face",
                "Unicode Test",
                "Unicode Test Regular",
                400,
                5,
                PdfExternalFontSlant.Upright,
                PdfExternalFontTechnology.TrueType,
                PdfExternalFontEmbeddingRights.SubsetOnly);

        return new TestContext(
            document,
            pageId,
            face,
            program);
    }

    internal static byte[] BuildFont(ushort fsType = 0x0100)
    {
        byte[][] glyphs =
        [
            SimpleGlyph(24),
            SimpleGlyph(100),
            SimpleGlyph(120),
            SimpleGlyph(140),
            SimpleGlyph(400)
        ];

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
                ["OS/2"] = Os2(fsType),
                ["cmap"] = Cmap(),
                ["glyf"] = glyf,
                ["head"] = Head(),
                ["hhea"] = Hhea(glyphs.Length),
                ["hmtx"] = Hmtx(glyphs.Length),
                ["loca"] = loca,
                ["maxp"] = Maxp(glyphs.Length),
                ["name"] = Name()
            };

        return BuildSfnt(
            tables);
    }

    private static byte[] Cmap()
    {
        byte[] format12 =
            new byte[
                16 +
                3 * 12];

        W16(format12, 0, 12);
        W16(format12, 2, 0);
        W32(
            format12,
            4,
            checked(
                (uint)format12.Length));
        W32(format12, 8, 0);
        W32(format12, 12, 3);

        // A -> GID 1
        W32(format12, 16, 0x0041);
        W32(format12, 20, 0x0041);
        W32(format12, 24, 1);

        // Omega -> GID 2
        W32(format12, 28, 0x03A9);
        W32(format12, 32, 0x03A9);
        W32(format12, 36, 2);

        // Grinning face -> GID 3
        W32(format12, 40, 0x1F600);
        W32(format12, 44, 0x1F600);
        W32(format12, 48, 3);

        byte[] cmap =
            new byte[
                12 +
                format12.Length];

        W16(cmap, 0, 0);
        W16(cmap, 2, 1);
        W16(cmap, 4, 3);
        W16(cmap, 6, 10);
        W32(cmap, 8, 12);

        format12.CopyTo(
            cmap.AsSpan(12));

        return cmap;
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

    private static byte[] SimpleGlyph(int length)
    {
        byte[] value =
            new byte[
                Math.Max(10, length)];

        W16(value, 0, 1);
        return value;
    }

    private static byte[] Head()
    {
        byte[] value = new byte[54];
        W32(value, 0, 0x00010000);
        W32(value, 12, 0x5F0F3CF5);
        W16(value, 18, 1000);
        W16(value, 50, 1);
        return value;
    }

    private static byte[] Hhea(int glyphCount)
    {
        byte[] value = new byte[36];
        W32(value, 0, 0x00010000);
        W16(value, 4, 800);
        W16(value, 6, unchecked((ushort)-200));
        W16(value, 34, checked((ushort)glyphCount));
        return value;
    }

    private static byte[] Hmtx(int glyphCount)
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
                        index * 50)));
        }

        return value;
    }

    private static byte[] Maxp(int glyphCount)
    {
        byte[] value = new byte[32];
        W32(value, 0, 0x00010000);
        W16(value, 4, checked((ushort)glyphCount));
        return value;
    }

    private static byte[] Name()
    {
        byte[] value = new byte[6];
        W16(value, 4, 6);
        return value;
    }

    private static byte[] Os2(ushort fsType)
    {
        byte[] value = new byte[78];
        W16(value, 8, fsType);
        return value;
    }

    private static byte[] BuildSfnt(
        IReadOnlyDictionary<string, byte[]> tables)
    {
        KeyValuePair<string, byte[]>[] ordered =
        [
            .. tables.OrderBy(
                static pair => pair.Key,
                StringComparer.Ordinal)
        ];

        int directory =
            12 +
            ordered.Length * 16;

        int cursor =
            Align4(directory);

        var offsets =
            new Dictionary<string, int>(
                StringComparer.Ordinal);

        foreach (KeyValuePair<string, byte[]> table in ordered)
        {
            cursor = Align4(cursor);
            offsets[table.Key] = cursor;
            cursor += Align4(table.Value.Length);
        }

        byte[] output = new byte[cursor];

        W32(output, 0, 0x00010000);
        W16(output, 4, checked((ushort)ordered.Length));

        int power = 1;
        int selector = 0;

        while ((power << 1) <= ordered.Length)
        {
            power <<= 1;
            selector++;
        }

        int range = power * 16;

        W16(output, 6, checked((ushort)range));
        W16(output, 8, checked((ushort)selector));
        W16(
            output,
            10,
            checked((ushort)(ordered.Length * 16 - range)));

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

            Encoding.ASCII.GetBytes(table.Key)
                .CopyTo(
                    output.AsSpan(record, 4));

            table.Value.CopyTo(
                output.AsSpan(offset));

            if (table.Key == "head")
            {
                output.AsSpan(offset + 8, 4).Clear();
            }

            W32(
                output,
                record + 4,
                Checksum(
                    output.AsSpan(
                        offset,
                        table.Value.Length),
                    table.Key == "head"));

            W32(output, record + 8, checked((uint)offset));
            W32(output, record + 12, checked((uint)table.Value.Length));
        }

        int headOffset =
            offsets["head"];

        W32(
            output,
            headOffset + 8,
            unchecked(
                0xB1B0AFBAu -
                Checksum(output, false)));

        return output;
    }

    private static uint Checksum(
        ReadOnlySpan<byte> data,
        bool zeroHead)
    {
        uint sum = 0;
        int padded = Align4(data.Length);
        Span<byte> word = stackalloc byte[4];

        for (int offset = 0;
            offset < padded;
            offset += 4)
        {
            word.Clear();

            if (offset < data.Length)
            {
                data.Slice(
                        offset,
                        Math.Min(
                            4,
                            data.Length - offset))
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

    private static void W16(
        Span<byte> data,
        int offset,
        ushort value) =>
        BinaryPrimitives.WriteUInt16BigEndian(
            data.Slice(offset, 2),
            value);

    private static void W32(
        Span<byte> data,
        int offset,
        uint value) =>
        BinaryPrimitives.WriteUInt32BigEndian(
            data.Slice(offset, 4),
            value);

    private sealed record TestContext(
        PdfDocument Document,
        PdfObjectIdentifier PageIdentifier,
        PdfExternalFontFace Face,
        PdfSanitizedExternalFontProgram Program);
}
