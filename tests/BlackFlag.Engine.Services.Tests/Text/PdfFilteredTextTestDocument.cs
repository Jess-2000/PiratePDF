using System.IO;
using System.IO.Compression;
using System.Text;
using BlackFlag.Engine.Document.Model;
using BlackFlag.Engine.Document.Objects;
using BlackFlag.Engine.Document.Objects.Primitives;
using BlackFlag.Engine.Document.Pages;
using BlackFlag.Engine.Editing.Documents;
using BlackFlag.Engine.Editing.Text;
using BlackFlag.Engine.Rendering.DisplayList;
using BlackFlag.Engine.Rendering.Graphics;
using BlackFlag.Engine.Rendering.Resources;
using BlackFlag.Engine.Services.Text;
using G = BlackFlag.Engine.Objects;

namespace BlackFlag.Engine.Services.Tests.Text;

internal sealed class PdfFilteredTextTestDocument : IDisposable
{
    internal static readonly PdfObjectIdentifier PageId = new(3);
    internal static readonly PdfObjectIdentifier ContentId = new(4, 2);
    private static readonly PdfObjectIdentifier FontId = new(5);

    internal PdfFilteredTextTestDocument(bool positioned = false)
    {
        Document = PdfDocument.CreateEmpty(PdfVersion.Pdf20);
        var catalogId = new PdfObjectIdentifier(1);
        var pagesId = new PdfObjectIdentifier(2);
        Document.BindCoreStructure(catalogId, pagesId);
        Document.Objects.Add(catalogId, new PdfDictionary
        {
            { Name("Type"), Name("Catalog") },
            { Name("Pages"), new PdfIndirectReference(pagesId) }
        });
        Document.Objects.Add(pagesId, new PdfDictionary
        {
            { Name("Type"), Name("Pages") },
            { Name("Kids"), new PdfArray([new PdfIndirectReference(PageId)]) },
            { Name("Count"), new PdfInteger(1) }
        });
        Document.Objects.Add(PageId, new PdfDictionary
        {
            { Name("Type"), Name("Page") },
            { Name("Parent"), new PdfIndirectReference(pagesId) },
            { Name("Contents"), new PdfIndirectReference(ContentId) },
            { Name("MediaBox"), new PdfArray([
                new PdfInteger(0), new PdfInteger(0), new PdfInteger(612), new PdfInteger(792)]) },
            { Name("Resources"), new PdfDictionary
                {
                    { Name("Font"), new PdfDictionary
                        {
                            { Name("F1"), new PdfIndirectReference(FontId) }
                        }
                    }
                }
            },
            { Name("PrivatePageEntry"), new PdfInteger(17) }
        });
        Document.Objects.Add(FontId, new PdfDictionary
        {
            { Name("Type"), Name("Font") },
            { Name("Subtype"), Name("Type1") },
            { Name("BaseFont"), Name("Helvetica") },
            { Name("Encoding"), Name("WinAnsiEncoding") }
        });
        var page = new PdfPage(612, 792);
        page.BindObjectIdentifier(PageId);
        Document.Pages.Add(page);

        string show = positioned ? "[(Hel) -120 <6C6F>] TJ" : "(Hello) Tj";
        OriginalContent = $"BT\n/F1 12 Tf\n{show}\n(Next) Tj\nET\n";
        Content = CreateStream(Encoding.ASCII.GetBytes(OriginalContent));
        Document.Objects.Add(ContentId, Content);
        Session = new PdfEditingSession(Document);

        int start = OriginalContent.IndexOf(show, StringComparison.Ordinal);
        Assert.True(PdfGraphicsOperator.TryGet(positioned ? "TJ" : "Tj", out var textOperator));
        Assert.True(PdfGraphicsOperator.TryGet("Tf", out var fontOperator));
        G.PdfObject[] operands = positioned
            ? [new G.PdfArray([new G.PdfLiteralString("Hel"), new G.PdfInteger(-120), new G.PdfHexString("lo"u8.ToArray())])]
            : [new G.PdfLiteralString("Hello")];
        var operation = new PdfGraphicsOperation(
            textOperator, operands, new PdfContentSourceSpan(0, start, start + show.Length));
        var fontSelection = new PdfGraphicsOperation(
            fontOperator, [new G.PdfName("F1"), new G.PdfInteger(12)], new PdfContentSourceSpan(0, 3, 12));
        var font = new PdfResolvedFont(
            PdfResourceIdentity.ForIndirect(5, 0), G.PdfDictionary.Empty,
            "Type1", "Helvetica", "Helvetica", 400, 5,
            PdfFontSlant.Upright, PdfFontTechnology.Type1, PdfFontEmbeddingRights.Unknown,
            false, new G.PdfName("WinAnsiEncoding"), null, null, null, null, []);
        PdfTextRunElement[] elements = positioned
            ? [PdfTextRunElement.CreateText("Hel"u8, "Hel"), PdfTextRunElement.CreateAdjustment(-120),
                PdfTextRunElement.CreateText("lo"u8, "lo")]
            : [PdfTextRunElement.CreateText("Hello"u8, "Hello")];
        var run = new PdfDisplayTextRun(
            font, 12, elements, PdfMatrix.Identity,
            new PdfGraphicsStateSnapshot { Font = font, FontSize = 12 },
            operation.SourceSpan, null);
        Source = new PdfExistingTextDiscoverySource(
            PageId, PdfExistingTextSourceKind.PageContent, [fontSelection, operation],
            new PdfDisplayList([run]), [ContentId]);
        Descriptor = Discover(Source);
    }

    internal PdfDocument Document { get; }
    internal PdfEditingSession Session { get; }
    internal PdfStream Content { get; }
    internal string OriginalContent { get; }
    internal PdfExistingTextDiscoverySource Source { get; }
    internal PdfExistingTextDescriptor Descriptor { get; }

    internal PdfExistingTextDescriptor Discover(PdfExistingTextDiscoverySource source) =>
        Assert.Single(PdfExistingTextDiscoveryService.Discover(Session, Session.Document.Pages[0], source));

    internal string ReadContent() =>
        Encoding.ASCII.GetString(PdfContentStreamMutationCodec.Decode(Content));

    internal static PdfStream CreateStream(ReadOnlySpan<byte> decoded) => new(
        new PdfDictionary
        {
            { Name("Filter"), new PdfArray([Name("Fl")]) },
            { Name("DecodeParms"), new PdfArray([new PdfDictionary
                {
                    { Name("Predictor"), new PdfInteger(1) },
                    { Name("PrivateParameter"), new PdfInteger(42) }
                }])
            },
            { Name("PrivateStreamEntry"), new PdfInteger(23) }
        }, Compress(decoded));

    internal static byte[] Compress(ReadOnlySpan<byte> decoded)
    {
        using var destination = new MemoryStream();
        using (var zlib = new ZLibStream(destination, CompressionLevel.Optimal, leaveOpen: true))
        {
            zlib.Write(decoded);
        }

        return destination.ToArray();
    }

    internal static byte[] Serialize(PdfObject value)
    {
        using var destination = new MemoryStream();
        BlackFlag.Engine.Writing.PdfObjectSerializer.Write(destination, value);
        return destination.ToArray();
    }

    private static PdfName Name(string name) => PdfName.FromLatin1(name);

    public void Dispose() => Session.Dispose();
}
