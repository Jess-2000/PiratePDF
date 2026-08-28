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

public sealed class PdfPositionedTextReplacementServiceTests
{
    [Fact]
    public void DiscoveryClassifiesCompositeTjAsPositionPreserving()
    {
        using TestContext context =
            CreateContext();

        PdfExistingTextDescriptor descriptor =
            Discover(
                context);

        Assert.Equal(
            PdfExistingTextEditability.EditableWithPositionPreservation,
            descriptor.Editability);

        Assert.Contains(
            "position",
            descriptor.EditabilityReason,
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ReplacementPreservesNumericAdjustmentsAndTokenKinds()
    {
        using TestContext context =
            CreateContext();

        PdfExistingTextDescriptor descriptor =
            Discover(
                context);

        _ = PdfPositionedTextReplacementService.Replace(
            context.Session,
            context.Session.Document.Pages[0],
            context.Source,
            descriptor,
            ["X", "Y", "Z"]);

        Assert.Equal(
            "BT\n/F1 12 Tf\n[(X) -120 <59> 35 (Z)] TJ\nET\n",
            ReadContent(
                context));
    }

    [Fact]
    public void ReplacementEscapesLiteralSegmentsWithoutChangingPositioningSyntax()
    {
        using TestContext context =
            CreateContext();

        PdfExistingTextDescriptor descriptor =
            Discover(
                context);

        _ = PdfPositionedTextReplacementService.Replace(
            context.Session,
            context.Session.Document.Pages[0],
            context.Source,
            descriptor,
            ["(X)", "Y", "Z\\"]);

        Assert.Equal(
            "BT\n/F1 12 Tf\n[(\\(X\\)) -120 <59> 35 (Z\\\\)] TJ\nET\n",
            ReadContent(
                context));
    }

    [Fact]
    public void ReplacementParticipatesInUndoAndRedo()
    {
        using TestContext context =
            CreateContext();

        PdfExistingTextDescriptor descriptor =
            Discover(
                context);

        using (var transaction =
            context.Session.BeginTransaction())
        {
            _ = PdfPositionedTextReplacementService.Replace(
                context.Session,
                context.Session.Document.Pages[0],
                context.Source,
                descriptor,
                ["X", "Y", "Z"]);

            _ = transaction.Commit();
        }

        Assert.Contains(
            "[(X) -120 <59> 35 (Z)] TJ",
            ReadContent(
                context),
            StringComparison.Ordinal);

        _ = context.Session.History.Undo();

        Assert.Contains(
            "[(A) -120 <42> 35 (C)] TJ",
            ReadContent(
                context),
            StringComparison.Ordinal);

        _ = context.Session.History.Redo();

        Assert.Contains(
            "[(X) -120 <59> 35 (Z)] TJ",
            ReadContent(
                context),
            StringComparison.Ordinal);
    }

    [Fact]
    public void WrongReplacementSegmentCountIsRejectedWithoutMutation()
    {
        using TestContext context =
            CreateContext();

        PdfExistingTextDescriptor descriptor =
            Discover(
                context);

        Assert.Throws<ArgumentException>(
            () =>
                PdfPositionedTextReplacementService.Replace(
                    context.Session,
                    context.Session.Document.Pages[0],
                    context.Source,
                    descriptor,
                    ["Only", "Two"]));

        Assert.Contains(
            "[(A) -120 <42> 35 (C)] TJ",
            ReadContent(
                context),
            StringComparison.Ordinal);
    }

    [Fact]
    public void StaleSegmentBytesAreRejectedInsteadOfRedistributed()
    {
        using TestContext context =
            CreateContext();

        PdfExistingTextDescriptor descriptor =
            Discover(
                context);

        context.Document.Objects
            .Get<Document.Objects.PdfStream>(
                context.ContentStreamIdentifier)
            .ReplaceData(
                Encoding.ASCII.GetBytes(
                    "BT\n/F1 12 Tf\n[(Q) -120 <42> 35 (C)] TJ\nET\n"));

        Assert.Throws<InvalidOperationException>(
            () =>
                PdfPositionedTextReplacementService.Replace(
                    context.Session,
                    context.Session.Document.Pages[0],
                    context.Source,
                    descriptor,
                    ["X", "Y", "Z"]));
    }

    [Fact]
    public void UnmappableSegmentIsRejectedWithoutMutation()
    {
        using TestContext context =
            CreateContext();

        PdfExistingTextDescriptor descriptor =
            Discover(
                context);

        Assert.Throws<NotSupportedException>(
            () =>
                PdfPositionedTextReplacementService.Replace(
                    context.Session,
                    context.Session.Document.Pages[0],
                    context.Source,
                    descriptor,
                    ["X", "\u03A9", "Z"]));

        Assert.Contains(
            "[(A) -120 <42> 35 (C)] TJ",
            ReadContent(
                context),
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
        Encoding.ASCII.GetString(
            context.Document.Objects
                .Get<Document.Objects.PdfStream>(
                    context.ContentStreamIdentifier)
                .ToByteArray());

    private static TestContext CreateContext()
    {
        PdfDocument document =
            PdfDocument.CreateEmpty(
                PdfVersion.Pdf20);

        var pageId =
            new PdfObjectIdentifier(3);
        var contentId =
            new PdfObjectIdentifier(4);
        var fontId =
            new PdfObjectIdentifier(5);

        var page =
            new PdfPage(
                612,
                792);

        page.BindObjectIdentifier(
            pageId);

        document.Pages.Add(
            page);

        string content =
            "BT\n/F1 12 Tf\n[(A) -120 <42> 35 (C)] TJ\nET\n";

        document.Objects.Add(
            contentId,
            new Document.Objects.PdfStream(
                new Document.Objects.PdfDictionary(),
                Encoding.ASCII.GetBytes(
                    content)));

        int operationStart =
            content.IndexOf(
                '[',
                StringComparison.Ordinal);

        int operationEnd =
            content.IndexOf(
                "TJ",
                operationStart,
                StringComparison.Ordinal) + 2;

        Assert.True(
            PdfGraphicsOperator.TryGet(
                "TJ",
                out PdfGraphicsOperator @operator));

        PdfGraphicsOperation operation =
            new(
                @operator!,
                [
                    new GraphicsPdfArray(
                    [
                        new PdfLiteralString("A"),
                        new PdfInteger(-120),
                        new PdfHexString(
                            new byte[] { 0x42 }),
                        new PdfInteger(35),
                        new PdfLiteralString("C")
                    ])
                ],
                new PdfContentSourceSpan(
                    0,
                    operationStart,
                    operationEnd));

        PdfResolvedFont font =
            new(
                PdfResourceIdentity.ForIndirect(
                    fontId.ObjectNumber,
                    fontId.GenerationNumber),
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

        PdfDisplayTextRun run =
            new(
                font,
                12,
                [
                    PdfTextRunElement.CreateText(
                        [0x41],
                        "A"),
                    PdfTextRunElement.CreateAdjustment(
                        -120),
                    PdfTextRunElement.CreateText(
                        [0x42],
                        "B"),
                    PdfTextRunElement.CreateAdjustment(
                        35),
                    PdfTextRunElement.CreateText(
                        [0x43],
                        "C")
                ],
                PdfMatrix.Identity,
                new PdfGraphicsStateSnapshot
                {
                    Font = font,
                    FontSize = 12
                },
                operation.SourceSpan,
                markedContent: null);

        var source =
            new PdfExistingTextDiscoverySource(
                pageId,
                PdfExistingTextSourceKind.PageContent,
                [operation],
                new PdfDisplayList(
                    [run]),
                [contentId]);

        var session =
            new PdfEditingSession(
                document);

        return new TestContext(
            document,
            session,
            source,
            contentId);
    }

    private sealed class TestContext :
        IDisposable
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

        public void Dispose()
        {
            Session.Dispose();
        }
    }
}
