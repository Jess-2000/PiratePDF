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

public sealed class PdfExistingTextTypographyServiceTests
{
    [Fact]
    public void DiscoveryCapturesInterpretedTypography()
    {
        using TestContext context =
            CreateContext();

        PdfExistingTextDescriptor descriptor =
            Discover(context);

        Assert.Equal(12, descriptor.Typography.FontSize);
        Assert.Equal(1, descriptor.Typography.CharacterSpacing);
        Assert.Equal(2, descriptor.Typography.WordSpacing);
        Assert.Equal(90, descriptor.Typography.HorizontalScaling);
        Assert.Equal(0, descriptor.Typography.TextRenderingMode);
        Assert.Equal(0, descriptor.Typography.TextRise);
    }

    [Fact]
    public void ApplyScopesRequestedTextStateAndRestoresOriginalValues()
    {
        using TestContext context =
            CreateContext();

        PdfExistingTextDescriptor descriptor =
            Discover(context);

        _ = PdfExistingTextTypographyService.Apply(
            context.Session,
            context.Session.Document.Pages[0],
            context.Source,
            descriptor,
            new PdfExistingTextTypographyUpdate
            {
                FontSize = 18,
                CharacterSpacing = 3,
                WordSpacing = 4,
                HorizontalScaling = 110,
                TextRenderingMode = 1,
                TextRise = 2
            });

        string content =
            ReadContent(context);

        Assert.Contains("/F1 18 Tf\n3 Tc\n4 Tw\n110 Tz\n1 Tr\n2 Ts\n(Hello) Tj", content, StringComparison.Ordinal);
        Assert.Contains("(Hello) Tj\n/F1 12 Tf\n1 Tc\n2 Tw\n90 Tz\n0 Tr\n0 Ts\n(Next) Tj", content, StringComparison.Ordinal);
    }

    [Fact]
    public void ApplyParticipatesInUndoAndRedo()
    {
        using TestContext context =
            CreateContext();

        PdfExistingTextDescriptor descriptor =
            Discover(context);

        string original =
            ReadContent(context);

        using (var transaction =
            context.Session.BeginTransaction())
        {
            _ = PdfExistingTextTypographyService.Apply(
                context.Session,
                context.Session.Document.Pages[0],
                context.Source,
                descriptor,
                new PdfExistingTextTypographyUpdate
                {
                    TextRise = 5
                });

            _ = transaction.Commit();
        }

        Assert.NotEqual(original, ReadContent(context));

        _ = context.Session.History.Undo();
        Assert.Equal(original, ReadContent(context));

        _ = context.Session.History.Redo();
        Assert.NotEqual(original, ReadContent(context));
    }

    [Fact]
    public void QuoteOperatorIsNormalizedWithScopedTypographyAndUndoRedo()
    {
        using TestContext context =
            CreateContext("'");

        PdfExistingTextDescriptor descriptor =
            Discover(context);

        string original =
            ReadContent(context);

        using (var transaction = context.Session.BeginTransaction())
        {
            _ = PdfExistingTextTypographyService.Apply(
                context.Session,
                context.Session.Document.Pages[0],
                context.Source,
                descriptor,
                new PdfExistingTextTypographyUpdate
                {
                    FontSize = 18
                });

            _ = transaction.Commit();
        }

        const string expected =
            "BT\n/F1 12 Tf\n1 Tc\n2 Tw\n90 Tz\n0 Tr\n0 Ts\n" +
            "/F1 18 Tf\nT*\n(Hello) Tj\n/F1 12 Tf\n(Next) Tj\nET\n";
        Assert.Equal(expected, ReadContent(context));

        _ = context.Session.History.Undo();
        Assert.Equal(original, ReadContent(context));

        _ = context.Session.History.Redo();
        Assert.Equal(expected, ReadContent(context));
    }

    [Fact]
    public void StaleSourceIsRejectedBeforeTypographyInsertion()
    {
        using TestContext context =
            CreateContext();

        PdfExistingTextDescriptor descriptor =
            Discover(context);

        context.Document.Objects
            .Get<Document.Objects.PdfStream>(
                context.ContentStreamIdentifier)
            .ReplaceData(
                Encoding.ASCII.GetBytes(
                    "BT\n/F1 12 Tf\n1 Tc\n2 Tw\n90 Tz\n0 Tr\n0 Ts\n(Other) Tj\n(Next) Tj\nET\n"));

        Assert.Throws<InvalidOperationException>(
            () =>
                PdfExistingTextTypographyService.Apply(
                    context.Session,
                    context.Session.Document.Pages[0],
                    context.Source,
                    descriptor,
                    new PdfExistingTextTypographyUpdate
                    {
                        TextRise = 4
                    }));
    }

    private static PdfExistingTextDescriptor Discover(
        TestContext context) =>
        PdfExistingTextDiscoveryService.Discover(
            context.Session,
            context.Session.Document.Pages[0],
            context.Source)[0];

    private static string ReadContent(TestContext context) =>
        Encoding.ASCII.GetString(
            context.Document.Objects
                .Get<Document.Objects.PdfStream>(
                    context.ContentStreamIdentifier)
                .ToByteArray());

    private static TestContext CreateContext(
        string textOperator = "Tj")
    {
        PdfDocument document =
            PdfDocument.CreateEmpty(
                PdfVersion.Pdf20);

        var pageId = new PdfObjectIdentifier(3);
        var contentId = new PdfObjectIdentifier(4);
        var fontId = new PdfObjectIdentifier(5);

        var page = new PdfPage(612, 792);
        page.BindObjectIdentifier(pageId);
        document.Pages.Add(page);

        string show =
            textOperator == "Tj"
                ? "(Hello) Tj"
                : "(Hello) '";

        string content =
            $"BT\n/F1 12 Tf\n1 Tc\n2 Tw\n90 Tz\n0 Tr\n0 Ts\n{show}\n(Next) Tj\nET\n";

        document.Objects.Add(
            contentId,
            new Document.Objects.PdfStream(
                new Document.Objects.PdfDictionary(),
                Encoding.ASCII.GetBytes(content)));

        int start =
            content.IndexOf("(Hello)", StringComparison.Ordinal);
        int end =
            content.IndexOf(textOperator, start, StringComparison.Ordinal) +
            textOperator.Length;

        int fontStart =
            content.IndexOf("/F1", StringComparison.Ordinal);
        int fontEnd =
            content.IndexOf("Tf", fontStart, StringComparison.Ordinal) + 2;

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

        Assert.True(
            PdfGraphicsOperator.TryGet(
                textOperator,
                out PdfGraphicsOperator @operator));

        PdfGraphicsOperation operation =
            new(
                @operator,
                [new PdfLiteralString("Hello")],
                new PdfContentSourceSpan(0, start, end));

        PdfResolvedFont font =
            new(
                PdfResourceIdentity.ForIndirect(
                    fontId.ObjectNumber,
                    fontId.GenerationNumber),
                BlackFlag.Engine.Objects.PdfDictionary.Empty,
                "TrueType",
                "Test Sans",
                "Test Sans Regular",
                400,
                5,
                PdfFontSlant.Upright,
                PdfFontTechnology.TrueType,
                PdfFontEmbeddingRights.Unknown,
                isSubset: false,
                encoding:
                    new PdfName(
                        "WinAnsiEncoding"),
                toUnicode: null,
                descriptor: null,
                descendantFonts: null,
                embeddedFontProgram: null,
                variationAxes: []);

        PdfDisplayTextRun run =
            new(
                font,
                12,
                [
                    PdfTextRunElement.CreateText(
                        Encoding.ASCII.GetBytes("Hello"),
                        "Hello")
                ],
                PdfMatrix.Identity,
                new PdfGraphicsStateSnapshot
                {
                    Font = font,
                    FontSize = 12,
                    CharacterSpacing = 1,
                    WordSpacing = 2,
                    HorizontalScaling = 90,
                    TextRenderingMode = 0,
                    TextRise = 0
                },
                operation.SourceSpan,
                markedContent: null);

        var source =
            new PdfExistingTextDiscoverySource(
                pageId,
                PdfExistingTextSourceKind.PageContent,
                [fontSelection, operation],
                new PdfDisplayList([run]),
                [contentId]);

        var session =
            new PdfEditingSession(document);

        return new TestContext(
            document,
            session,
            source,
            contentId);
    }

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
            ContentStreamIdentifier = contentStreamIdentifier;
        }

        public PdfDocument Document { get; }
        public PdfEditingSession Session { get; }
        public PdfExistingTextDiscoverySource Source { get; }
        public PdfObjectIdentifier ContentStreamIdentifier { get; }

        public void Dispose() => Session.Dispose();
    }
}
