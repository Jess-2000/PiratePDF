using System.Buffers.Binary;
using System.IO;
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

public sealed partial class PdfExternalFontEmbeddingRightsTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void PermittedExternalTextChangeSavesItsCollisionResolvedTf(bool streamXref)
    {
        var saved = CreateEditedDocument(streamXref);
        Assert.NotEmpty(saved.Pdf);
    }

    public static (byte[] Pdf, byte[] Font, int FontObjectNumber) CreateEditedDocument(bool streamXref)
    {
        using var context = new PdfFontCollisionTestDocument("direct");
        var text = context.Text;
        using var transaction = text.Session.BeginTransaction();
        var operation = BlackFlag.Engine.Services.Text.PdfExternalExistingTextFontService.ChangeFont(
            text.Session, text.Session.Document.Pages[0], text.Source, text.Descriptor, Match(8), new PdfOpenTypeFontSanitizer());
        Assert.Equal("FExt2", operation.ResourceName);
        _ = transaction.Commit();
        var page = context.Document.Objects.Get<PdfDictionary>(PdfFilteredTextTestDocument.PageId);
        var fontId = page.Get<PdfDictionary>(Name("Resources")).Get<PdfDictionary>(Name("Font"))
            .Get<PdfIndirectReference>(Name("FExt2")).Identifier;
        var font = context.Document.Objects.Get<PdfDictionary>(fontId);
        var descendant = context.Document.Objects.Get<PdfDictionary>(font.Get<PdfArray>(Name("DescendantFonts")).Get<PdfIndirectReference>(0).Identifier);
        var descriptor = context.Document.Objects.Get<PdfDictionary>(descendant.Get<PdfIndirectReference>(Name("FontDescriptor")).Identifier);
        var embedded = context.Document.Objects.Get<PdfStream>(descriptor.Get<PdfIndirectReference>(Name("FontFile2")).Identifier);
        // Resource-change operations already publish the coherent object graph.
        // The generic EditingSaveService does not expose this operation domain;
        // use the established full-rewrite writer without broadening that API.
        using var output = new MemoryStream();
        PdfFullRewriteWriter.Write(output, PdfWritePlanner.Create(context.Document,
            new PdfWriteOptions { CrossReferenceMode = streamXref
                ? PdfCrossReferenceWriteMode.CrossReferenceStream : PdfCrossReferenceWriteMode.TraditionalTable }));
        return (output.ToArray(), embedded.ToByteArray(), fontId.ObjectNumber);
    }

    [Theory]
    [InlineData("full", 0x100, false)]
    [InlineData("full", 0x100, true)]
    [InlineData("full", 0x108, false)]
    [InlineData("full", 0x108, true)]
    [InlineData("full", 8, false)]
    [InlineData("full", 8, true)]
    [InlineData("subset", 8, false)]
    [InlineData("subset", 8, true)]
    [InlineData("type0", 8, false)]
    [InlineData("type0", 8, true)]
    public void PermittedProjectionPreservesFontFlagsMappingsAndCanonicalName(string operation, ushort flags, bool streamXref)
    {
        var saved = CreateSavedDocument(operation, flags, streamXref);
        Assert.NotEmpty(saved.Pdf);
        Assert.NotEmpty(saved.Font);
    }

    // Also used by the supplemental real-parser acceptance executable.
    public static (byte[] Pdf, byte[] Font, int FontObjectNumber) CreateSavedDocument(string operation, ushort flags, bool streamXref)
    {
        using var context = new PdfFontCollisionTestDocument("direct");
        var program = Program(3, flags);
        var metadata = PdfSanitizedExternalFontMetadataInspector.Inspect(program);
        var face = new PdfExternalFontFace("test", "face", "Rights Test", "Rights Test", 400, 5,
            PdfExternalFontSlant.Upright, PdfExternalFontTechnology.TrueType, metadata.EmbeddingRights)
        { NoSubsetting = metadata.NoSubsetting, BitmapEmbeddingOnly = metadata.BitmapEmbeddingOnly };
        var options = new PdfWriteOptions { CrossReferenceMode = streamXref
            ? PdfCrossReferenceWriteMode.CrossReferenceStream : PdfCrossReferenceWriteMode.TraditionalTable };
        var plan = PdfWritePlanner.Create(context.Document, options);
        PdfObjectWriteProjectionSet projections;
        PdfObjectIdentifier fontId;
        string resourceName;
        if (operation == "type0")
        {
            var projected = PdfExternalType0TrueTypeResourceProjector.Create(plan, PdfFilteredTextTestDocument.PageId,
                "FExt1", face, program, "A\u03A9\U0001F600");
            projections = projected.Projections;
            fontId = projected.Type0FontIdentifier;
            resourceName = projected.ResourceName;
            Assert.Equal(new byte[] { 0, 1, 0, 2, 0, 3 }, projected.Subset.EncodeText("A\u03A9\U0001F600"));
        }
        else
        {
            var projected = operation == "full"
                ? PdfExternalTrueTypeResourceProjector.Create(plan, PdfFilteredTextTestDocument.PageId, "FExt1", face, program)
                : PdfExternalTrueTypeSubsetResourceProjector.Create(plan, PdfFilteredTextTestDocument.PageId, "FExt1", face, program, "A");
            projections = projected.Projections;
            fontId = projected.FontIdentifier;
            resourceName = projected.ResourceName;
        }
        Assert.Equal("FExt2", resourceName);
        context.AssertOriginalGraph();
        projections.ApplyTo(context.Document.Objects);
        PdfExternalFontResourceCollisionTests.AssertMapping(context, resourceName, fontId);
        context.AssertUnrelatedObjectsUnchanged();
        var font = context.Document.Objects.Get<PdfDictionary>(fontId);
        if (operation == "type0")
        {
            var map = context.Document.Objects.Get<PdfStream>(font.Get<PdfIndirectReference>(Name("ToUnicode")).Identifier);
            Assert.Contains("<0003> <D83DDE00>", Encoding.ASCII.GetString(map.ToByteArray()), StringComparison.Ordinal);
            font = context.Document.Objects.Get<PdfDictionary>(font.Get<PdfArray>(Name("DescendantFonts")).Get<PdfIndirectReference>(0).Identifier);
            Assert.NotEmpty(font.Get<PdfArray>(Name("W")));
            var gids = context.Document.Objects.Get<PdfStream>(font.Get<PdfIndirectReference>(Name("CIDToGIDMap")).Identifier);
            Assert.Equal(new byte[] { 0, 0, 0, 1, 0, 2, 0, 3 }, gids.ToByteArray());
        }
        else Assert.Equal(224, font.Get<PdfArray>(Name("Widths")).Count);
        var descriptor = context.Document.Objects.Get<PdfDictionary>(font.Get<PdfIndirectReference>(Name("FontDescriptor")).Identifier);
        var embedded = context.Document.Objects.Get<PdfStream>(descriptor.Get<PdfIndirectReference>(Name("FontFile2")).Identifier);
        byte[] bytes = embedded.ToByteArray();
        Assert.Equal(bytes.Length, embedded.Dictionary.Get<PdfInteger>(Name("Length1")).Value);
        if (operation == "full") Assert.Equal(program.Bytes.ToArray(), bytes);
        else Assert.True(bytes.Length < program.Bytes.Length);
        using var input = new MemoryStream(bytes, writable: false);
        var sanitized = new PdfOpenTypeFontSanitizer().Sanitize(input);
        Assert.Equal(bytes, sanitized.Bytes.ToArray());
        Assert.Equal(metadata.EmbeddingRights, PdfExternalTrueTypeInspector.Inspect(sanitized).EmbeddingRights);
        int os2 = TableOffset(bytes, "OS/2");
        Assert.Equal(3, BinaryPrimitives.ReadUInt16BigEndian(bytes.AsSpan(os2)));
        Assert.Equal(flags, BinaryPrimitives.ReadUInt16BigEndian(bytes.AsSpan(os2 + 8)));
        using var output = new MemoryStream();
        PdfFullRewriteWriter.Write(output, PdfWritePlanner.Create(context.Document, options));
        return (output.ToArray(), bytes, fontId.ObjectNumber);
    }

    private static int TableOffset(byte[] bytes, string tag)
    {
        int count = BinaryPrimitives.ReadUInt16BigEndian(bytes.AsSpan(4));
        for (int i = 0; i < count; i++)
        {
            int record = 12 + i * 16;
            if (Encoding.ASCII.GetString(bytes, record, 4) == tag)
                return checked((int)BinaryPrimitives.ReadUInt32BigEndian(bytes.AsSpan(record + 8)));
        }
        throw new InvalidOperationException("Fixture table missing.");
    }

    private static PdfName Name(string name) => PdfName.FromLatin1(name);
}
