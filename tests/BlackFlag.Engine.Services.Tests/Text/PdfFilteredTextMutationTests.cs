using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using BlackFlag.Engine.Document.Objects;
using BlackFlag.Engine.Editing.Operations;
using BlackFlag.Engine.Editing.Text;
using BlackFlag.Engine.Filters;
using BlackFlag.Engine.Services.Editing;
using BlackFlag.Engine.Services.Text;
using BlackFlag.Engine.Writing;

namespace BlackFlag.Engine.Services.Tests.Text;

public sealed class PdfFilteredTextMutationTests
{
    [Theory]
    [InlineData("direct", "(World) Tj")]
    [InlineData("positioned", "[(Hi) -120 <616C6C>] TJ")]
    [InlineData("style", "1 0 0 rg\n(Hello) Tj")]
    [InlineData("typography", "/F1 18 Tf\n(Hello) Tj")]
    public void FilteredCallersPreserveDictionaryAndUndoRedo(string workflow, string expected)
    {
        using var context = new PdfFilteredTextTestDocument(workflow == "positioned");
        byte[] original = context.Content.ToByteArray();
        byte[] dictionary = PdfFilteredTextTestDocument.Serialize(context.Content.Dictionary);

        using (var transaction = context.Session.BeginTransaction())
        {
            _ = Apply(context, workflow);
            _ = transaction.Commit();
        }

        byte[] changed = context.Content.ToByteArray();
        Assert.Contains(expected, context.ReadContent(), StringComparison.Ordinal);
        Assert.EndsWith("\n(Next) Tj\nET\n", context.ReadContent(), StringComparison.Ordinal);
        Assert.Equal(dictionary, PdfFilteredTextTestDocument.Serialize(context.Content.Dictionary));
        _ = context.Session.History.Undo();
        Assert.Equal(original, context.Content.ToByteArray());
        _ = context.Session.History.Redo();
        Assert.Equal(changed, context.Content.ToByteArray());
        Assert.Equal(dictionary, PdfFilteredTextTestDocument.Serialize(context.Content.Dictionary));
    }

    [Theory]
    [InlineData("direct")]
    [InlineData("positioned")]
    [InlineData("style")]
    [InlineData("typography")]
    public void BudgetFailureLeavesContentDictionaryAndHistoryUnchanged(string workflow)
    {
        using var context = new PdfFilteredTextTestDocument(workflow == "positioned");
        byte[] bomb = PdfFilteredTextTestDocument.Compress(new byte[1024 * 1024]);
        context.Content.ReplaceData(bomb);
        byte[] dictionary = PdfFilteredTextTestDocument.Serialize(context.Content.Dictionary);

        PdfFilterDecodeException exception = Assert.Throws<PdfFilterDecodeException>(
            () => Apply(context, workflow));

        Assert.Equal(PdfFilterDecodeFailure.ExpansionRatioExceeded, exception.Failure);
        Assert.Equal(bomb, context.Content.ToByteArray());
        Assert.Equal(dictionary, PdfFilteredTextTestDocument.Serialize(context.Content.Dictionary));
        Assert.False(context.Session.History.CanUndo);
    }

    [Theory]
    [InlineData("direct")]
    [InlineData("positioned")]
    [InlineData("style")]
    public void CancelledCallerDoesNotMutateOrRecordHistory(string workflow)
    {
        using var context = new PdfFilteredTextTestDocument(workflow == "positioned");
        byte[] original = context.Content.ToByteArray();
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        OperationCanceledException exception = Assert.Throws<OperationCanceledException>(
            () => Apply(context, workflow, cancellation.Token));

        Assert.Equal(cancellation.Token, exception.CancellationToken);
        Assert.Equal(original, context.Content.ToByteArray());
        Assert.False(context.Session.History.CanUndo);
    }

    [Fact]
    public void LaterBudgetFailureRollsBackAnEarlierFilteredReplacement()
    {
        using var context = new PdfFilteredTextTestDocument();
        byte[] original = context.Content.ToByteArray();
        var secondId = new PdfObjectIdentifier(6);
        var second = PdfFilteredTextTestDocument.CreateStream(new byte[1024 * 1024]);
        context.Document.Objects.Add(secondId, second);
        byte[] secondOriginal = second.ToByteArray();
        var secondSource = new PdfExistingTextDiscoverySource(
            PdfFilteredTextTestDocument.PageId, PdfExistingTextSourceKind.PageContent,
            context.Source.Operations, context.Source.DisplayList, [secondId]);
        PdfExistingTextDescriptor secondDescriptor = context.Discover(secondSource);

        Assert.Throws<PdfFilterDecodeException>(() =>
        {
            using var transaction = context.Session.BeginTransaction();
            _ = Apply(context, "direct");
            Assert.NotEqual(original, context.Content.ToByteArray());
            _ = PdfExistingTextReplacementService.Replace(
                context.Session, context.Session.Document.Pages[0],
                secondSource, secondDescriptor, "World");
            _ = transaction.Commit();
        });

        Assert.Equal(original, context.Content.ToByteArray());
        Assert.Equal(secondOriginal, second.ToByteArray());
        Assert.False(context.Session.History.CanUndo);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void FilteredMutationSavesWithOriginalFilterAndUnknownEntries(
        bool incremental, bool crossReferenceStream)
    {
        byte[] saved = CreateSavedDocument(incremental, crossReferenceStream);
        string serialized = Encoding.Latin1.GetString(saved);

        Assert.StartsWith("%PDF-2.0", serialized, StringComparison.Ordinal);
        Assert.Contains("/PrivatePageEntry 17", serialized, StringComparison.Ordinal);
        Assert.Contains("/PrivateStreamEntry 23", serialized, StringComparison.Ordinal);
        Assert.Contains("/PrivateParameter 42", serialized, StringComparison.Ordinal);
        Assert.Contains("/Predictor 1", serialized, StringComparison.Ordinal);
        Assert.Contains("/Fl", serialized, StringComparison.Ordinal);
    }

    // Reused by the separate parser reopen verification without changing
    // project references between the isolated Services and Parsing test suites.
    public static byte[] CreateSavedDocument(bool incremental, bool crossReferenceStream)
    {
        using var context = new PdfFilteredTextTestDocument();
        PdfCrossReferenceWriteMode mode = crossReferenceStream
            ? PdfCrossReferenceWriteMode.CrossReferenceStream
            : PdfCrossReferenceWriteMode.TraditionalTable;
        using var originalOutput = new MemoryStream();
        PdfFullRewriteWriter.Write(originalOutput, PdfWritePlanner.Create(
            context.Document, new PdfWriteOptions { CrossReferenceMode = mode }));
        byte[] original = originalOutput.ToArray();
        string sourceText = Encoding.Latin1.GetString(original);
        int startXref = sourceText.LastIndexOf("startxref", StringComparison.Ordinal) + "startxref".Length;
        long xrefOffset = long.Parse(
            sourceText[startXref..].Split((char[])null, StringSplitOptions.RemoveEmptyEntries)[0],
            CultureInfo.InvariantCulture);
        var sourceRevision = new PdfMemorySourceRevision(original, xrefOffset, mode);

        using var transaction = context.Session.BeginTransaction();
        _ = Apply(context, "direct");
        var changeSet = transaction.Commit();
        Assert.Equal(context.OriginalContent.Replace("Hello", "World", StringComparison.Ordinal),
            context.ReadContent());

        byte[] output = PdfEditingSaveService.Write(
            context.Session, changeSet,
            new PdfWriteOptions
            {
                Mode = incremental ? PdfWriteMode.IncrementalUpdate : PdfWriteMode.FullRewrite,
                CrossReferenceMode = mode
            },
            incremental ? sourceRevision : null);

        if (incremental)
        {
            Assert.True(output.AsSpan(0, original.Length).SequenceEqual(original));
        }

        return output;
    }

    private static PdfExistingTextEditOperation Apply(
        PdfFilteredTextTestDocument context,
        string workflow,
        CancellationToken cancellationToken = default) =>
        workflow switch
        {
            "direct" => PdfExistingTextReplacementService.Replace(
                context.Session, context.Session.Document.Pages[0], context.Source,
                context.Descriptor, "World", cancellationToken),
            "positioned" => PdfPositionedTextReplacementService.Replace(
                context.Session, context.Session.Document.Pages[0], context.Source,
                context.Descriptor, ["Hi", "all"], cancellationToken),
            "style" => PdfExistingTextStyleService.Apply(
                context.Session, context.Session.Document.Pages[0], context.Source,
                context.Descriptor, new PdfExistingTextStyleUpdate
                {
                    FillColor = PdfExistingTextColor.Rgb(1, 0, 0)
                }, cancellationToken),
            "typography" => PdfExistingTextTypographyService.Apply(
                context.Session, context.Session.Document.Pages[0], context.Source,
                context.Descriptor, new PdfExistingTextTypographyUpdate
                {
                    FontSize = 18
                }),
            _ => throw new ArgumentOutOfRangeException(nameof(workflow))
        };
}
