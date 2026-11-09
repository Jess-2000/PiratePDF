#nullable enable
using System.IO;
using System.Text;
using System.Threading;
using BlackFlag.Engine.Document.Model;
using BlackFlag.Engine.Document.Objects;
using BlackFlag.Engine.Document.Objects.Primitives;
using BlackFlag.Engine.Document.Pages;
using BlackFlag.Engine.Fonts;
using BlackFlag.Engine.Services.Tests.Text;
using BlackFlag.Engine.Writing;

namespace BlackFlag.Engine.Services.Tests.Conformance;

internal static class ConformanceFixture
{
    internal static readonly PdfObjectIdentifier PageId = new(3);
    internal static readonly PdfObjectIdentifier ContentId = new(4, 2);
    internal static readonly PdfObjectIdentifier SiblingId = new(6, 3);
    internal static readonly PdfObjectIdentifier FormId = new(12, 5);
    internal const string Text = "q\n1 0.25 0 1 10 20 cm\nBT\n/F1 20 Tf\n1 0 0 1 30 40 Tm\n(A) Tj\nET\nQ\n";

    internal static PdfName N(string name) => PdfName.FromLatin1(name);
    internal static PdfIndirectReference R(int number, int generation = 0) => new(new(number, generation));
    internal static PdfArray Box() => new([new PdfInteger(0), new PdfInteger(0), new PdfInteger(200), new PdfInteger(200)]);
    internal static PdfWriteOptions Options(bool streamXref, bool incremental = false) => new()
    {
        Mode = incremental ? PdfWriteMode.IncrementalUpdate : PdfWriteMode.FullRewrite,
        CrossReferenceMode = streamXref ? PdfCrossReferenceWriteMode.CrossReferenceStream
            : PdfCrossReferenceWriteMode.TraditionalTable
    };

    internal static byte[] Create(bool streamXref, string? text = null, bool forms = false, bool shaping = false)
    {
        using var template = new PdfFilteredTextTestDocument(text: "A");
        PdfDocument document = template.Document;
        var page = document.Objects.Get<PdfDictionary>(PageId);
        page.Set(N("MediaBox"), Box());
        var resources = page.Get<PdfDictionary>(N("Resources"));
        resources.Add(N("PrivateResource"), new PdfInteger(83));
        var fonts = resources.Get<PdfDictionary>(N("Font"));
        fonts.Add(N("FExt1"), R(10, 7));
        fonts.Add(N("KeepAlias"), R(5));
        document.Objects.Add(new(10, 7), new PdfDictionary
        {
            { N("Type"), N("Font") }, { N("Subtype"), N("Type1") }, { N("BaseFont"), N("Courier") }
        });
        document.Objects.Set(new(5), new PdfDictionary
        {
            { N("Type"), N("Font") }, { N("Subtype"), N("TrueType") },
            { N("BaseFont"), N("OwnedRectangles") }, { N("Encoding"), N("WinAnsiEncoding") },
            { N("FirstChar"), new PdfInteger(65) }, { N("LastChar"), new PdfInteger(66) },
            { N("Widths"), new PdfArray([new PdfInteger(600), new PdfInteger(600)]) },
            { N("FontDescriptor"), R(8) }, { N("PrivateFont"), new PdfInteger(89) }
        });
        document.Objects.Add(new(8), new PdfDictionary
        {
            { N("Type"), N("FontDescriptor") }, { N("FontName"), N("OwnedRectangles") },
            { N("Flags"), new PdfInteger(32) },
            { N("FontBBox"), new PdfArray([new PdfInteger(0), new PdfInteger(0), new PdfInteger(500), new PdfInteger(700)]) },
            { N("Ascent"), new PdfInteger(800) }, { N("Descent"), new PdfInteger(-200) },
            { N("CapHeight"), new PdfInteger(700) }, { N("ItalicAngle"), new PdfInteger(0) },
            { N("StemV"), new PdfInteger(80) }, { N("FontFile2"), R(9, 4) }
        });
        document.Objects.Add(new(9, 4), new PdfStream(new PdfDictionary(), ConformanceFont.Create()));
        if (shaping)
        {
            document.Objects.Get<PdfDictionary>(new(5)).Add(N("ToUnicode"), R(13));
            document.Objects.Add(new(13), new PdfStream(new PdfDictionary(),
                "/CIDInit /ProcSet findresource begin 12 dict begin begincmap\n/CIDSystemInfo << /Registry (Adobe) /Ordering (UCS) /Supplement 0 >> def\n/CMapName /B10ToUnicode def /CMapType 2 def\n1 begincodespacerange <00> <FF> endcodespacerange\n1 beginbfchar <41> <00610301> endbfchar\nendcmap CMapName currentdict /CMap defineresource pop end end\n"u8));
        }
        var sibling = page.CreateCopy();
        sibling.Set(N("Contents"), R(7, 1));
        sibling.Set(N("PrivatePageEntry"), new PdfInteger(97));
        document.Objects.Add(SiblingId, sibling);
        document.Objects.Add(new(7, 1), PdfFilteredTextTestDocument.CreateStream(
            "BT /F1 20 Tf 1 0 0 1 100 100 Tm (B) Tj ET\n"u8));
        var parent = document.Objects.Get<PdfDictionary>(new(2));
        parent.Get<PdfArray>(N("Kids")).Add(new PdfIndirectReference(SiblingId));
        parent.Set(N("Count"), new PdfInteger(2));
        document.Objects.Get<PdfDictionary>(new(1)).Add(N("PrivateCatalog"), new PdfInteger(79));
        if (forms)
        {
            resources.Add(N("XObject"), new PdfDictionary { { N("X1"), new PdfIndirectReference(FormId) } });
            // A separate resource dictionary avoids a cyclic Form resource graph.
            var form = PdfFilteredTextTestDocument.CreateStream("BT /F1 20 Tf (A) Tj ET\n"u8);
            form.Dictionary.Add(N("Type"), N("XObject"));
            form.Dictionary.Add(N("Subtype"), N("Form"));
            form.Dictionary.Add(N("BBox"), Box());
            form.Dictionary.Add(N("Resources"), new PdfDictionary { { N("Font"), fonts.CreateCopy() } });
            document.Objects.Add(FormId, form);
            text = "q 1 0 0 1 20 30 cm /X1 Do Q\nq 1 0 0 1 80 100 cm /X1 Do Q\n";
        }
        document.Objects.Set(ContentId, PdfFilteredTextTestDocument.CreateStream(Encoding.ASCII.GetBytes(text ?? Text)));
        // The mutation-unit helper uses /Fl shorthand. Ordinary PDF stream filters
        // use the full name understood by the general parsing/filter pipeline.
        foreach (var id in document.Objects.Identifiers)
            if (document.Objects.Get(id) is PdfStream stream && stream.Dictionary.ContainsKey(N("Filter")))
                stream.Dictionary.Set(N("Filter"), new PdfArray([N("FlateDecode")]));
        return PdfFullRewriteWriter.Write(PdfWritePlanner.Create(document, Options(streamXref)));
    }

    internal static Dictionary<PdfObjectIdentifier, byte[]> Snapshot(PdfDocument document) =>
        document.Objects.Identifiers.ToDictionary(id => id,
            id => PdfFilteredTextTestDocument.Serialize(document.Objects.Get(id)));

    internal static void AssertSnapshot(PdfDocument document, Dictionary<PdfObjectIdentifier, byte[]> snapshot)
    {
        Assert.Equal(snapshot.Keys.OrderBy(id => id.ObjectNumber), document.Objects.Identifiers.OrderBy(id => id.ObjectNumber));
        foreach (var entry in snapshot)
            Assert.Equal(entry.Value, PdfFilteredTextTestDocument.Serialize(document.Objects.Get(entry.Key)));
    }

    internal sealed class FontProvider : IPdfExternalFontProvider
    {
        public string Id => "b10-owned-outlines";
        public IReadOnlyList<PdfExternalFontFace> GetFaces(CancellationToken cancellationToken = default) =>
            [new(Id, "rectangles", "B10 Rectangles", "B10 Rectangles", 400, 5,
                PdfExternalFontSlant.Upright, PdfExternalFontTechnology.TrueType, PdfExternalFontEmbeddingRights.Allowed)];
        public bool SupportsRune(PdfExternalFontFace face, Rune rune) => rune.Value is 65 or 66;
        public Stream OpenFontProgram(PdfExternalFontFace face) => new MemoryStream(ConformanceFont.Create(), writable: false);
    }
}
