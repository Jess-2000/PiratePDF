using BlackFlag.Engine.Document.Model;
using BlackFlag.Engine.Document.Objects;
using BlackFlag.Engine.Document.Pages;
using BlackFlag.Engine.Editing.Documents;
using BlackFlag.Engine.Editing.Pages;
using BlackFlag.Engine.Editing.Text;
using BlackFlag.Engine.Objects;
using BlackFlag.Engine.Rendering.DisplayList;
using BlackFlag.Engine.Rendering.Graphics;
using BlackFlag.Engine.Rendering.Resources;
using BlackFlag.Engine.Services.Text;
using GraphicsPdfArray = BlackFlag.Engine.Objects.PdfArray;
using GraphicsPdfDictionary = BlackFlag.Engine.Objects.PdfDictionary;
using GraphicsPdfObject = BlackFlag.Engine.Objects.PdfObject;

namespace BlackFlag.Engine.Services.Tests.Text;

public sealed class PdfExistingTextDiscoveryServiceTests
{
    [Fact]
    public void DiscoversDirectPageTextWithDeterministicProvenance()
    {
        using PdfEditingSession session =
            CreateSession();

        PdfEditablePage page =
            session.Document.Pages[0];

        PdfResolvedFont font =
            CreateFont(
                PdfResourceIdentity.ForIndirect(
                    14,
                    2));

        PdfGraphicsOperation[] operations =
        [
            Operation("BT", 0, 0),
            Operation(
                "Tf",
                0,
                10,
                new PdfName("F7"),
                new PdfInteger(12)),
            Operation(
                "Tj",
                0,
                20,
                new PdfLiteralString("Hello")),
            Operation("ET", 0, 30)
        ];

        PdfDisplayTextRun run =
            CreateRun(
                font,
                operations[2].SourceSpan,
                "Hello");

        var source =
            new PdfExistingTextDiscoverySource(
                new PdfObjectIdentifier(4),
                PdfExistingTextSourceKind.PageContent,
                operations,
                new PdfDisplayList([run]),
                [new PdfObjectIdentifier(9)]);

        PdfExistingTextDescriptor descriptor =
            Assert.Single(
                PdfExistingTextDiscoveryService.Discover(
                    session,
                    page,
                    source));

        Assert.Equal(
            session.Id,
            descriptor.Identity.SessionId);
        Assert.Equal(
            page.Id,
            descriptor.Identity.PageId);
        Assert.Equal(
            new PdfObjectIdentifier(4),
            descriptor.Identity.ContentOwnerIdentifier);
        Assert.Equal(
            new PdfObjectIdentifier(9),
            descriptor.Identity.ContentStreamIdentifier);
        Assert.Equal(
            2,
            descriptor.Identity.OperationIndex);
        Assert.Equal(
            0,
            descriptor.Identity.RunOrdinal);
        Assert.Equal(
            20,
            descriptor.Identity.StartOffset);
        Assert.Equal(
            "F7",
            descriptor.FontResourceName);
        Assert.Equal(
            new PdfObjectIdentifier(14, 2),
            descriptor.FontObjectIdentifier);
        Assert.Equal(
            "Hello",
            descriptor.UnicodeText);
        Assert.Equal(
            PdfExistingTextEditability.DirectlyEditable,
            descriptor.Editability);
    }

    [Fact]
    public void SameOperationRunsReceiveStableDistinctOrdinals()
    {
        using PdfEditingSession session =
            CreateSession();

        PdfResolvedFont font =
            CreateFont(
                PdfResourceIdentity.ForLocal(11));

        PdfGraphicsOperation operation =
            Operation(
                "Tj",
                0,
                10,
                new PdfLiteralString("AB"));

        PdfContentSourceSpan span =
            operation.SourceSpan;

        var source =
            new PdfExistingTextDiscoverySource(
                new PdfObjectIdentifier(4),
                PdfExistingTextSourceKind.PageContent,
                [operation],
                new PdfDisplayList(
                [
                    CreateRun(font, span, "A"),
                    CreateRun(font, span, "B")
                ]));

        IReadOnlyList<PdfExistingTextDescriptor> result =
            PdfExistingTextDiscoveryService.Discover(
                session,
                session.Document.Pages[0],
                source);

        Assert.Equal(
            2,
            result.Count);
        Assert.Equal(
            0,
            result[0].Identity.RunOrdinal);
        Assert.Equal(
            1,
            result[1].Identity.RunOrdinal);
        Assert.NotEqual(
            result[0].Identity,
            result[1].Identity);
    }

    [Fact]
    public void SharedFormTextIsClassifiedWithSharedSourceImpact()
    {
        using PdfEditingSession session =
            CreateSession();

        PdfResolvedFont font =
            CreateFont(
                PdfResourceIdentity.ForLocal(12));

        PdfGraphicsOperation operation =
            Operation(
                "Tj",
                0,
                5,
                new PdfLiteralString("Shared"));

        var source =
            new PdfExistingTextDiscoverySource(
                new PdfObjectIdentifier(18),
                PdfExistingTextSourceKind.FormXObject,
                [operation],
                new PdfDisplayList(
                [
                    CreateRun(
                        font,
                        operation.SourceSpan,
                        "Shared")
                ]),
                hasSharedSourceImpact: true);

        PdfExistingTextDescriptor descriptor =
            Assert.Single(
                PdfExistingTextDiscoveryService.Discover(
                    session,
                    session.Document.Pages[0],
                    source));

        Assert.Equal(
            PdfExistingTextSourceKind.FormXObject,
            descriptor.Identity.SourceKind);
        Assert.Equal(
            new PdfObjectIdentifier(18),
            descriptor.Identity.ContentOwnerIdentifier);
        Assert.Equal(
            PdfExistingTextEditability.EditableWithSharedSourceImpact,
            descriptor.Editability);
        Assert.Contains(
            "multiple placements",
            descriptor.EditabilityReason,
            StringComparison.Ordinal);
    }

    [Fact]
    public void CompositeTextArrayUsesPositionPreservingClassification()
    {
        using PdfEditingSession session =
            CreateSession();

        PdfResolvedFont font =
            CreateFont(
                PdfResourceIdentity.ForLocal(13));

        PdfGraphicsOperation operation =
            Operation(
                "TJ",
                0,
                5,
                new GraphicsPdfArray(
                [
                    new PdfLiteralString("A"),
                    new PdfInteger(-120),
                    new PdfLiteralString("B")
                ]));

        PdfDisplayTextRun run =
            new(
                font,
                12,
                [
                    PdfTextRunElement.CreateText(
                        [65],
                        "A"),
                    PdfTextRunElement.CreateAdjustment(
                        -120),
                    PdfTextRunElement.CreateText(
                        [66],
                        "B")
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
                new PdfObjectIdentifier(4),
                PdfExistingTextSourceKind.PageContent,
                [operation],
                new PdfDisplayList([run]));

        PdfExistingTextDescriptor descriptor =
            Assert.Single(
                PdfExistingTextDiscoveryService.Discover(
                    session,
                    session.Document.Pages[0],
                    source));

        Assert.Equal(
            PdfExistingTextEditability.EditableWithPositionPreservation,
            descriptor.Editability);
        Assert.NotEqual(
            PdfExistingTextEditability.DirectlyEditable,
            descriptor.Editability);
        Assert.Contains(
            "position",
            descriptor.EditabilityReason,
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void MissingOperationProvenanceFailsRatherThanFuzzyMatching()
    {
        using PdfEditingSession session =
            CreateSession();

        PdfResolvedFont font =
            CreateFont(
                PdfResourceIdentity.ForLocal(14));

        PdfGraphicsOperation operation =
            Operation(
                "Tj",
                0,
                10,
                new PdfLiteralString("Source"));

        PdfDisplayTextRun run =
            CreateRun(
                font,
                new PdfContentSourceSpan(
                    0,
                    100,
                    110),
                "Source");

        var source =
            new PdfExistingTextDiscoverySource(
                new PdfObjectIdentifier(4),
                PdfExistingTextSourceKind.PageContent,
                [operation],
                new PdfDisplayList([run]));

        InvalidOperationException exception =
            Assert.Throws<InvalidOperationException>(
                () =>
                    PdfExistingTextDiscoveryService.Discover(
                        session,
                        session.Document.Pages[0],
                        source));

        Assert.Contains(
            "does not map",
            exception.Message,
            StringComparison.Ordinal);
    }

    [Fact]
    public void DiscoveryRejectsPageOwnedByAnotherSession()
    {
        PdfDocument firstDocument =
            CreateDocument();
        PdfDocument secondDocument =
            CreateDocument();

        using PdfEditingSession first =
            new(firstDocument);
        using PdfEditingSession second =
            new(secondDocument);

        PdfResolvedFont font =
            CreateFont(
                PdfResourceIdentity.ForLocal(15));

        PdfGraphicsOperation operation =
            Operation(
                "Tj",
                0,
                0,
                new PdfLiteralString("Owner"));

        var source =
            new PdfExistingTextDiscoverySource(
                new PdfObjectIdentifier(4),
                PdfExistingTextSourceKind.PageContent,
                [operation],
                new PdfDisplayList(
                [
                    CreateRun(
                        font,
                        operation.SourceSpan,
                        "Owner")
                ]));

        Assert.ThrowsAny<Exception>(
            () =>
                PdfExistingTextDiscoveryService.Discover(
                    first,
                    second.Document.Pages[0],
                    source));
    }

    private static PdfEditingSession CreateSession() =>
        new(
            CreateDocument());

    private static PdfDocument CreateDocument()
    {
        PdfDocument document =
            PdfDocument.CreateEmpty();

        var page =
            new PdfPage(
                612,
                792);

        page.BindObjectIdentifier(
            new PdfObjectIdentifier(4));

        document.Pages.Add(
            page);

        return document;
    }

    private static PdfDisplayTextRun CreateRun(
        PdfResolvedFont font,
        PdfContentSourceSpan span,
        string text) =>
        new(
            font,
            12,
            [
                PdfTextRunElement.CreateText(
                    System.Text.Encoding.ASCII.GetBytes(
                        text),
                    text)
            ],
            PdfMatrix.Identity,
            new PdfGraphicsStateSnapshot
            {
                Font = font,
                FontSize = 12
            },
            span,
            markedContent: null);

    private static PdfGraphicsOperation Operation(
        string code,
        int streamIndex,
        long offset,
        params GraphicsPdfObject[] operands)
    {
        Assert.True(
            PdfGraphicsOperator.TryGet(
                code,
                out PdfGraphicsOperator @operator));

        return new PdfGraphicsOperation(
            @operator!,
            operands,
            new PdfContentSourceSpan(
                streamIndex,
                offset,
                offset + 5));
    }

    private static PdfResolvedFont CreateFont(
        PdfResourceIdentity identity) =>
        new(
            identity,
            GraphicsPdfDictionary.Empty,
            "TrueType",
            "Test Sans",
            "Test Sans Regular",
            400,
            5,
            PdfFontSlant.Upright,
            PdfFontTechnology.TrueType,
            PdfFontEmbeddingRights.Unknown,
            isSubset: false,
            encoding: null,
            toUnicode: null,
            descriptor: null,
            descendantFonts: null,
            embeddedFontProgram: null,
            variationAxes: []);
}
