using System.Buffers.Binary;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using BlackFlag.Engine.Document.Objects;
using BlackFlag.Engine.Document.Objects.Primitives;
using BlackFlag.Engine.Fonts;
using BlackFlag.Engine.Fonts.Embedding;
using BlackFlag.Engine.Fonts.Sanitization;
using BlackFlag.Engine.Services.Fonts;
using BlackFlag.Engine.Services.Tests.Text;
using BlackFlag.Engine.Writing;

namespace BlackFlag.Engine.Services.Tests.Fonts;

public sealed class PdfSanitizedFontOwnershipIntegrationTests
{
    [Theory]
    [InlineData("full", false)]
    [InlineData("full", true)]
    [InlineData("subset", false)]
    [InlineData("subset", true)]
    [InlineData("type0", false)]
    [InlineData("type0", true)]
    public void PublicMutationCannotChangeMetadataSubsetOrEmbeddedFont(string kind, bool streamXref)
    {
        var saved = CreateSavedDocument(kind, streamXref);
        Assert.Contains("/FontFile2", Encoding.Latin1.GetString(saved.Pdf), StringComparison.Ordinal);
        Assert.NotEmpty(saved.Font);
    }

    // The supplemental parser executable uses this fixture without adding a Parsing project reference.
    public static (byte[] Pdf, byte[] Font, int FontObjectNumber) CreateSavedDocument(string kind, bool streamXref)
    {
        using var context = new PdfFontCollisionTestDocument("direct");
        using var input = new MemoryStream(PdfExternalType0TrueTypeResourceProjectorTests.BuildFont(
            fsType: 0, nameTable: CreateNameTable()), writable: false);
        var program = new PdfOpenTypeFontSanitizer().Sanitize(input);
        byte[] original = program.Bytes.ToArray();
        var metadata = PdfSanitizedExternalFontMetadataInspector.Inspect(program);
        var metrics = PdfExternalTrueTypeInspector.Inspect(program);
        byte[] expectedFont = kind switch
        {
            "full" => original,
            "subset" => new PdfTrueTypeGlyphSubsetter().CreateSubset(program, "A").Program.Bytes.ToArray(),
            "type0" => new PdfUnicodeTrueTypeGlyphSubsetter().CreateSubset(program, "A\u03A9\U0001F600").Program.Bytes.ToArray(),
            _ => throw new ArgumentOutOfRangeException(nameof(kind))
        };
        ReadOnlyMemory<byte> retained = program.Bytes;
        using Stream retainedReader = program.OpenRead();
        if (MemoryMarshal.TryGetArray(retained, out ArraySegment<byte> array))
        {
            array.Array![array.Offset] ^= 0xFF;
        }
        MemoryMarshal.AsMemory(retained).Span.Fill(0xFF);

        var currentMetadata = PdfSanitizedExternalFontMetadataInspector.Inspect(program);
        var currentMetrics = PdfExternalTrueTypeInspector.Inspect(program);
        Assert.Equal(metadata.FamilyName, currentMetadata.FamilyName);
        Assert.Equal(metadata.FaceName, currentMetadata.FaceName);
        Assert.Equal(metadata.EmbeddingRights, currentMetadata.EmbeddingRights);
        foreach (Rune rune in "A\u03A9\U0001F600".EnumerateRunes())
        {
            Assert.True(metadata.SupportsRune(rune));
            Assert.True(currentMetadata.SupportsRune(rune));
        }
        Assert.Equal(metrics.UnitsPerEm, currentMetrics.UnitsPerEm);
        Assert.Equal(metrics.WinAnsiWidths, currentMetrics.WinAnsiWidths);
        Assert.Equal(metrics.EmbeddingRights, currentMetrics.EmbeddingRights);

        var options = new PdfWriteOptions
        {
            CrossReferenceMode = streamXref
                ? PdfCrossReferenceWriteMode.CrossReferenceStream
                : PdfCrossReferenceWriteMode.TraditionalTable
        };
        var plan = PdfWritePlanner.Create(context.Document, options);
        PdfObjectWriteProjectionSet projections;
        PdfObjectIdentifier fontIdentifier;
        if (kind == "type0")
        {
            var projection = PdfExternalType0TrueTypeResourceProjector.Create(
                plan, PdfFilteredTextTestDocument.PageId, "FExt1", context.Face, program, "A\u03A9\U0001F600");
            projections = projection.Projections;
            fontIdentifier = projection.Type0FontIdentifier;
        }
        else
        {
            var projection = kind == "full"
                ? PdfExternalTrueTypeResourceProjector.Create(
                    plan, PdfFilteredTextTestDocument.PageId, "FExt1", context.Face, program)
                : PdfExternalTrueTypeSubsetResourceProjector.Create(
                    plan, PdfFilteredTextTestDocument.PageId, "FExt1", context.Face, program, "A");
            projections = projection.Projections;
            fontIdentifier = projection.FontIdentifier;
        }

        context.AssertOriginalGraph();
        projections.ApplyTo(context.Document.Objects);
        PdfExternalFontResourceCollisionTests.AssertMapping(context, "FExt2", fontIdentifier);
        context.AssertUnrelatedObjectsUnchanged();
        var font = context.Document.Objects.Get<PdfDictionary>(fontIdentifier);
        if (kind == "type0")
        {
            var descendant = font.Get<PdfArray>(Name("DescendantFonts")).Get<PdfIndirectReference>(0);
            font = context.Document.Objects.Get<PdfDictionary>(descendant.Identifier);
        }
        var descriptor = context.Document.Objects.Get<PdfDictionary>(
            font.Get<PdfIndirectReference>(Name("FontDescriptor")).Identifier);
        var embedded = context.Document.Objects.Get<PdfStream>(
            descriptor.Get<PdfIndirectReference>(Name("FontFile2")).Identifier);
        Assert.Equal(expectedFont, embedded.ToByteArray());
        Assert.Equal(expectedFont.Length, embedded.Dictionary.Get<PdfInteger>(Name("Length1")).Value);

        // Retain and mutate the old public result again after all downstream work.
        MemoryMarshal.AsMemory(retained).Span.Clear();
        Assert.Equal(original, program.Bytes.ToArray());
        Assert.Equal(Convert.ToHexString(SHA256.HashData(program.Bytes.Span)), program.Sha256);
        using var read = new MemoryStream();
        retainedReader.CopyTo(read);
        Assert.Equal(original, read.ToArray());
        using var output = new MemoryStream();
        PdfFullRewriteWriter.Write(output, PdfWritePlanner.Create(context.Document, options));
        return (output.ToArray(), expectedFont, fontIdentifier.ObjectNumber);
    }

    private static byte[] CreateNameTable()
    {
        byte[] family = Encoding.BigEndianUnicode.GetBytes("Ownership Test");
        byte[] table = new byte[18 + family.Length];
        BinaryPrimitives.WriteUInt16BigEndian(table.AsSpan(2), 1);
        BinaryPrimitives.WriteUInt16BigEndian(table.AsSpan(4), 18);
        BinaryPrimitives.WriteUInt16BigEndian(table.AsSpan(6), 3);
        BinaryPrimitives.WriteUInt16BigEndian(table.AsSpan(8), 1);
        BinaryPrimitives.WriteUInt16BigEndian(table.AsSpan(10), 0x0409);
        BinaryPrimitives.WriteUInt16BigEndian(table.AsSpan(12), 1);
        BinaryPrimitives.WriteUInt16BigEndian(table.AsSpan(14), checked((ushort)family.Length));
        family.CopyTo(table, 18);
        return table;
    }

    private static PdfName Name(string value) => PdfName.FromLatin1(value);
}
