#nullable enable
using System.Globalization;
using System.Text;
using BlackFlag.Engine.Document.Model;
using BlackFlag.Engine.Document.Objects;
using BlackFlag.Engine.Document.Objects.Primitives;
using BlackFlag.Engine.Document.Pages;
using BlackFlag.Engine.Editing.Text;
using BlackFlag.Engine.Filters;
using BlackFlag.Engine.Parsing.ContentStreams;
using BlackFlag.Engine.Parsing.CrossReferences;
using BlackFlag.Engine.Parsing.Documents;
using BlackFlag.Engine.Parsing.Loading;
using BlackFlag.Engine.Parsing.Rendering;
using BlackFlag.Engine.Parsing.Resources;
using BlackFlag.Engine.Parsing.Tests;
using BlackFlag.Engine.Rendering.DisplayList;
using BlackFlag.Engine.Rendering.Fonts;
using BlackFlag.Engine.Rendering.Interpretation;
using BlackFlag.Engine.Rendering.Raster;
using BlackFlag.Engine.Rendering.Skia;
using BlackFlag.Engine.Services.Text;
using G = BlackFlag.Engine.Objects;

namespace BlackFlag.Engine.Services.Tests.Conformance;

/// <summary>
/// Reopens owned, small fixtures using the real xref/object/page/content parsers.
/// The bounded model adapter below is fixture plumbing, not a production PDF importer.
/// </summary>
internal sealed class ReopenedPdf : IDisposable
{
    private readonly TestRandomAccessSource source;
    private readonly PdfFilterPipeline filters = new(PdfFilterRegistrations.CreateDefault());
    private readonly PdfResourceResolver resources;
    internal PdfIndirectObjectResolver Objects { get; }
    internal PdfObjectLocationTable Locations { get; }
    internal PdfEffectivePageCollection Pages { get; }
    internal long XrefOffset { get; }
    internal int RevisionCount { get; }
    internal G.PdfDictionary Trailer { get; }

    internal ReopenedPdf(byte[] bytes, bool streamXref)
    {
        Assert.InRange(bytes.Length, 1, 128 * 1024);
        source = new(bytes);
        string tail = Encoding.Latin1.GetString(bytes);
        int marker = tail.LastIndexOf("startxref\n", StringComparison.Ordinal);
        Assert.True(marker >= 0);
        XrefOffset = long.Parse(tail[(marker + 10)..].Split('\n')[0], CultureInfo.InvariantCulture);
        if (!streamXref)
        {
            var chain = new PdfCrossReferenceParser(source).Parse();
            RevisionCount = chain.Tables.Count;
            Trailer = chain.Tables[^1].Trailer;
            Locations = new(chain.Tables.Select(table =>
                table.Sections.SelectMany(section => section.Entries).Select(PdfObjectLocation.From)));
        }
        else
        {
            var revisions = new List<PdfObjectLocation[]>();
            long offset = XrefOffset;
            G.PdfDictionary? newest = null;
            var seen = new HashSet<long>();
            while (true)
            {
                Assert.True(seen.Add(offset));
                Assert.InRange(seen.Count, 1, 4);
                // Only the declaration identifies the xref object; its stream and entries
                // are loaded/validated by the production resolver and xref-stream parser.
                string[] header = tail[checked((int)offset)..].Split([' ', '\n', '\r'], 4);
                var id = new PdfObjectId(int.Parse(header[0], CultureInfo.InvariantCulture),
                    int.Parse(header[1], CultureInfo.InvariantCulture));
                var bootstrap = Resolver(new([[PdfObjectLocation.CreateDirect(id, offset)]]));
                var stream = Assert.IsType<G.PdfStream>(bootstrap.Resolve(id));
                var xref = new PdfCrossReferenceStreamParser(filters, new()).Parse(offset, stream);
                newest ??= xref.Dictionary;
                revisions.Insert(0, xref.Sections.SelectMany(section => section.Entries)
                    .Select(PdfObjectLocation.From).ToArray());
                if (!xref.Dictionary.TryGetValue(new("Prev"), out var previous)) break;
                offset = Assert.IsType<G.PdfInteger>(previous).Value;
            }
            RevisionCount = revisions.Count;
            Trailer = newest!;
            Locations = new(revisions);
        }
        Objects = Resolver(Locations);
        resources = new(Objects);
        var catalog = Dictionary(Assert.IsType<G.PdfIndirectReference>(Trailer[new("Root")]));
        var pages = Assert.IsType<G.PdfIndirectReference>(catalog[new("Pages")]);
        Pages = new PdfEffectivePageTreeLoader(Objects).Load(new(pages.ObjectNumber, pages.GenerationNumber));
        Assert.Equal(2, Pages.Count);
        Assert.All(Pages, page => Assert.Equal(new PdfPageBox(0, 0, 200, 200), page.Geometry.MediaBox.Value));
    }

    private PdfIndirectObjectResolver Resolver(PdfObjectLocationTable locations) =>
        new(source, locations, filters, new());

    internal G.PdfDictionary Dictionary(G.PdfIndirectReference reference) =>
        Assert.IsType<G.PdfDictionary>(Objects.Resolve(new(reference.ObjectNumber, reference.GenerationNumber)));

    internal G.PdfDictionary Dictionary(int number, int generation = 0) =>
        Assert.IsType<G.PdfDictionary>(Objects.Resolve(new(number, generation)));

    internal G.PdfStream Stream(int number, int generation = 0) =>
        Assert.IsType<G.PdfStream>(Objects.Resolve(new(number, generation)));

    internal byte[] Decode(G.PdfStream stream)
    {
        G.PdfName[] names = stream.Dictionary.TryGetValue(new("Filter"), out var filter)
            ? filter is G.PdfName name ? [name] : Assert.IsType<G.PdfArray>(filter).Cast<G.PdfName>().ToArray()
            : [];
        IReadOnlyList<G.PdfDictionary?>? parameters = null;
        if (stream.Dictionary.TryGetValue(new("DecodeParms"), out var value))
            parameters = value is G.PdfDictionary dictionary ? [dictionary]
                : Assert.IsType<G.PdfArray>(value).Select(item => item as G.PdfDictionary).ToArray();
        return filters.Decode(stream.Data, names, parameters, new()).ToArray();
    }

    internal PdfExistingTextDiscoverySource TextSource(int pageIndex = 0)
    {
        var page = Pages[pageIndex];
        var content = Assert.IsType<G.PdfIndirectReference>(page.Dictionary[new("Contents")]);
        var operations = new PdfContentStreamParser().Parse(Decode(Stream(content.ObjectNumber, content.GenerationNumber)));
        var scope = resources.CreateScope(page.Resources.Value);
        var display = new PdfGraphicsInterpreter(formOperationProvider: new PdfNestedGraphicsProvider(Objects, resources))
            .Interpret(operations, scope);
        return new(new(page.ObjectNumber, page.GenerationNumber), PdfExistingTextSourceKind.PageContent,
            operations, DecodeText(display), [new PdfObjectIdentifier(content.ObjectNumber, content.GenerationNumber)]);
    }

    // Interpretation deliberately retains encoded strings. The host supplies decoded
    // text for discovery; use the real font decoder, never guessed Latin-1 mappings.
    private static PdfDisplayList DecodeText(PdfDisplayList display) => new(display.Select(item => item switch
    {
        PdfDisplayTextRun run => new PdfDisplayTextRun(run.Font, run.FontSize,
            run.Elements.Select(element => DecodeElement(run, element)), run.TextMatrix,
            run.GraphicsState, run.SourceSpan, run.MarkedContent),
        PdfDisplayForm form => new PdfDisplayForm(form.Form, form.Transform, DecodeText(form.Content),
            form.GraphicsState, form.SourceSpan, form.MarkedContent),
        _ => item
    }));

    private static PdfTextRunElement DecodeElement(PdfDisplayTextRun run, PdfTextRunElement element)
    {
        if (element.Kind != PdfTextRunElementKind.Text) return element;
        var decoded = new PdfFontDecoder().Decode(new(run.Font, run.FontSize, [element], run.TextMatrix,
            run.GraphicsState, run.SourceSpan, run.MarkedContent));
        Assert.Empty(decoded.Diagnostics);
        Assert.All(decoded.Glyphs, glyph => Assert.NotNull(glyph.UnicodeText));
        return PdfTextRunElement.CreateText(element.EncodedBytes.Span,
            string.Concat(decoded.Glyphs.Select(glyph => glyph.UnicodeText)));
    }

    internal PdfRenderResult Render(int pageIndex = 0)
    {
        using var renderer = new SkiaPdfPageRenderer(new PdfNestedGraphicsProvider(Objects, resources));
        var result = renderer.Render(TextSource(pageIndex).DisplayList, new PdfRenderRequest
        {
            Region = new PdfRenderRegion(0, 0, 200, 200), PixelWidth = 200, PixelHeight = 200
        });
        Assert.Empty(result.Diagnostics); // In particular, no missing/invalid embedded-font fallback.
        return result;
    }

    internal PdfDocument ToEditableFixture()
    {
        Assert.InRange(Locations.Locations.Count, 1, 32);
        var document = PdfDocument.CreateEmpty(PdfVersion.Pdf20);
        document.BindCoreStructure(new(1), new(2));
        foreach (var location in Locations.Locations.Values)
        {
            if (location.Kind == PdfObjectLocationKind.Free) continue;
            var value = Objects.Resolve(location.ObjectId);
            if (value is G.PdfStream stream && stream.Dictionary.TryGetValue(new("Type"), out var type)
                && type is G.PdfName { Value: "XRef" }) continue;
            document.Objects.Add(new(location.ObjectId.ObjectNumber, location.ObjectId.GenerationNumber), Convert(value));
        }
        foreach (var loaded in Pages)
        {
            var page = new PdfPage(200, 200);
            page.BindObjectIdentifier(new(loaded.ObjectNumber, loaded.GenerationNumber));
            document.Pages.Add(page);
        }
        return document;
    }

    // Explicit supported fixture types; unknown types fail rather than silently disappearing.
    private static PdfObject Convert(G.PdfObject value) => value switch
    {
        G.PdfName name => PdfName.FromLatin1(name.Value),
        G.PdfInteger integer => new PdfInteger(integer.Value),
        G.PdfReal real => new PdfReal(real.Value),
        G.PdfIndirectReference reference => new PdfIndirectReference(new(reference.ObjectNumber, reference.GenerationNumber)),
        G.PdfLiteralString text => PdfString.FromLatin1(text.Value),
        G.PdfHexString text => new PdfString(text.Value.Span, PdfStringFormat.Hexadecimal),
        G.PdfArray array => new PdfArray(array.Select(Convert)),
        G.PdfDictionary dictionary => new PdfDictionary(dictionary.Select(pair =>
            new KeyValuePair<PdfName, PdfObject>(PdfName.FromLatin1(pair.Key.Value), Convert(pair.Value)))),
        G.PdfStream stream => new PdfStream((PdfDictionary)Convert(stream.Dictionary), stream.Data.Span),
        _ => throw new NotSupportedException($"Unexpected fixture type {value.Type}.")
    };

    public void Dispose() => source.Dispose();
}
