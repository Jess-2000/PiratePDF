using System.Text;
using BlackFlag.Engine.Document.Model;
using BlackFlag.Engine.Document.Objects;
using BlackFlag.Engine.Document.Pages;
using BlackFlag.Engine.Editing.Documents;
using BlackFlag.Engine.Editing.Text;
using BlackFlag.Engine.Objects;
using BlackFlag.Engine.Rendering.DisplayList;
using BlackFlag.Engine.Rendering.Graphics;
using BlackFlag.Engine.Rendering.Resources;
using BlackFlag.Engine.Services.Text;

namespace BlackFlag.Engine.Services.Tests.Text;

public sealed class PdfExistingTextFontServiceTests
{
    [Fact]
    public void CandidatesExposeCanonicalExistingResourceMetadata()
    {
        using TestContext context =
            CreateContext(
                "Hello");

        IReadOnlyList<PdfExistingFontCandidate> candidates =
            PdfExistingFontSelectionService.GetCandidates(
                context.Source);

        Assert.Equal(2, candidates.Count);

        PdfExistingFontCandidate alternate =
            Assert.Single(candidates, static candidate =>
                        candidate.ResourceName == "F2");

        Assert.Equal("Alternate Sans", alternate.FamilyName);
        Assert.Equal(700, alternate.Weight);
        Assert.Equal(PdfFontSlant.Italic, alternate.Slant);
    }

    [Fact]
    public void ChangeFontScopesSelectedResourceAndRestoresOriginalFont()
    {
        using TestContext context =
            CreateContext(
                "Hello");

        PdfExistingTextDescriptor descriptor =
            Discover(context);

        _ = PdfExistingTextFontService.ChangeFont(
            context.Session,
            context.Session.Document.Pages[0],
            context.Source,
            descriptor,
            "F2");

        Assert.Equal(
            "BT\n/F1 12 Tf\n/F2 12 Tf\n(Hello) Tj\n/F1 12 Tf\n(Next) Tj\nET\n",
            ReadContent(context));
    }

    [Fact]
    public void ChangeFontParticipatesInUndoAndRedo()
    {
        using TestContext context =
            CreateContext(
                "Hello");

        PdfExistingTextDescriptor descriptor =
            Discover(context);
        string original =
            ReadContent(context);

        using (var transaction =
            context.Session.BeginTransaction())
        {
            _ = PdfExistingTextFontService.ChangeFont(
                context.Session,
                context.Session.Document.Pages[0],
                context.Source,
                descriptor,
                "F2");

            _ = transaction.Commit();
        }

        Assert.NotEqual(original, ReadContent(context));

        _ = context.Session.History.Undo();
        Assert.Equal(original, ReadContent(context));

        _ = context.Session.History.Redo();
        Assert.NotEqual(original, ReadContent(context));
    }

    [Fact]
    public void SelectedExistingFontMustRepresentCurrentUnicode()
    {
        using TestContext context =
            CreateContext(
                "\u20AC",
                currentEncoding: "WinAnsiEncoding",
                alternateEncoding: "StandardEncoding",
                currentBytes: [0x80]);

        PdfExistingTextDescriptor descriptor =
            Discover(context);

        string original =
            ReadContent(context);

        Assert.Throws<NotSupportedException>(
            () =>
                PdfExistingTextFontService.ChangeFont(
                    context.Session,
                    context.Session.Document.Pages[0],
                    context.Source,
                    descriptor,
                    "F2"));

        Assert.Equal(original, ReadContent(context));
    }

    [Fact]
    public void MissingFontResourceIsRejectedWithoutMutation()
    {
        using TestContext context =
            CreateContext(
                "Hello");

        PdfExistingTextDescriptor descriptor =
            Discover(context);
        string original =
            ReadContent(context);

        Assert.Throws<KeyNotFoundException>(
            () =>
                PdfExistingTextFontService.ChangeFont(
                    context.Session,
                    context.Session.Document.Pages[0],
                    context.Source,
                    descriptor,
                    "Missing"));

        Assert.Equal(original, ReadContent(context));
    }

    [Fact]
    public void PositionedTjPreservesAdjustmentsWhileSwitchingFont()
    {
        using TestContext context =
            CreatePositionedContext();

        PdfExistingTextDescriptor descriptor =
            Discover(context);

        _ = PdfExistingTextFontService.ChangeFont(
            context.Session,
            context.Session.Document.Pages[0],
            context.Source,
            descriptor,
            "F2");

        Assert.Contains(
            "/F2 12 Tf\n[(A) -120 (B)] TJ\n/F1 12 Tf",
            ReadContent(context),
            StringComparison.Ordinal);
    }

    private static PdfExistingTextDescriptor Discover(
        TestContext context) =>
        Assert.Single(
            PdfExistingTextDiscoveryService.Discover(
                context.Session,
                context.Session.Document.Pages[0],
                context.Source));

    private static string ReadContent(
        TestContext context) =>
        Encoding.Latin1.GetString(
            context.Document.Objects
                .Get<Document.Objects.PdfStream>(
                    context.ContentStreamIdentifier)
                .ToByteArray());

    private static TestContext CreateContext(
        string text,
        string currentEncoding = "WinAnsiEncoding",
        string alternateEncoding = "WinAnsiEncoding",
        byte[] currentBytes = null)
    {
        currentBytes ??=
            Encoding.Latin1.GetBytes(text);

        string literal =
            text == "\u20AC"
                ? "(\\200)"
                : $"({text})";

        string content =
            $"BT\n/F1 12 Tf\n{literal} Tj\n(Next) Tj\nET\n";

        int start =
            content.IndexOf(
                literal,
                StringComparison.Ordinal);
        int end =
            content.IndexOf(
                "Tj",
                start,
                StringComparison.Ordinal) + 2;

        Assert.True(
            PdfGraphicsOperator.TryGet(
                "Tj",
                out PdfGraphicsOperator @operator));

        PdfGraphicsOperation operation =
            new(
                @operator!,
                [
                    new PdfLiteralString(
                        Encoding.Latin1.GetString(
                            currentBytes))
                ],
                new PdfContentSourceSpan(
                    0,
                    start,
                    end));

        PdfResolvedFont current =
            CreateFont(
                5,
                "Current Sans",
                "Current Sans Regular",
                400,
                PdfFontSlant.Upright,
                currentEncoding);

        PdfResolvedFont alternate =
            CreateFont(
                6,
                "Alternate Sans",
                "Alternate Sans Bold Italic",
                700,
                PdfFontSlant.Italic,
                alternateEncoding);

        PdfDisplayTextRun run =
            new(
                current,
                12,
                [
                    PdfTextRunElement.CreateText(
                        currentBytes,
                        text)
                ],
                PdfMatrix.Identity,
                new PdfGraphicsStateSnapshot
                {
                    Font = current,
                    FontSize = 12
                },
                operation.SourceSpan,
                markedContent: null);

        return BuildContext(
            content,
            operation,
            run,
            new Dictionary<string, PdfResolvedFont>(
                StringComparer.Ordinal)
            {
                ["F1"] = current,
                ["F2"] = alternate
            });
    }

    private static TestContext CreatePositionedContext()
    {
        const string content =
            "BT\n/F1 12 Tf\n[(A) -120 (B)] TJ\nET\n";

        int start =
            content.IndexOf(
                '[',
                StringComparison.Ordinal);
        int end =
            content.IndexOf(
                "TJ",
                start,
                StringComparison.Ordinal) + 2;

        Assert.True(
            PdfGraphicsOperator.TryGet(
                "TJ",
                out PdfGraphicsOperator @operator));

        PdfGraphicsOperation operation =
            new(
                @operator!,
                [
                    new Objects.PdfArray(
                    [
                        new PdfLiteralString("A"),
                        new PdfInteger(-120),
                        new PdfLiteralString("B")
                    ])
                ],
                new PdfContentSourceSpan(
                    0,
                    start,
                    end));

        PdfResolvedFont current =
            CreateFont(
                5,
                "Current Sans",
                "Current Sans Regular",
                400,
                PdfFontSlant.Upright,
                "WinAnsiEncoding");

        PdfResolvedFont alternate =
            CreateFont(
                6,
                "Alternate Sans",
                "Alternate Sans Bold Italic",
                700,
                PdfFontSlant.Italic,
                "WinAnsiEncoding");

        PdfDisplayTextRun run =
            new(
                current,
                12,
                [
                    PdfTextRunElement.CreateText([0x41], "A"),
                    PdfTextRunElement.CreateAdjustment(-120),
                    PdfTextRunElement.CreateText([0x42], "B")
                ],
                PdfMatrix.Identity,
                new PdfGraphicsStateSnapshot
                {
                    Font = current,
                    FontSize = 12
                },
                operation.SourceSpan,
                markedContent: null);

        return BuildContext(
            content,
            operation,
            run,
            new Dictionary<string, PdfResolvedFont>(
                StringComparer.Ordinal)
            {
                ["F1"] = current,
                ["F2"] = alternate
            });
    }

    private static TestContext BuildContext(
        string content,
        PdfGraphicsOperation operation,
        PdfDisplayTextRun run,
        IReadOnlyDictionary<string, PdfResolvedFont> availableFonts)
    {
        PdfDocument document =
            PdfDocument.CreateEmpty(
                PdfVersion.Pdf20);

        var pageId =
            new PdfObjectIdentifier(3);
        var contentId =
            new PdfObjectIdentifier(4);

        var page =
            new PdfPage(
                612,
                792);

        page.BindObjectIdentifier(
            pageId);
        document.Pages.Add(
            page);

        document.Objects.Add(
            contentId,
            new Document.Objects.PdfStream(
                new Document.Objects.PdfDictionary(),
                Encoding.Latin1.GetBytes(
                    content)));

        int fontStart =
            content.IndexOf(
                "/F1",
                StringComparison.Ordinal);
        int fontEnd =
            content.IndexOf(
                "Tf",
                fontStart,
                StringComparison.Ordinal) + 2;

        Assert.True(
            PdfGraphicsOperator.TryGet(
                "Tf",
                out PdfGraphicsOperator fontOperator));

        PdfGraphicsOperation fontSelection =
            new(
                fontOperator,
                [new PdfName("F1"), new PdfInteger(12)],
                new PdfContentSourceSpan(
                    0,
                    fontStart,
                    fontEnd));

        var source =
            new PdfExistingTextDiscoverySource(
                pageId,
                PdfExistingTextSourceKind.PageContent,
                [fontSelection, operation],
                new PdfDisplayList([run]),
                [contentId],
                hasSharedSourceImpact: false,
                availableFonts);

        var session =
            new PdfEditingSession(
                document);

        return new TestContext(
            document,
            session,
            source,
            contentId);
    }

    private static PdfResolvedFont CreateFont(
        int objectNumber,
        string familyName,
        string faceName,
        int weight,
        PdfFontSlant slant,
        string encoding) =>
        new(
            PdfResourceIdentity.ForIndirect(
                objectNumber,
                0),
            Objects.PdfDictionary.Empty,
            "TrueType",
            familyName,
            faceName,
            weight,
            5,
            slant,
            PdfFontTechnology.TrueType,
            PdfFontEmbeddingRights.Unknown,
            isSubset: false,
            encoding:
                new PdfName(
                    encoding),
            toUnicode: null,
            descriptor: null,
            descendantFonts: null,
            embeddedFontProgram: null,
            variationAxes: []);

    private sealed class TestContext : IDisposable
    {
        public TestContext(
            PdfDocument document,
            PdfEditingSession session,
            PdfExistingTextDiscoverySource source,
            PdfObjectIdentifier contentStreamIdentifier)
        {
            Document = document;
            Session = session;
            Source = source;
            ContentStreamIdentifier =
                contentStreamIdentifier;
        }

        public PdfDocument Document { get; }
        public PdfEditingSession Session { get; }
        public PdfExistingTextDiscoverySource Source { get; }
        public PdfObjectIdentifier ContentStreamIdentifier { get; }

        public void Dispose() =>
            Session.Dispose();
    }
}
