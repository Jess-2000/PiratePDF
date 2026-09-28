using System.Globalization;
using System.Text;
using BlackFlag.Engine.Document.Model;
using BlackFlag.Engine.Document.Objects;
using BlackFlag.Engine.Document.Pages;
using BlackFlag.Engine.Editing.Documents;
using BlackFlag.Engine.Editing.Text;
using BlackFlag.Engine.Rendering.DisplayList;
using BlackFlag.Engine.Rendering.Graphics;
using BlackFlag.Engine.Rendering.Interpretation;
using BlackFlag.Engine.Rendering.Resources;
using BlackFlag.Engine.Services.Text;
using G = BlackFlag.Engine.Objects;
using D = BlackFlag.Engine.Document.Objects.Primitives;

namespace BlackFlag.Engine.Services.Tests.Text;

// Fixture operations and bytes are built together; actual output parsing/reopen
// is additionally exercised by the cross-project acceptance harness.
public sealed class PdfDecorationTestContext : IDisposable
{
    private readonly StringBuilder content = new();
    private readonly List<PdfGraphicsOperation> operations = [];
    private readonly PdfResourceScope resources;
    private readonly PdfResolvedFont font;
    public static readonly PdfObjectIdentifier ContentId = new(4, 2);
    public static readonly PdfObjectIdentifier PageId = new(3);

    public PdfDecorationTestContext()
    {
        Document = PdfDocument.CreateEmpty();
        var page = new PdfPage(612, 792);
        page.BindObjectIdentifier(PageId);
        Document.Pages.Add(page);
        Document.Objects.Add(ContentId, new PdfStream([], []));
        BindStructure();
        Session = new PdfEditingSession(Document);
        font = new PdfResolvedFont(
            PdfResourceIdentity.ForIndirect(5, 0), G.PdfDictionary.Empty,
            "Type1", "Helvetica", "Helvetica", 400, 5,
            PdfFontSlant.Upright, PdfFontTechnology.Type1, PdfFontEmbeddingRights.Unknown,
            false, new G.PdfName("WinAnsiEncoding"), null,
            new G.PdfDictionary([new(new G.PdfName("MissingWidth"), new G.PdfInteger(500))]),
            null, null, []);
        resources = new PdfResourceScope(
            (kind, name) => kind == PdfResourceKind.Font && name.Value == "F1" ? font : null, null);
    }

    public PdfDocument Document { get; }
    public PdfEditingSession Session { get; }
    public PdfStream Stream => Document.Objects.Get<PdfStream>(ContentId);
    public PdfExistingTextDiscoverySource Source { get; private set; }
    public PdfDisplayList DisplayList => Source.DisplayList;
    public string Original => content.ToString();
    public string Current => Encoding.ASCII.GetString(PdfContentStreamMutationCodec.Decode(Stream));

    public PdfDecorationTestContext Add(string code, params object[] operands)
    {
        int start = content.Length;
        G.PdfObject[] values = operands.Select(ConvertOperand).ToArray();
        foreach (G.PdfObject value in values)
        {
            content.Append(Format(value)).Append(' ');
        }
        content.Append(code);
        Assert.True(PdfGraphicsOperator.TryGet(code, out var op));
        operations.Add(new PdfGraphicsOperation(op, values, new PdfContentSourceSpan(0, start, content.Length)));
        content.Append('\n');
        return this;
    }

    public PdfDecorationTestContext Finish(bool filtered = false)
    {
        byte[] data = Encoding.ASCII.GetBytes(Original);
        Document.Objects.Set(ContentId, filtered
            ? PdfFilteredTextTestDocument.CreateStream(data)
            : new PdfStream([], data));
        SetSource(operations);
        return this;
    }

    public void SetSource(IReadOnlyList<PdfGraphicsOperation> currentOperations)
    {
        Source = new PdfExistingTextDiscoverySource(
            PageId, PdfExistingTextSourceKind.PageContent, currentOperations,
            new PdfDisplayList(Interpret(currentOperations).Select(WithFixtureUnicode)), [ContentId],
            availableFonts: new Dictionary<string, PdfResolvedFont> { ["F1"] = font });
    }

    private static PdfDisplayItem WithFixtureUnicode(PdfDisplayItem item) => item is PdfDisplayTextRun run
        ? new PdfDisplayTextRun(run.Font, run.FontSize,
            run.Elements.Select(element => element.Kind == PdfTextRunElementKind.Text
                // All fixture strings are ASCII with the matching WinAnsi encoding.
                ? PdfTextRunElement.CreateText(element.EncodedBytes.Span, Encoding.ASCII.GetString(element.EncodedBytes.Span))
                : element), run.TextMatrix, run.GraphicsState, run.SourceSpan, run.MarkedContent)
        : item;

    public PdfDisplayList Interpret(IEnumerable<PdfGraphicsOperation> currentOperations) =>
        new PdfGraphicsInterpreter().Interpret(currentOperations, resources);

    public PdfExistingTextDescriptor Target(int index = 0) =>
        PdfExistingTextDiscoveryService.Discover(Session, Session.Document.Pages[0], Source)[index];

    public byte[] Save(BlackFlag.Engine.Editing.Transactions.PdfEditChangeSet changeSet, bool crossReferenceStream)
    {
        return BlackFlag.Engine.Services.Editing.PdfEditingSaveService.Write(
            Session, changeSet, new BlackFlag.Engine.Writing.PdfWriteOptions
            {
                CrossReferenceMode = crossReferenceStream
                    ? BlackFlag.Engine.Writing.PdfCrossReferenceWriteMode.CrossReferenceStream
                    : BlackFlag.Engine.Writing.PdfCrossReferenceWriteMode.TraditionalTable
            });
    }

    public void Apply(PdfExistingTextStyleUpdate update, int index = 0) =>
        PdfExistingTextStyleService.Apply(Session, Session.Document.Pages[0], Source, Target(index), update);

    public static PdfExistingTextStyleUpdate Underline(double width = 12) => new()
    {
        Decoration = new PdfExistingTextDecoration(PdfExistingTextDecorationKind.Underline, width, -2, 1)
    };

    private void BindStructure()
    {
        var catalog = new PdfObjectIdentifier(1);
        var pages = new PdfObjectIdentifier(2);
        var fontId = new PdfObjectIdentifier(5);
        Document.BindCoreStructure(catalog, pages);
        Document.Objects.Add(catalog, new PdfDictionary
        {
            { D.PdfName.FromLatin1("Type"), D.PdfName.FromLatin1("Catalog") },
            { D.PdfName.FromLatin1("Pages"), new PdfIndirectReference(pages) }
        });
        Document.Objects.Add(pages, new PdfDictionary
        {
            { D.PdfName.FromLatin1("Type"), D.PdfName.FromLatin1("Pages") },
            { D.PdfName.FromLatin1("Kids"), new PdfArray([new PdfIndirectReference(PageId)]) },
            { D.PdfName.FromLatin1("Count"), new D.PdfInteger(1) }
        });
        Document.Objects.Add(PageId, new PdfDictionary
        {
            { D.PdfName.FromLatin1("Type"), D.PdfName.FromLatin1("Page") },
            { D.PdfName.FromLatin1("Parent"), new PdfIndirectReference(pages) },
            { D.PdfName.FromLatin1("Contents"), new PdfIndirectReference(ContentId) },
            { D.PdfName.FromLatin1("MediaBox"), new PdfArray([new D.PdfInteger(0), new D.PdfInteger(0), new D.PdfInteger(612), new D.PdfInteger(792)]) },
            { D.PdfName.FromLatin1("Resources"), new PdfDictionary
                { { D.PdfName.FromLatin1("Font"), new PdfDictionary
                    { { D.PdfName.FromLatin1("F1"), new PdfIndirectReference(fontId) } } } } }
        });
        Document.Objects.Add(fontId, new PdfDictionary
        {
            { D.PdfName.FromLatin1("Type"), D.PdfName.FromLatin1("Font") },
            { D.PdfName.FromLatin1("Subtype"), D.PdfName.FromLatin1("Type1") },
            { D.PdfName.FromLatin1("BaseFont"), D.PdfName.FromLatin1("Helvetica") },
            { D.PdfName.FromLatin1("Encoding"), D.PdfName.FromLatin1("WinAnsiEncoding") },
            { D.PdfName.FromLatin1("FontDescriptor"), new PdfDictionary
                { { D.PdfName.FromLatin1("MissingWidth"), new D.PdfInteger(500) } } }
        });
    }

    private static G.PdfObject ConvertOperand(object value) => value switch
    {
        G.PdfObject pdf => pdf,
        string name when name.StartsWith('/') => new G.PdfName(name[1..]),
        string text => new G.PdfLiteralString(text),
        int number => new G.PdfInteger(number),
        double number => new G.PdfReal(number),
        _ => throw new ArgumentException("Unsupported fixture operand.")
    };

    private static string Format(G.PdfObject value) => value switch
    {
        G.PdfName name => "/" + name.Value,
        G.PdfLiteralString text => "(" + text.Value + ")",
        G.PdfInteger number => number.Value.ToString(CultureInfo.InvariantCulture),
        G.PdfReal number => FormatReal(number.Value),
        G.PdfArray array => "[" + string.Join(" ", array.Select(Format)) + "]",
        G.PdfDictionary dictionary => "<< " + string.Join(" ", dictionary.Select(pair => Format(pair.Key) + " " + Format(pair.Value))) + " >>",
        _ => throw new ArgumentException("Unsupported fixture operand.")
    };

    private static string FormatReal(double number)
    {
        using var output = new System.IO.MemoryStream();
        BlackFlag.Engine.Writing.PdfObjectSerializer.Write(output, new D.PdfReal(number));
        return Encoding.ASCII.GetString(output.ToArray());
    }

    public void Dispose() => Session.Dispose();
}
