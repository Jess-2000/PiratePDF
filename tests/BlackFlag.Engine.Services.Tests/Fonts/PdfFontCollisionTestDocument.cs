using System.IO;
using System.Text;
using System.Threading;
using BlackFlag.Engine.Document.Model;
using BlackFlag.Engine.Document.Objects;
using BlackFlag.Engine.Document.Objects.Primitives;
using BlackFlag.Engine.Fonts;
using BlackFlag.Engine.Fonts.Sanitization;
using BlackFlag.Engine.Services.Tests.Text;

namespace BlackFlag.Engine.Services.Tests.Fonts;

internal sealed class PdfFontCollisionTestDocument : IDisposable
{
    internal static readonly PdfObjectIdentifier OldFontId = new(9, 7);
    internal static readonly PdfObjectIdentifier ResourcesId = new(10, 3);
    internal static readonly PdfObjectIdentifier CategoryId = new(11, 4);
    internal static readonly PdfObjectIdentifier SiblingId = new(12, 1);

    internal PdfFontCollisionTestDocument(string ownership, bool collision = true)
    {
        Text = new PdfFilteredTextTestDocument(text: "A");
        PdfDictionary page = Document.Objects.Get<PdfDictionary>(PdfFilteredTextTestDocument.PageId);
        PdfDictionary parent = Document.Objects.Get<PdfDictionary>(new PdfObjectIdentifier(2));
        var sibling = page.CreateCopy();
        var resources = page.Get<PdfDictionary>(Name("Resources"));
        Fonts = resources.Get<PdfDictionary>(Name("Font"));
        if (collision)
        {
            Fonts.Add(Name("FExt1"), new PdfIndirectReference(OldFontId));
        }

        resources.Add(Name("PrivateResourceEntry"), new PdfInteger(71));
        resources.Add(Name("ProcSet"), new PdfArray([Name("PDF"), Name("Text")]));
        Fonts.Add(Name("KeepFontAlias"), new PdfIndirectReference(new PdfObjectIdentifier(5)));
        Document.Objects.Add(OldFontId, new PdfDictionary
        {
            { Name("Type"), Name("Font") },
            { Name("Subtype"), Name("Type1") },
            { Name("BaseFont"), Name("Courier") },
            { Name("Encoding"), Name("WinAnsiEncoding") }
        });

        if (ownership == "indirect-category")
        {
            Document.Objects.Add(CategoryId, Fonts);
            resources.Set(Name("Font"), new PdfIndirectReference(CategoryId));
        }

        switch (ownership)
        {
            case "direct":
            case "indirect-category":
                sibling.Set(Name("Resources"), resources);
                break;
            case "indirect":
                Document.Objects.Add(ResourcesId, resources);
                page.Set(Name("Resources"), new PdfIndirectReference(ResourcesId));
                sibling.Set(Name("Resources"), new PdfIndirectReference(ResourcesId));
                break;
            case "inherited":
                parent.Set(Name("Resources"), resources);
                page.Remove(Name("Resources"));
                sibling.Remove(Name("Resources"));
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(ownership));
        }

        // A real sibling shares the original resource owner but has no content.
        sibling.Remove(Name("Contents"));
        Document.Objects.Add(SiblingId, sibling);
        parent.Get<PdfArray>(Name("Kids")).Add(new PdfIndirectReference(SiblingId));
        parent.Set(Name("Count"), new PdfInteger(2));
        OriginalObjects = Document.Objects.Identifiers.ToDictionary(id => id, id => Document.Objects.Get(id));
        OriginalBytes = Snapshot();
        byte[] raw = PdfExternalType0TrueTypeResourceProjectorTests.BuildFont(fsType: 0);
        Provider = new SyntheticProvider(raw);
        Match = Assert.Single(new PdfExternalFontCatalog([Provider]).Match(new PdfExternalFontMatchRequest("Collision Test"), "A"));
        Program = Match.SanitizeFontProgram(new PdfOpenTypeFontSanitizer());
    }

    internal PdfFilteredTextTestDocument Text { get; }
    internal PdfDocument Document => Text.Document;
    internal PdfDictionary Fonts { get; }
    internal PdfExternalFontMatch Match { get; }
    internal PdfExternalFontFace Face => Match.Face;
    internal PdfSanitizedExternalFontProgram Program { get; }
    private SyntheticProvider Provider { get; }
    internal Dictionary<PdfObjectIdentifier, PdfObject> OriginalObjects { get; }
    internal Dictionary<PdfObjectIdentifier, byte[]> OriginalBytes { get; }

    internal Dictionary<PdfObjectIdentifier, byte[]> Snapshot() =>
        Document.Objects.Identifiers.ToDictionary(id => id,
            id => PdfFilteredTextTestDocument.Serialize(Document.Objects.Get(id)));

    internal void AssertOriginalGraph()
    {
        Assert.Equal(OriginalBytes.Keys, Document.Objects.Identifiers);
        foreach (var pair in OriginalBytes)
        {
            Assert.Equal(pair.Value, PdfFilteredTextTestDocument.Serialize(Document.Objects.Get(pair.Key)));
        }
    }

    internal void AssertUnrelatedObjectsUnchanged()
    {
        foreach (var pair in OriginalBytes)
        {
            if (pair.Key == PdfFilteredTextTestDocument.PageId || pair.Key == PdfFilteredTextTestDocument.ContentId)
            {
                continue;
            }

            Assert.Equal(pair.Value, PdfFilteredTextTestDocument.Serialize(Document.Objects.Get(pair.Key)));
            Assert.Same(OriginalObjects[pair.Key], Document.Objects.Get(pair.Key));
        }
    }

    internal static PdfName Name(string value) => PdfName.FromLatin1(value);
    public void Dispose() => Text.Dispose();

    private sealed class SyntheticProvider(byte[] program) : IPdfExternalFontProvider
    {
        public string Id => "collision-test";
        public IReadOnlyList<PdfExternalFontFace> GetFaces(CancellationToken cancellationToken = default) =>
            [new(Id, "face", "Collision Test", "Collision Test Regular", 400, 5,
                PdfExternalFontSlant.Upright, PdfExternalFontTechnology.TrueType, PdfExternalFontEmbeddingRights.Allowed)];
        public bool SupportsRune(PdfExternalFontFace face, Rune rune) => rune.Value is 65 or 0x03A9 or 0x1F600;
        public Stream OpenFontProgram(PdfExternalFontFace face) => new MemoryStream(program, writable: false);
    }
}
