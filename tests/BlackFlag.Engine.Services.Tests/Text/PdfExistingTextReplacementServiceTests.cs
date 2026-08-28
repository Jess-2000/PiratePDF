using System.Text;
using BlackFlag.Engine.Document.Model;
using BlackFlag.Engine.Document.Objects;
using BlackFlag.Engine.Document.Pages;
using BlackFlag.Engine.Editing.Documents;
using BlackFlag.Engine.Editing.Operations;
using BlackFlag.Engine.Editing.Text;
using BlackFlag.Engine.Editing.Transactions;
using BlackFlag.Engine.Objects;
using BlackFlag.Engine.Rendering.DisplayList;
using BlackFlag.Engine.Rendering.Graphics;
using BlackFlag.Engine.Rendering.Resources;
using BlackFlag.Engine.Services.Editing;
using BlackFlag.Engine.Services.Text;
using BlackFlag.Engine.Writing;
using DocumentPdfArray = BlackFlag.Engine.Document.Objects.PdfArray;
using DocumentPdfIndirectReference = BlackFlag.Engine.Document.Objects.PdfIndirectReference;
using DocumentPdfInteger = BlackFlag.Engine.Document.Objects.Primitives.PdfInteger;
using DocumentPdfName = BlackFlag.Engine.Document.Objects.Primitives.PdfName;

namespace BlackFlag.Engine.Services.Tests.Text;

public sealed class PdfExistingTextReplacementServiceTests
{
    private static readonly DocumentPdfName TypeName =
        DocumentPdfName.FromLatin1("Type");
    private static readonly DocumentPdfName PagesName =
        DocumentPdfName.FromLatin1("Pages");
    private static readonly DocumentPdfName PageName =
        DocumentPdfName.FromLatin1("Page");
    private static readonly DocumentPdfName KidsName =
        DocumentPdfName.FromLatin1("Kids");
    private static readonly DocumentPdfName CountName =
        DocumentPdfName.FromLatin1("Count");
    private static readonly DocumentPdfName ParentName =
        DocumentPdfName.FromLatin1("Parent");
    private static readonly DocumentPdfName ContentsName =
        DocumentPdfName.FromLatin1("Contents");

    [Fact]
    public void ReplacementMutatesOnlySourceStringAndParticipatesInUndoRedo()
    {
        TestContext context =
            CreateContext(
                PdfVersion.Pdf17,
                "Hello");

        using PdfEditingSession session =
            new(
                context.Document);

        PdfExistingTextDescriptor descriptor =
            Discover(
                session,
                context);

        PdfEditChangeSet changeSet;

        using (var transaction =
            session.BeginTransaction())
        {
            PdfExistingTextEditOperation operation =
                PdfExistingTextReplacementService.Replace(
                    session,
                    session.Document.Pages[0],
                    context.Source,
                    descriptor,
                    "World");

            Assert.Equal(
                context.ContentStreamIdentifier,
                operation.ContentStreamIdentifier);

            changeSet =
                transaction.Commit();
        }

        Assert.Equal(
            "BT\n/F1 12 Tf\n(World) Tj\nET\n",
            ReadContent(
                context));

        _ = session.History.Undo();

        Assert.Equal(
            "BT\n/F1 12 Tf\n(Hello) Tj\nET\n",
            ReadContent(
                context));

        _ = session.History.Redo();

        Assert.Equal(
            "BT\n/F1 12 Tf\n(World) Tj\nET\n",
            ReadContent(
                context));

        Assert.Single(
            changeSet);
    }

    [Fact]
    public void DeletionWritesEmptyStringWithoutChangingOperatorSyntax()
    {
        TestContext context =
            CreateContext(
                PdfVersion.Pdf17,
                "Hello");

        using PdfEditingSession session =
            new(
                context.Document);

        PdfExistingTextDescriptor descriptor =
            Discover(
                session,
                context);

        _ = PdfExistingTextReplacementService.Replace(
            session,
            session.Document.Pages[0],
            context.Source,
            descriptor,
            string.Empty);

        Assert.Equal(
            "BT\n/F1 12 Tf\n() Tj\nET\n",
            ReadContent(
                context));
    }

    [Fact]
    public void ReplacementEscapesLiteralStringDelimitersWithoutChangingOtherSyntax()
    {
        TestContext context =
            CreateContext(
                PdfVersion.Pdf17,
                "Hello");

        using PdfEditingSession session =
            new(
                context.Document);

        PdfExistingTextDescriptor descriptor =
            Discover(
                session,
                context);

        _ = PdfExistingTextReplacementService.Replace(
            session,
            session.Document.Pages[0],
            context.Source,
            descriptor,
            "(A)\\B");

        Assert.Equal(
            "BT\n/F1 12 Tf\n(\\(A\\)\\\\B) Tj\nET\n",
            ReadContent(
                context));
    }

    [Fact]
    public void UnmappableReplacementIsRejectedBeforeMutation()
    {
        TestContext context =
            CreateContext(
                PdfVersion.Pdf17,
                "Hello");

        using PdfEditingSession session =
            new(
                context.Document);

        PdfExistingTextDescriptor descriptor =
            Discover(
                session,
                context);

        Assert.Throws<NotSupportedException>(
            () =>
                PdfExistingTextReplacementService.Replace(
                    session,
                    session.Document.Pages[0],
                    context.Source,
                    descriptor,
                    "\u03A9"));

        Assert.Equal(
            "BT\n/F1 12 Tf\n(Hello) Tj\nET\n",
            ReadContent(
                context));
    }

    [Fact]
    public void FilteredContentStreamIsRejectedWithoutChangingBytes()
    {
        TestContext context =
            CreateContext(
                PdfVersion.Pdf17,
                "Hello",
                filtered: true);

        using PdfEditingSession session =
            new(
                context.Document);

        PdfExistingTextDescriptor descriptor =
            Discover(
                session,
                context);

        Assert.Throws<NotSupportedException>(
            () =>
                PdfExistingTextReplacementService.Replace(
                    session,
                    session.Document.Pages[0],
                    context.Source,
                    descriptor,
                    "World"));

        Assert.Equal(
            "BT\n/F1 12 Tf\n(Hello) Tj\nET\n",
            ReadContent(
                context));
    }

    [Theory]
    [InlineData(1, 7, "%PDF-1.7")]
    [InlineData(2, 0, "%PDF-2.0")]
    public void FullRewritePreservesDocumentVersionAndReplacement(
        int major,
        int minor,
        string expectedHeader)
    {
        TestContext context =
            CreateContext(
                new PdfVersion(
                    major,
                    minor),
                "Hello");

        using PdfEditingSession session =
            new(
                context.Document);

        PdfEditChangeSet changeSet =
            ReplaceAndCommit(
                session,
                context,
                "World");

        byte[] output =
            PdfEditingSaveService.Write(
                session,
                changeSet);

        string serialized =
            Encoding.Latin1.GetString(
                output);

        Assert.StartsWith(
            expectedHeader,
            serialized);
        Assert.Contains(
            "(World) Tj",
            serialized);
    }

    [Fact]
    public void IncrementalSaveAppendsOnlyChangedContentStream()
    {
        TestContext context =
            CreateContext(
                PdfVersion.Pdf20,
                "Hello");

        using PdfEditingSession session =
            new(
                context.Document);

        PdfEditChangeSet changeSet =
            ReplaceAndCommit(
                session,
                context,
                "World");

        byte[] sourceBytes =
            Encoding.ASCII.GetBytes(
                "%PDF-2.0\n% source revision\nxref\n%%EOF\n");

        var sourceRevision =
            new PdfMemorySourceRevision(
                sourceBytes,
                24,
                PdfCrossReferenceWriteMode.TraditionalTable);

        byte[] output =
            PdfEditingSaveService.Write(
                session,
                changeSet,
                new PdfWriteOptions
                {
                    Mode =
                        PdfWriteMode.IncrementalUpdate,
                    CrossReferenceMode =
                        PdfCrossReferenceWriteMode.TraditionalTable
                },
                sourceRevision);

        Assert.True(
            output.AsSpan(
                    0,
                    sourceBytes.Length)
                .SequenceEqual(
                    sourceBytes));

        string appended =
            Encoding.Latin1.GetString(
                output.AsSpan(
                    sourceBytes.Length));

        Assert.Contains(
            "(World) Tj",
            appended);
        Assert.Contains(
            $"{context.ContentStreamIdentifier.ObjectNumber} 0 obj",
            appended);
        Assert.DoesNotContain(
            "3 0 obj",
            appended);
    }

    [Fact]
    public void StaleSourceStringIsRejectedInsteadOfFuzzyMatched()
    {
        TestContext context =
            CreateContext(
                PdfVersion.Pdf17,
                "Hello");

        using PdfEditingSession session =
            new(
                context.Document);

        PdfExistingTextDescriptor descriptor =
            Discover(
                session,
                context);

        context.Document.Objects
            .Get<Document.Objects.PdfStream>(
                context.ContentStreamIdentifier)
            .ReplaceData(
                Encoding.ASCII.GetBytes(
                    "BT\n/F1 12 Tf\n(Other) Tj\nET\n"));

        Assert.Throws<InvalidOperationException>(
            () =>
                PdfExistingTextReplacementService.Replace(
                    session,
                    session.Document.Pages[0],
                    context.Source,
                    descriptor,
                    "World"));
    }

    private static PdfEditChangeSet ReplaceAndCommit(
        PdfEditingSession session,
        TestContext context,
        string replacement)
    {
        PdfExistingTextDescriptor descriptor =
            Discover(
                session,
                context);

        using var transaction =
            session.BeginTransaction();

        _ = PdfExistingTextReplacementService.Replace(
            session,
            session.Document.Pages[0],
            context.Source,
            descriptor,
            replacement);

        return transaction.Commit();
    }

    private static PdfExistingTextDescriptor Discover(
        PdfEditingSession session,
        TestContext context) =>
        Assert.Single(
            PdfExistingTextDiscoveryService.Discover(
                session,
                session.Document.Pages[0],
                context.Source));

    private static string ReadContent(
        TestContext context) =>
        Encoding.ASCII.GetString(
            context.Document.Objects
                .Get<Document.Objects.PdfStream>(
                    context.ContentStreamIdentifier)
                .ToByteArray());

    private static TestContext CreateContext(
        PdfVersion version,
        string text,
        bool filtered = false)
    {
        PdfDocument document =
            PdfDocument.CreateEmpty(
                version);

        var catalogId =
            new PdfObjectIdentifier(1);
        var rootId =
            new PdfObjectIdentifier(2);
        var pageId =
            new PdfObjectIdentifier(3);
        var contentId =
            new PdfObjectIdentifier(4);
        var fontId =
            new PdfObjectIdentifier(5);

        document.BindCoreStructure(
            catalogId,
            rootId);
        document.Objects.Add(
            catalogId,
            new Document.Objects.PdfDictionary());
        document.Objects.Add(
            rootId,
            CreatePagesDictionary(
                pageId));

        var page =
            new PdfPage(
                612,
                792);

        page.BindObjectIdentifier(
            pageId);
        document.Pages.Add(
            page);

        var pageDictionary =
            new Document.Objects.PdfDictionary
            {
                {
                    TypeName,
                    PageName
                },
                {
                    ParentName,
                    new DocumentPdfIndirectReference(
                        rootId)
                },
                {
                    ContentsName,
                    new DocumentPdfIndirectReference(
                        contentId)
                }
            };

        document.Objects.Add(
            pageId,
            pageDictionary);

        var streamDictionary =
            new Document.Objects.PdfDictionary();

        if (filtered)
        {
            streamDictionary.Set(
                DocumentPdfName.FromLatin1("Filter"),
                DocumentPdfName.FromLatin1("FlateDecode"));
        }

        string content =
            $"BT\n/F1 12 Tf\n({text}) Tj\nET\n";

        document.Objects.Add(
            contentId,
            new Document.Objects.PdfStream(
                streamDictionary,
                Encoding.ASCII.GetBytes(
                    content)));

        document.Objects.Add(
            fontId,
            new Document.Objects.PdfDictionary());

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
            CreateTextOperation(
                text,
                start,
                end);

        PdfResolvedFont resolvedFont =
            CreateFont(
                fontId);

        PdfDisplayTextRun run =
            new(
                resolvedFont,
                12,
                [
                    PdfTextRunElement.CreateText(
                        Encoding.ASCII.GetBytes(
                            text),
                        text)
                ],
                PdfMatrix.Identity,
                new PdfGraphicsStateSnapshot
                {
                    Font =
                        resolvedFont,
                    FontSize =
                        12
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

        return new TestContext(
            document,
            source,
            contentId);
    }

    private static Document.Objects.PdfDictionary
        CreatePagesDictionary(
        PdfObjectIdentifier pageId) =>
        new()
        {
            {
                TypeName,
                PagesName
            },
            {
                KidsName,
                new DocumentPdfArray(
                [
                    new DocumentPdfIndirectReference(
                        pageId)
                ])
            },
            {
                CountName,
                new DocumentPdfInteger(1)
            }
        };

    private static PdfGraphicsOperation CreateTextOperation(
        string text,
        int start,
        int end)
    {
        Assert.True(
            PdfGraphicsOperator.TryGet(
                "Tj",
                out PdfGraphicsOperator @operator));

        return new PdfGraphicsOperation(
            @operator,
            [
                new PdfLiteralString(
                    text)
            ],
            new PdfContentSourceSpan(
                0,
                start,
                end));
    }

    private static PdfResolvedFont CreateFont(
        PdfObjectIdentifier fontId) =>
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

    private sealed record TestContext(
        PdfDocument Document,
        PdfExistingTextDiscoverySource Source,
        PdfObjectIdentifier ContentStreamIdentifier);
}
