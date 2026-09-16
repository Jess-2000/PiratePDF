using System.IO;
using System.Text;
using System.Threading;
using BlackFlag.Engine.Fonts;
using BlackFlag.Engine.Fonts.Sanitization;
using BlackFlag.Engine.Services.Text;

namespace BlackFlag.Engine.Services.Tests.Fonts;

public sealed partial class PdfExternalFontEmbeddingRightsTests
{
    [Theory]
    [InlineData(0x100, PdfExternalFontEmbeddingRights.Allowed, false, false)]
    [InlineData(0x108, PdfExternalFontEmbeddingRights.Allowed, false, false)]
    [InlineData(0x200, PdfExternalFontEmbeddingRights.Allowed, false, false)]
    [InlineData(4, PdfExternalFontEmbeddingRights.Allowed, false, false)]
    [InlineData(2, PdfExternalFontEmbeddingRights.Allowed, false, false)]
    [InlineData(0, PdfExternalFontEmbeddingRights.Restricted, false, false)]
    [InlineData(0, PdfExternalFontEmbeddingRights.Allowed, true, false)]
    [InlineData(8, PdfExternalFontEmbeddingRights.Allowed, false, true)]
    public void ExternalTextDenialPreservesTargetGraphAndEarlierTransactionState(
        ushort flags, PdfExternalFontEmbeddingRights rights, bool noSubset, bool bitmap)
    {
        using var context = new PdfFontCollisionTestDocument("direct");
        var match = Match(flags, rights, noSubset, bitmap);
        var text = context.Text;
        using (var transaction = text.Session.BeginTransaction())
        {
            // An unrelated earlier edit belongs to the caller's transaction.
            PdfExistingTextStyleService.Apply(text.Session, text.Session.Document.Pages[0], text.Source,
                text.Descriptor, new PdfExistingTextStyleUpdate { FillColor = PdfExistingTextColor.Gray(0.5) });
            byte[] earlier = text.Content.ToByteArray();
            int count = context.Document.Objects.Count;
            Assert.Throws<NotSupportedException>(() => PdfExternalExistingTextFontService.ChangeFont(
                text.Session, text.Session.Document.Pages[0], text.Source, text.Descriptor,
                match, new PdfOpenTypeFontSanitizer()));
            Assert.Equal(earlier, text.Content.ToByteArray());
            Assert.Equal(count, context.Document.Objects.Count);
            context.AssertUnrelatedObjectsUnchanged();
            Assert.False(text.Session.History.CanUndo);
        }
        context.AssertOriginalGraph();
        Assert.False(text.Session.History.CanUndo);
        // Denial has not made the unchanged registered target unusable.
        using var permitted = text.Session.BeginTransaction();
        var operation = PdfExternalExistingTextFontService.ChangeFont(text.Session, text.Session.Document.Pages[0],
            text.Source, text.Descriptor, Match(8), new PdfOpenTypeFontSanitizer());
        Assert.Equal("FExt2", operation.ResourceName);
        _ = permitted.Commit();
        Assert.Contains("/FExt2 12 Tf", Encoding.ASCII.GetString(PdfContentStreamMutationCodec.Decode(text.Content)), StringComparison.Ordinal);
        byte[] changed = text.Content.ToByteArray();
        _ = text.Session.History.Undo();
        context.AssertOriginalGraph();
        _ = text.Session.History.Redo();
        Assert.Equal(changed, text.Content.ToByteArray());
    }

    private static PdfExternalFontMatch Match(ushort flags,
        PdfExternalFontEmbeddingRights rights = PdfExternalFontEmbeddingRights.Allowed,
        bool noSubset = false, bool bitmap = false)
    {
        var provider = new RightsProvider(Program(3, flags).Bytes.ToArray(), rights, noSubset, bitmap);
        return Assert.Single(new PdfExternalFontCatalog([provider]).Match(
            new PdfExternalFontMatchRequest("Rights", requireEmbeddable: false), "A"));
    }

    private sealed class RightsProvider(byte[] bytes, PdfExternalFontEmbeddingRights rights, bool noSubset, bool bitmap) : IPdfExternalFontProvider
    {
        public string Id => "rights";
        public IReadOnlyList<PdfExternalFontFace> GetFaces(CancellationToken cancellationToken = default) =>
            [new(Id, "face", "Rights", "Rights", 400, 5, PdfExternalFontSlant.Upright, PdfExternalFontTechnology.TrueType, rights)
            { NoSubsetting = noSubset, BitmapEmbeddingOnly = bitmap }];
        public bool SupportsRune(PdfExternalFontFace face, Rune rune) => rune.Value == 65;
        public Stream OpenFontProgram(PdfExternalFontFace face) => new MemoryStream(bytes, writable: false);
    }
}
