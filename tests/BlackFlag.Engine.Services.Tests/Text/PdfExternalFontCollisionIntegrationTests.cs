using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using BlackFlag.Engine.Document.Objects;
using BlackFlag.Engine.Document.Objects.Primitives;
using BlackFlag.Engine.Editing.Operations;
using BlackFlag.Engine.Fonts.Sanitization;
using BlackFlag.Engine.Services.Tests.Fonts;
using BlackFlag.Engine.Services.Text;
using BlackFlag.Engine.Writing;

namespace BlackFlag.Engine.Services.Tests.Text;

public sealed class PdfExternalFontCollisionIntegrationTests
{
    [Theory]
    [InlineData("direct")]
    [InlineData("indirect")]
    [InlineData("inherited")]
    [InlineData("indirect-category")]
    public void AllocatedTfAndResourcesUndoAndRedoTogether(string ownership)
    {
        using var context = new PdfFontCollisionTestDocument(ownership);
        using (var transaction = context.Text.Session.BeginTransaction())
        {
            var operation = ChangeFont(context);
            Assert.Equal("FExt2", operation.ResourceName);
            _ = transaction.Commit();
        }

        AssertFontSwitch(context);
        var changed = context.Snapshot();
        _ = context.Text.Session.History.Undo();
        context.AssertOriginalGraph();
        Assert.Same(context.OriginalObjects[PdfFilteredTextTestDocument.PageId],
            context.Document.Objects.Get(PdfFilteredTextTestDocument.PageId));
        _ = context.Text.Session.History.Redo();
        AssertFontSwitch(context);
        Assert.Equal(changed.Keys, context.Document.Objects.Identifiers);
        foreach (var pair in changed)
        {
            Assert.Equal(pair.Value, PdfFilteredTextTestDocument.Serialize(context.Document.Objects.Get(pair.Key)));
        }
    }

    [Theory]
    [InlineData("direct")]
    [InlineData("indirect")]
    [InlineData("inherited")]
    [InlineData("indirect-category")]
    public void LaterCancelledFontChangeRollsBackAllEarlierContentAndResources(string ownership)
    {
        using var context = new PdfFontCollisionTestDocument(ownership);
        using var cancelled = new CancellationTokenSource();
        cancelled.Cancel();

        OperationCanceledException exception = Assert.Throws<OperationCanceledException>(() =>
        {
            using var transaction = context.Text.Session.BeginTransaction();
            _ = ChangeFont(context);
            AssertFontSwitch(context);
            Assert.True(context.Document.Objects.Count > context.OriginalObjects.Count);
            _ = ChangeFont(context, cancelled.Token);
            _ = transaction.Commit();
        });

        Assert.Equal(cancelled.Token, exception.CancellationToken);
        context.AssertOriginalGraph();
        Assert.False(context.Text.Session.History.CanUndo);
    }

    public static TheoryData<string, bool, bool> SaveCases
    {
        get
        {
            var cases = new TheoryData<string, bool, bool>();
            foreach (string ownership in new[] { "direct", "indirect", "inherited", "indirect-category" })
            {
                foreach (bool incremental in new[] { false, true })
                {
                    cases.Add(ownership, incremental, false);
                    cases.Add(ownership, incremental, true);
                }
            }

            return cases;
        }
    }

    [Theory]
    [MemberData(nameof(SaveCases))]
    public void SaveRetainsAllocatedAndOriginalFontReferences(string ownership, bool incremental, bool streamXref)
    {
        byte[] saved = CreateSavedDocument(ownership, incremental, streamXref);
        string serialized = Encoding.Latin1.GetString(saved);

        Assert.Contains("/FExt1 9 7 R", serialized, StringComparison.Ordinal);
        Assert.Contains("/FExt2 18 0 R", serialized, StringComparison.Ordinal);
        Assert.Contains("/PrivateResourceEntry 71", serialized, StringComparison.Ordinal);
        Assert.Contains("/Subtype /Type0", serialized, StringComparison.Ordinal);
    }

    // Also used by parser-based reopen validation across the isolated test assemblies.
    public static byte[] CreateSavedDocument(string ownership, bool incremental, bool streamXref)
    {
        using var context = new PdfFontCollisionTestDocument(ownership);
        var xrefMode = streamXref ? PdfCrossReferenceWriteMode.CrossReferenceStream : PdfCrossReferenceWriteMode.TraditionalTable;
        using var originalOutput = new MemoryStream();
        PdfFullRewriteWriter.Write(originalOutput, PdfWritePlanner.Create(context.Document,
            new PdfWriteOptions { CrossReferenceMode = xrefMode }));
        byte[] original = originalOutput.ToArray();
        string originalText = Encoding.Latin1.GetString(original);
        int startXref = originalText.LastIndexOf("startxref", StringComparison.Ordinal) + "startxref".Length;
        long xrefOffset = long.Parse(originalText[startXref..].Split((char[])null,
            StringSplitOptions.RemoveEmptyEntries)[0], CultureInfo.InvariantCulture);
        var sourceRevision = new PdfMemorySourceRevision(original, xrefOffset, xrefMode);

        using var transaction = context.Text.Session.BeginTransaction();
        _ = ChangeFont(context);
        _ = transaction.Commit();
        AssertFontSwitch(context);
        var plan = PdfWritePlanner.Create(context.Document, new PdfWriteOptions
        {
            Mode = incremental ? PdfWriteMode.IncrementalUpdate : PdfWriteMode.FullRewrite,
            CrossReferenceMode = xrefMode
        });
        using var output = new MemoryStream();
        if (incremental)
        {
            // The public Editing save facade does not support resource-change
            // operations yet. Use Writing's existing explicit changed-object API.
            var changed = context.Snapshot().Where(pair =>
                !context.OriginalBytes.TryGetValue(pair.Key, out byte[] originalBytes) ||
                !originalBytes.AsSpan().SequenceEqual(pair.Value)).Select(pair => pair.Key).ToArray();
            Assert.Equal(8, changed.Length);
            PdfIncrementalUpdateWriter.Write(output, PdfIncrementalWritePlanner.Create(plan, sourceRevision, changed));
        }
        else
        {
            PdfFullRewriteWriter.Write(output, plan);
        }

        byte[] saved = output.ToArray();
        if (incremental)
        {
            Assert.True(saved.AsSpan(0, original.Length).SequenceEqual(original));
        }

        return saved;
    }

    private static PdfExistingTextResourceChangeOperation ChangeFont(
        PdfFontCollisionTestDocument context, CancellationToken cancellationToken = default) =>
        PdfExternalExistingTextFontService.ChangeFont(context.Text.Session, context.Text.Session.Document.Pages[0],
            context.Text.Source, context.Text.Descriptor, context.Match, new PdfOpenTypeFontSanitizer(),
            "FExt1", cancellationToken);

    private static void AssertFontSwitch(PdfFontCollisionTestDocument context)
    {
        string content = context.Text.ReadContent();
        Assert.Contains("/FExt2 12 Tf", content, StringComparison.Ordinal);
        Assert.DoesNotContain("/FExt1 12 Tf", content, StringComparison.Ordinal);
        Assert.EndsWith("/F1 12 Tf\n(Next) Tj\nET\n", content, StringComparison.Ordinal);
        PdfExternalFontResourceCollisionTests.AssertMapping(context, "FExt1", PdfFontCollisionTestDocument.OldFontId);
        PdfExternalFontResourceCollisionTests.AssertMapping(context, "FExt2", new PdfObjectIdentifier(18));
        Assert.Equal(PdfFontCollisionTestDocument.Name("Type0"), context.Document.Objects.Get<PdfDictionary>(
            new PdfObjectIdentifier(18)).Get(PdfFontCollisionTestDocument.Name("Subtype")));
        context.AssertUnrelatedObjectsUnchanged();
    }
}
