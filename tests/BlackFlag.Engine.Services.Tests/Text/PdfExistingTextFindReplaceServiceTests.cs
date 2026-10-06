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
using GraphicsPdfArray = BlackFlag.Engine.Objects.PdfArray;

namespace BlackFlag.Engine.Services.Tests.Text;

public sealed class PdfExistingTextFindReplaceServiceTests
{
    [Fact]
    public void FindReturnsProvenanceBackedDirectMatches()
    {
        using TestContext context =
            CreateDirectContext(
                "Alpha alpha ALPHA");

        IReadOnlyList<PdfExistingTextSearchMatch> matches =
            PdfExistingTextFindReplaceService.Find(
                context.Session,
                context.Scopes,
                "alpha",
                matchCase: false);

        Assert.Equal(
            3,
            matches.Count);

        Assert.All(
            matches,
            match =>
            {
                Assert.Equal(
                    context.Session.Id,
                    match.Descriptor.Identity.SessionId);
                Assert.Equal(
                    0,
                    match.ScopeIndex);
                Assert.Equal(
                    0,
                    match.TextElementIndex);
            });
    }

    [Fact]
    public void PositionedFindDoesNotCreateCrossSegmentMatch()
    {
        using TestContext context =
            CreatePositionedContext();

        Assert.Empty(
            PdfExistingTextFindReplaceService.Find(
                context.Session,
                context.Scopes,
                "AB"));

        IReadOnlyList<PdfExistingTextSearchMatch> matches =
            PdfExistingTextFindReplaceService.Find(
                context.Session,
                context.Scopes,
                "A");

        PdfExistingTextSearchMatch match =
            Assert.Single(
                matches);

        Assert.Equal(
            0,
            match.TextElementIndex);
    }

    [Fact]
    public void ReplaceAllDirectMatchesCommitsOneUndoableTransaction()
    {
        using TestContext context =
            CreateDirectContext(
                "cat cat");

        PdfExistingTextFindReplaceResult result =
            PdfExistingTextFindReplaceService.ReplaceAll(
                context.Session,
                context.Scopes,
                "cat",
                "dog");

        Assert.Equal(
            2,
            result.MatchCount);
        Assert.Single(
            result.ChangeSet);
        Assert.Contains(
            "(dog dog) Tj",
            ReadStream(
                context,
                context.StreamIdentifiers[0]),
            StringComparison.Ordinal);

        _ = context.Session.History.Undo();

        Assert.Contains(
            "(cat cat) Tj",
            ReadStream(
                context,
                context.StreamIdentifiers[0]),
            StringComparison.Ordinal);

        _ = context.Session.History.Redo();

        Assert.Contains(
            "(dog dog) Tj",
            ReadStream(
                context,
                context.StreamIdentifiers[0]),
            StringComparison.Ordinal);
    }

    [Fact]
    public void ReplaceAllPositionedMatchesPreservesSegmentBoundaries()
    {
        using TestContext context =
            CreatePositionedContext();

        PdfExistingTextFindReplaceResult result =
            PdfExistingTextFindReplaceService.ReplaceAll(
                context.Session,
                context.Scopes,
                "A",
                "X");

        Assert.Single(
            result.Matches);

        Assert.Contains(
            "[(X) -120 (B)] TJ",
            ReadStream(
                context,
                context.StreamIdentifiers[0]),
            StringComparison.Ordinal);
    }

    [Fact]
    public void ReplaceAllOrdersSameStreamTargetsFromHigherOffsetsDown()
    {
        using TestContext context =
            CreateTwoOperationContext();

        PdfExistingTextFindReplaceResult result =
            PdfExistingTextFindReplaceService.ReplaceAll(
                context.Session,
                context.Scopes,
                "A",
                "Longer");

        Assert.Equal(
            2,
            result.MatchCount);

        string content =
            ReadStream(
                context,
                context.StreamIdentifiers[0]);

        Assert.Equal(
            "BT\n/F1 12 Tf\n(Longer) Tj\n(Longer) Tj\nET\n",
            content);
    }

    [Fact]
    public void FailureRollsBackEarlierReplaceAllMutations()
    {
        using TestContext context =
            CreateTwoOperationContext();

        PdfExistingTextDescriptor[] descriptors =
        [
            .. PdfExistingTextDiscoveryService.Discover(
                context.Session,
                context.Session.Document.Pages[0],
                context.Scopes[0].Source)
        ];

        Assert.Equal(
            2,
            descriptors.Length);

        byte[] stale =
            Encoding.ASCII.GetBytes(
                "BT\n/F1 12 Tf\n(Q) Tj\n(A) Tj\nET\n");

        context.Document.Objects
            .Get<Document.Objects.PdfStream>(
                context.StreamIdentifiers[0])
            .ReplaceData(
                stale);

        Assert.Throws<InvalidOperationException>(
            () =>
                PdfExistingTextFindReplaceService.ReplaceAll(
                    context.Session,
                    context.Scopes,
                    "A",
                    "Longer"));

        Assert.Equal(
            Encoding.ASCII.GetString(
                stale),
            ReadStream(
                context,
                context.StreamIdentifiers[0]));
    }

    [Fact]
    public void NoMatchesDoesNotCreateHistory()
    {
        using TestContext context =
            CreateDirectContext(
                "Alpha");

        Assert.Throws<InvalidOperationException>(
            () =>
                PdfExistingTextFindReplaceService.ReplaceAll(
                    context.Session,
                    context.Scopes,
                    "Missing",
                    "Replacement"));

        Assert.False(
            context.Session.History.CanUndo);
    }

    private static TestContext CreateDirectContext(
        string text)
    {
        string content =
            $"BT\n/F1 12 Tf\n({text}) Tj\nET\n";

        int start =
            content.IndexOf(
                $"({text})",
                StringComparison.Ordinal);
        int end =
            content.IndexOf(
                "Tj",
                start,
                StringComparison.Ordinal) + 2;

        PdfGraphicsOperation operation =
            CreateOperation(
                "Tj",
                start,
                end,
                new PdfLiteralString(
                    text));

        PdfResolvedFont font =
            CreateFont();

        PdfDisplayTextRun run =
            CreateRun(
                font,
                operation.SourceSpan,
                [
                    PdfTextRunElement.CreateText(
                        Encoding.ASCII.GetBytes(
                            text),
                        text)
                ]);

        return CreateContext(
            content,
            [operation],
            [run]);
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

        PdfGraphicsOperation operation =
            CreateOperation(
                "TJ",
                start,
                end,
                new GraphicsPdfArray(
                [
                    new PdfLiteralString("A"),
                    new PdfInteger(-120),
                    new PdfLiteralString("B")
                ]));

        PdfResolvedFont font =
            CreateFont();

        PdfDisplayTextRun run =
            CreateRun(
                font,
                operation.SourceSpan,
                [
                    PdfTextRunElement.CreateText(
                        [0x41],
                        "A"),
                    PdfTextRunElement.CreateAdjustment(
                        -120),
                    PdfTextRunElement.CreateText(
                        [0x42],
                        "B")
                ]);

        return CreateContext(
            content,
            [operation],
            [run]);
    }

    private static TestContext CreateTwoOperationContext()
    {
        const string content =
            "BT\n/F1 12 Tf\n(A) Tj\n(A) Tj\nET\n";

        int firstStart =
            content.IndexOf(
                "(A)",
                StringComparison.Ordinal);
        int firstEnd =
            content.IndexOf(
                "Tj",
                firstStart,
                StringComparison.Ordinal) + 2;

        int secondStart =
            content.IndexOf(
                "(A)",
                firstEnd,
                StringComparison.Ordinal);
        int secondEnd =
            content.IndexOf(
                "Tj",
                secondStart,
                StringComparison.Ordinal) + 2;

        PdfGraphicsOperation first =
            CreateOperation(
                "Tj",
                firstStart,
                firstEnd,
                new PdfLiteralString("A"));

        PdfGraphicsOperation second =
            CreateOperation(
                "Tj",
                secondStart,
                secondEnd,
                new PdfLiteralString("A"));

        PdfResolvedFont font =
            CreateFont();

        return CreateContext(
            content,
            [first, second],
            [
                CreateRun(
                    font,
                    first.SourceSpan,
                    [
                        PdfTextRunElement.CreateText(
                            [0x41],
                            "A")
                    ]),
                CreateRun(
                    font,
                    second.SourceSpan,
                    [
                        PdfTextRunElement.CreateText(
                            [0x41],
                            "A")
                    ])
            ]);
    }

    private static TestContext CreateContext(
        string content,
        IReadOnlyList<PdfGraphicsOperation> operations,
        IReadOnlyList<PdfDisplayTextRun> runs)
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
                Encoding.ASCII.GetBytes(
                    content)));

        var source =
            new PdfExistingTextDiscoverySource(
                pageId,
                PdfExistingTextSourceKind.PageContent,
                operations,
                new PdfDisplayList(
                    runs),
                [contentId]);

        var session =
            new PdfEditingSession(
                document);

        return new TestContext(
            document,
            session,
            [
                new PdfExistingTextSearchScope(
                    session.Document.Pages[0],
                    source)
            ],
            [contentId]);
    }

    private static PdfGraphicsOperation CreateOperation(
        string code,
        int start,
        int end,
        params Objects.PdfObject[] operands)
    {
        Assert.True(
            PdfGraphicsOperator.TryGet(
                code,
                out PdfGraphicsOperator @operator));

        return new PdfGraphicsOperation(
            @operator,
            operands,
            new PdfContentSourceSpan(
                0,
                start,
                end));
    }

    private static PdfDisplayTextRun CreateRun(
        PdfResolvedFont font,
        PdfContentSourceSpan span,
        IEnumerable<PdfTextRunElement> elements) =>
        new(
            font,
            12,
            elements,
            PdfMatrix.Identity,
            new PdfGraphicsStateSnapshot
            {
                Font = font,
                FontSize = 12
            },
            span,
            markedContent: null);

    private static PdfResolvedFont CreateFont() =>
        new(
            PdfResourceIdentity.ForIndirect(
                5,
                0),
            Objects.PdfDictionary.Empty,
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

    private static string ReadStream(
        TestContext context,
        PdfObjectIdentifier identifier) =>
        Encoding.ASCII.GetString(
            context.Document.Objects
                .Get<Document.Objects.PdfStream>(
                    identifier)
                .ToByteArray());

    private sealed class TestContext :
        IDisposable
    {
        public TestContext(
            PdfDocument document,
            PdfEditingSession session,
            IReadOnlyList<PdfExistingTextSearchScope> scopes,
            IReadOnlyList<PdfObjectIdentifier> streamIdentifiers)
        {
            Document = document;
            Session = session;
            Scopes = scopes;
            StreamIdentifiers = streamIdentifiers;
        }

        public PdfDocument Document { get; }

        public PdfEditingSession Session { get; }

        public IReadOnlyList<PdfExistingTextSearchScope> Scopes { get; }

        public IReadOnlyList<PdfObjectIdentifier> StreamIdentifiers { get; }

        public void Dispose()
        {
            Session.Dispose();
        }
    }
}
