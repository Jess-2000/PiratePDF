using System.Buffers.Binary;
using System.IO;
using System.Text;
using BlackFlag.Engine.Document.Model;
using BlackFlag.Engine.Document.Objects;
using BlackFlag.Engine.Document.Objects.Primitives;
using BlackFlag.Engine.Fonts;
using BlackFlag.Engine.Fonts.Sanitization;
using BlackFlag.Engine.Services.Fonts;
using BlackFlag.Engine.Writing;

namespace BlackFlag.Engine.Services.Tests.Fonts;

public sealed class PdfExternalTrueTypeResourceProjectorTests
{
    [Fact]
    public void ProjectionCreatesFontDescriptorProgramAndPageResource()
    {
        TestContext context = CreateContext();

        PdfWritePlan plan =
            PdfWritePlanner.Create(
                context.Document);

        PdfExternalFontResourceProjection projection =
            PdfExternalTrueTypeResourceProjector.Create(
                plan,
                context.PageIdentifier,
                "FExt1",
                context.Face,
                context.Program);

        Assert.Equal(4, projection.Projections.Projections.Count);
        Assert.Equal("FExt1", projection.ResourceName);

        PdfObjectWriteProjection pageProjection =
            Assert.Single(projection.Projections.Projections, item =>
                        item.Identifier ==
                        context.PageIdentifier);

        PdfDictionary page =
            Assert.IsType<PdfDictionary>(
                pageProjection.Value);

        PdfDictionary resources =
            Assert.IsType<PdfDictionary>(
                page.Get(
                    PdfName.FromLatin1("Resources")));

        PdfDictionary fonts =
            Assert.IsType<PdfDictionary>(
                resources.Get(
                    PdfName.FromLatin1("Font")));

        PdfIndirectReference reference =
            Assert.IsType<PdfIndirectReference>(
                fonts.Get(
                    PdfName.FromLatin1("FExt1")));

        Assert.Equal(
            projection.FontIdentifier,
            reference.Identifier);
    }

    [Fact]
    public void FullRewriteContainsEmbeddedFontProgram()
    {
        TestContext context = CreateContext();

        PdfWritePlan basePlan =
            PdfWritePlanner.Create(
                context.Document);

        PdfWritePlan projected =
            PdfExternalFontEmbeddingWritingService.CreateFullRewritePlan(
                basePlan,
                context.PageIdentifier,
                "FExt1",
                context.Face,
                context.Program);

        using var destination =
            new MemoryStream();

        PdfFullRewriteWriter.Write(
            destination,
            projected);

        string serialized =
            Encoding.Latin1.GetString(
                destination.ToArray());

        Assert.Contains("/Subtype /TrueType", serialized, StringComparison.Ordinal);
        Assert.Contains("/FontFile2", serialized, StringComparison.Ordinal);
        Assert.Contains("/FExt1", serialized, StringComparison.Ordinal);
    }

    [Fact]
    public void ProgramFsTypeOverridesProviderPermission()
    {
        TestContext context =
            CreateContext(
                restrictedProgram: true);

        PdfWritePlan plan =
            PdfWritePlanner.Create(
                context.Document);

        Assert.Throws<NotSupportedException>(
            () =>
                PdfExternalTrueTypeResourceProjector.Create(
                    plan,
                    context.PageIdentifier,
                    "FExt1",
                    context.Face,
                    context.Program));
    }

    [Fact]
    public void SubsetOnlyProviderPermissionIsNotFullEmbedded()
    {
        TestContext context =
            CreateContext(
                providerRights:
                    PdfExternalFontEmbeddingRights.SubsetOnly);

        PdfWritePlan plan =
            PdfWritePlanner.Create(
                context.Document);

        Assert.Throws<NotSupportedException>(
            () =>
                PdfExternalTrueTypeResourceProjector.Create(
                    plan,
                    context.PageIdentifier,
                    "FExt1",
                    context.Face,
                    context.Program));
    }

    [Fact]
    public void IndirectPageResourcesAreCopiedOntoPageBeforeFontMutation()
    {
        TestContext context =
            CreateContext(
                indirectResources: true);

        PdfWritePlan plan =
            PdfWritePlanner.Create(
                context.Document);

        PdfExternalFontResourceProjection projection =
            PdfExternalTrueTypeResourceProjector.Create(
                plan,
                context.PageIdentifier,
                "FExt1",
                context.Face,
                context.Program);

        PdfObjectWriteProjection pageProjection =
            Assert.Single(
                projection.Projections.Projections,
                item =>
                    item.Identifier ==
                    context.PageIdentifier);

        PdfDictionary page =
            Assert.IsType<PdfDictionary>(
                pageProjection.Value);

        PdfDictionary resources =
            Assert.IsType<PdfDictionary>(
                page.Get(
                    PdfName.FromLatin1("Resources")));

        PdfDictionary fonts =
            Assert.IsType<PdfDictionary>(
                resources.Get(
                    PdfName.FromLatin1("Font")));

        Assert.True(
            fonts.ContainsKey(
                PdfName.FromLatin1(
                    projection.ResourceName)));
    }

    private static TestContext CreateContext(
        PdfExternalFontEmbeddingRights providerRights =
            PdfExternalFontEmbeddingRights.Allowed,
        bool restrictedProgram = false,
        bool indirectResources = false)
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
                {
                    PdfName.FromLatin1("Type"),
                    PdfName.FromLatin1("Catalog")
                },
                {
                    PdfName.FromLatin1("Pages"),
                    new PdfIndirectReference(pagesId)
                }
            });

        document.Objects.Add(
            pagesId,
            new PdfDictionary
            {
                {
                    PdfName.FromLatin1("Type"),
                    PdfName.FromLatin1("Pages")
                },
                {
                    PdfName.FromLatin1("Kids"),
                    new PdfArray(
                    [
                        new PdfIndirectReference(pageId)
                    ])
                },
                {
                    PdfName.FromLatin1("Count"),
                    new PdfInteger(1)
                }
            });

        var pageDictionary =
            new PdfDictionary
            {
                {
                    PdfName.FromLatin1("Type"),
                    PdfName.FromLatin1("Page")
                },
                {
                    PdfName.FromLatin1("Parent"),
                    new PdfIndirectReference(pagesId)
                },
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
            };

        if (indirectResources)
        {
            var resourcesId =
                new PdfObjectIdentifier(4);

            document.Objects.Add(
                resourcesId,
                new PdfDictionary());

            pageDictionary.Add(
                PdfName.FromLatin1("Resources"),
                new PdfIndirectReference(
                    resourcesId));
        }

        document.Objects.Add(
            pageId,
            pageDictionary);

        byte[] rawFont =
            BuildTrueType(
                restrictedProgram);

        using var raw =
            new MemoryStream(
                rawFont,
                writable: false);

        PdfSanitizedExternalFontProgram program =
            new PdfOpenTypeFontSanitizer()
                .Sanitize(raw);

        var face =
            new PdfExternalFontFace(
                "test",
                "face",
                "Test Sans",
                "Test Sans Regular",
                400,
                5,
                PdfExternalFontSlant.Upright,
                PdfExternalFontTechnology.TrueType,
                providerRights);

        return new TestContext(
            document,
            pageId,
            face,
            program);
    }

    private static byte[] BuildTrueType(
        bool restricted)
    {
        var tables =
            new Dictionary<string, byte[]>(StringComparer.Ordinal)
            {
                ["OS/2"] = Os2(restricted),
                ["cmap"] = Cmap(),
                ["glyf"] = [],
                ["head"] = Head(),
                ["hhea"] = Hhea(),
                ["hmtx"] = Hmtx(),
                ["loca"] = Loca(),
                ["maxp"] = Maxp(),
                ["name"] = Name()
            };

        KeyValuePair<string, byte[]>[] ordered =
        [
            .. tables.OrderBy(
                static pair => pair.Key,
                StringComparer.Ordinal)
        ];

        int directoryLength =
            12 + (ordered.Length * 16);
        int cursor =
            Align4(directoryLength);

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

        U32(result, 0, 0x00010000);
        U16(result, 4, checked((ushort)ordered.Length));
        Search(result, ordered.Length);

        for (int index = 0; index < ordered.Length; index++)
        {
            KeyValuePair<string, byte[]> table =
                ordered[index];

            int record =
                12 + (index * 16);
            int offset =
                offsets[table.Key];

            Encoding.ASCII.GetBytes(
                    table.Key)
                .CopyTo(
                    result.AsSpan(
                        record,
                        4));

            table.Value.CopyTo(
                result.AsSpan(offset));

            if (table.Key == "head")
            {
                result.AsSpan(
                        offset + 8,
                        4)
                    .Clear();
            }

            U32(
                result,
                record + 4,
                Checksum(
                    result.AsSpan(
                        offset,
                        table.Value.Length),
                    table.Key == "head"));

            U32(
                result,
                record + 8,
                checked((uint)offset));

            U32(
                result,
                record + 12,
                checked((uint)table.Value.Length));
        }

        int head =
            offsets["head"];

        U32(
            result,
            head + 8,
            unchecked(
                0xB1B0AFBAu -
                Checksum(result, false)));

        return result;
    }

    private static byte[] Head()
    {
        byte[] v = new byte[54];
        U32(v, 0, 0x00010000);
        U32(v, 12, 0x5F0F3CF5);
        U16(v, 18, 1000);
        I16(v, 36, -50);
        I16(v, 38, -200);
        I16(v, 40, 1000);
        I16(v, 42, 900);
        U16(v, 50, 0);
        return v;
    }

    private static byte[] Hhea()
    {
        byte[] v = new byte[36];
        U32(v, 0, 0x00010000);
        I16(v, 4, 800);
        I16(v, 6, -200);
        U16(v, 34, 1);
        return v;
    }

    private static byte[] Hmtx()
    {
        byte[] v = new byte[4];
        U16(v, 0, 500);
        return v;
    }

    private static byte[] Maxp()
    {
        byte[] v = new byte[32];
        U32(v, 0, 0x00010000);
        U16(v, 4, 1);
        return v;
    }

    private static byte[] Loca() => new byte[4];

    private static byte[] Cmap()
    {
        byte[] v = new byte[4 + 8 + 262];
        U16(v, 0, 0);
        U16(v, 2, 1);
        U16(v, 4, 3);
        U16(v, 6, 1);
        U32(v, 8, 12);
        U16(v, 12, 0);
        U16(v, 14, 262);
        U16(v, 16, 0);
        return v;
    }

    private static byte[] Name()
    {
        byte[] v = new byte[6];
        U16(v, 4, 6);
        return v;
    }

    private static byte[] Os2(
        bool restricted)
    {
        byte[] v = new byte[78];
        U16(
            v,
            8,
            restricted
                ? (ushort)0x0002
                : (ushort)0);
        return v;
    }

    private static void Search(
        Span<byte> v,
        int count)
    {
        int power = 1;
        int selector = 0;

        while ((power << 1) <= count)
        {
            power <<= 1;
            selector++;
        }

        int range = power * 16;
        U16(v, 6, checked((ushort)range));
        U16(v, 8, checked((ushort)selector));
        U16(v, 10, checked((ushort)((count * 16) - range)));
    }

    private static uint Checksum(
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
                        Math.Min(
                            4,
                            data.Length - offset))
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
                    BinaryPrimitives
                        .ReadUInt32BigEndian(word));
        }

        return sum;
    }

    private static int Align4(int v) =>
        (v + 3) & ~3;

    private static void U16(
        Span<byte> v,
        int o,
        ushort x) =>
        BinaryPrimitives.WriteUInt16BigEndian(
            v.Slice(o, 2),
            x);

    private static void I16(
        Span<byte> v,
        int o,
        short x) =>
        U16(
            v,
            o,
            unchecked((ushort)x));

    private static void U32(
        Span<byte> v,
        int o,
        uint x) =>
        BinaryPrimitives.WriteUInt32BigEndian(
            v.Slice(o, 4),
            x);

    private sealed record TestContext(
        PdfDocument Document,
        PdfObjectIdentifier PageIdentifier,
        PdfExternalFontFace Face,
        PdfSanitizedExternalFontProgram Program);
}