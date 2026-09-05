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

public sealed class PdfExternalTrueTypeSubsetResourceProjectorTests
{
    [Fact]
    public void SubsetOnlyFaceCanBeProjectedWithToUnicode()
    {
        TestContext context =
            CreateContext(
                PdfExternalFontEmbeddingRights.SubsetOnly);

        PdfWritePlan plan =
            PdfWritePlanner.Create(
                context.Document);

        PdfExternalFontResourceProjection projection =
            PdfExternalTrueTypeSubsetResourceProjector.Create(
                plan,
                context.PageIdentifier,
                "FSub1",
                context.Face,
                context.Program,
                "A");

        Assert.Equal(
            5,
            projection.Projections.Projections.Count);

        Assert.NotNull(
            projection.ToUnicodeIdentifier);

        PdfObjectWriteProjection fontProjection =
            Assert.Single(
                projection.Projections.Projections,
                item =>
                    item.Identifier ==
                    projection.FontIdentifier);

        PdfDictionary font =
            Assert.IsType<PdfDictionary>(
                fontProjection.Value);

        Assert.IsType<PdfIndirectReference>(
            font.Get(
                PdfName.FromLatin1("ToUnicode")));

        string baseFont =
            Assert.IsType<PdfName>(
                font.Get(
                    PdfName.FromLatin1("BaseFont")))
            .ToString();

        Assert.Matches(
            "^/[A-Z]{6}\\+",
            baseFont);
    }

    [Fact]
    public void SubsetProgramIsSmallerWhenUnselectedGlyphOutlineExists()
    {
        TestContext context =
            CreateContext(
                PdfExternalFontEmbeddingRights.SubsetOnly);

        var subsetter =
            new PdfTrueTypeGlyphSubsetter();

        PdfTrueTypeSubsetResult subset =
            subsetter.CreateSubset(
                context.Program,
                "A");

        Assert.True(
            subset.Program.Bytes.Length <
            context.Program.Bytes.Length);
    }

    [Fact]
    public void CompositeSelectedGlyphIsRejected()
    {
        TestContext context =
            CreateContext(
                PdfExternalFontEmbeddingRights.SubsetOnly,
                compositeA: true);

        var subsetter =
            new PdfTrueTypeGlyphSubsetter();

        Assert.Throws<NotSupportedException>(
            () =>
                subsetter.CreateSubset(
                    context.Program,
                    "A"));
    }

    [Fact]
    public void NonWinAnsiTextIsRejectedRatherThanMisencoded()
    {
        TestContext context =
            CreateContext(
                PdfExternalFontEmbeddingRights.SubsetOnly);

        var subsetter =
            new PdfTrueTypeGlyphSubsetter();

        Assert.Throws<NotSupportedException>(
            () =>
                subsetter.CreateSubset(
                    context.Program,
                    "\u03A9"));
    }

    [Fact]
    public void FullRewriteContainsSubsetTagAndToUnicode()
    {
        TestContext context =
            CreateContext(
                PdfExternalFontEmbeddingRights.SubsetOnly);

        PdfWritePlan basePlan =
            PdfWritePlanner.Create(
                context.Document);

        PdfWritePlan projected =
            PdfExternalFontEmbeddingWritingService
                .CreateSubsetFullRewritePlan(
                    basePlan,
                    context.PageIdentifier,
                    "FSub1",
                    context.Face,
                    context.Program,
                    "A");

        using var destination =
            new MemoryStream();

        PdfFullRewriteWriter.Write(
            destination,
            projected);

        string serialized =
            Encoding.Latin1.GetString(
                destination.ToArray());

        Assert.Contains(
            "/ToUnicode",
            serialized,
            StringComparison.Ordinal);

        Assert.Contains(
            "/FSub1",
            serialized,
            StringComparison.Ordinal);
    }

    private static TestContext CreateContext(
        PdfExternalFontEmbeddingRights rights,
        bool compositeA = false)
    {
        PdfDocument document =
            PdfDocument.CreateEmpty(
                PdfVersion.Pdf20);

        var catalogId =
            new PdfObjectIdentifier(1);
        var pagesId =
            new PdfObjectIdentifier(2);
        var pageId =
            new PdfObjectIdentifier(3);

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
            BuildFont(
                rights,
                compositeA);

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
                "face",
                "Subset Test",
                "Subset Test Regular",
                400,
                5,
                PdfExternalFontSlant.Upright,
                PdfExternalFontTechnology.TrueType,
                rights);

        return new TestContext(
            document,
            pageId,
            face,
            program);
    }

    private static byte[] BuildFont(
        PdfExternalFontEmbeddingRights rights,
        bool compositeA)
    {
        byte[] glyph0 =
            SimpleGlyph(20);

        byte[] glyphA =
            compositeA
                ? CompositeGlyph()
                : SimpleGlyph(120);

        byte[] glyphB =
            SimpleGlyph(300);

        byte[] glyf =
        [
            .. glyph0,
            .. Pad4(glyph0.Length),
            .. glyphA,
            .. Pad4(glyphA.Length),
            .. glyphB
        ];

        int offset0 = 0;
        int offset1 = Align4(glyph0.Length);
        int offset2 = offset1 + Align4(glyphA.Length);
        int offset3 = offset2 + glyphB.Length;

        byte[] loca =
            new byte[16];

        W32(loca, 0, checked((uint)offset0));
        W32(loca, 4, checked((uint)offset1));
        W32(loca, 8, checked((uint)offset2));
        W32(loca, 12, checked((uint)offset3));

        var tables =
            new Dictionary<string, byte[]>(StringComparer.Ordinal)
            {
                ["OS/2"] = Os2(rights),
                ["cmap"] = Cmap(),
                ["glyf"] = glyf,
                ["head"] = Head(),
                ["hhea"] = Hhea(),
                ["hmtx"] = Hmtx(),
                ["loca"] = loca,
                ["maxp"] = Maxp(),
                ["name"] = Name()
            };

        return BuildSfnt(tables);
    }

    private static byte[] SimpleGlyph(
        int payloadBytes)
    {
        byte[] v =
            new byte[
                Math.Max(
                    10,
                    payloadBytes)];

        W16(v, 0, 1);
        return v;
    }

    private static byte[] CompositeGlyph()
    {
        byte[] v =
            new byte[10];

        W16(
            v,
            0,
            unchecked((ushort)-1));

        return v;
    }

    private static byte[] Pad4(
        int length) =>
        new byte[
            (4 -
            (length & 3)) &
            3];

    private static byte[] Head()
    {
        byte[] v = new byte[54];
        W32(v, 0, 0x00010000);
        W32(v, 12, 0x5F0F3CF5);
        W16(v, 18, 1000);
        W16(v, 50, 1);
        return v;
    }

    private static byte[] Hhea()
    {
        byte[] v = new byte[36];
        W32(v, 0, 0x00010000);
        W16(v, 4, 800);
        W16(v, 6, unchecked((ushort)-200));
        W16(v, 34, 3);
        return v;
    }

    private static byte[] Hmtx()
    {
        byte[] v = new byte[12];
        W16(v, 0, 500);
        W16(v, 4, 600);
        W16(v, 8, 700);
        return v;
    }

    private static byte[] Maxp()
    {
        byte[] v = new byte[32];
        W32(v, 0, 0x00010000);
        W16(v, 4, 3);
        return v;
    }

    private static byte[] Cmap()
    {
        byte[] format0 =
            new byte[262];

        W16(format0, 0, 0);
        W16(format0, 2, 262);
        format0[6 + 65] = 1;
        format0[6 + 66] = 2;

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
        byte[] v = new byte[6];
        W16(v, 4, 6);
        return v;
    }

    private static byte[] Os2(
        PdfExternalFontEmbeddingRights rights)
    {
        byte[] v = new byte[78];

        ushort fsType =
            rights switch
            {
                PdfExternalFontEmbeddingRights.SubsetOnly => 0x0100,
                PdfExternalFontEmbeddingRights.Restricted => 0x0002,
                _ => 0
            };

        W16(v, 8, fsType);
        return v;
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
            (ordered.Length * 16);

        int cursor =
            Align4(directory);

        var offsets =
            new Dictionary<string, int>(
                StringComparer.Ordinal);

        foreach (KeyValuePair<string, byte[]> table in ordered)
        {
            cursor =
                Align4(cursor);
            offsets[table.Key] = cursor;
            cursor +=
                Align4(
                    table.Value.Length);
        }

        byte[] output =
            new byte[cursor];

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
        W16(output, 10, checked((ushort)((ordered.Length * 16) - range)));

        for (int index = 0;
            index < ordered.Length;
            index++)
        {
            KeyValuePair<string, byte[]> table =
                ordered[index];

            int record =
                12 +
                (index * 16);

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

        int head =
            offsets["head"];

        W32(
            output,
            head + 8,
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
                        Math.Min(4, data.Length - offset))
                    .CopyTo(word);
            }

            if (zeroHead &&
                offset == 8)
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

    private static int Align4(
        int value) =>
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

