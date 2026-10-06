using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using BlackFlag.Engine.Document.Objects;
using BlackFlag.Engine.Fonts.Sanitization;
using BlackFlag.Engine.Rendering.Fonts;
using BlackFlag.Engine.Services.Editing;
using BlackFlag.Engine.Services.Tests.Fonts;
using BlackFlag.Engine.Services.Text;
using BlackFlag.Engine.Writing;

namespace BlackFlag.Engine.Services.Tests.Text;

public sealed class PdfDescriptorOwnershipIntegrationTests
{
    [Fact]
    public void ExternalFontMutationAttemptsPreserveResourcesHistoryAndSavedText()
    {
        var saved = CreateExternalSavedDocument();
        Assert.NotEmpty(saved.Pdf);
        Assert.Contains("/FExt2 12 Tf", saved.Content, StringComparison.Ordinal);
    }

    // Supplemental parser validation reuses the same deterministic fixture.
    public static (byte[] Pdf, string Content) CreateExternalSavedDocument()
    {
        using var context = new PdfFontCollisionTestDocument("direct");
        ReadOnlyMemory<byte> retained = context.Text.Descriptor.EncodedBytes;
        MemoryMarshal.AsMemory(retained).Span[0] = (byte)'B';
        Assert.Equal("A"u8.ToArray(), context.Text.Descriptor.EncodedBytes.ToArray());
        using (var transaction = context.Text.Session.BeginTransaction())
        {
            ChangeFont(context);
            transaction.Rollback();
        }
        context.AssertOriginalGraph();
        using (var transaction = context.Text.Session.BeginTransaction())
        {
            ChangeFont(context);
            _ = transaction.Commit();
        }
        string content = context.Text.ReadContent();
        Assert.Contains("/FExt2 12 Tf", content, StringComparison.Ordinal);
        var cmapStream = context.Document.Objects.Get<PdfStream>(new PdfObjectIdentifier(15));
        var cmap = PdfCMapParser.Parse(cmapStream.ToByteArray());
        Assert.True(cmap.TryGetUnicode(new PdfCharacterCode([0, 1]), out string text));
        Assert.Equal("A", text);
        var changed = context.Snapshot();
        MemoryMarshal.AsMemory(retained).Span.Fill(0xFF);
        _ = context.Text.Session.History.Undo();
        context.AssertOriginalGraph();
        _ = context.Text.Session.History.Redo();
        Assert.Equal(content, context.Text.ReadContent());
        Assert.Equal(changed.Keys, context.Document.Objects.Identifiers);
        foreach (var entry in changed)
        {
            Assert.Equal(entry.Value, PdfFilteredTextTestDocument.Serialize(context.Document.Objects.Get(entry.Key)));
        }
        context.AssertUnrelatedObjectsUnchanged();
        Assert.Equal("A"u8.ToArray(), context.Text.Descriptor.EncodedBytes.ToArray());
        return (PdfFullRewriteWriter.Write(PdfWritePlanner.Create(context.Document)), content);
    }

    [Fact]
    public void ExternalFontStaleRejectionCannotBeBypassedByRetainedSnapshot()
    {
        using var context = new PdfFontCollisionTestDocument("direct");
        ReadOnlyMemory<byte> retained = context.Text.Descriptor.EncodedBytes;
        string current = context.Text.OriginalContent.Replace("(A)", "(B)", StringComparison.Ordinal);
        context.Text.Content.ReplaceData(PdfFilteredTextTestDocument.Compress(Encoding.ASCII.GetBytes(current)));
        var before = context.Snapshot();
        var initialFailure = Assert.Throws<InvalidOperationException>(() => ChangeFont(context));
        MemoryMarshal.AsMemory(retained).Span[0] = (byte)'B';
        var failure = Assert.Throws<InvalidOperationException>(() => ChangeFont(context));
        Assert.Equal(initialFailure.Message, failure.Message);
        Assert.Equal("A"u8.ToArray(), context.Text.Descriptor.EncodedBytes.ToArray());
        Assert.Equal(current, context.Text.ReadContent());
        Assert.Equal(before.Keys, context.Document.Objects.Identifiers);
        foreach (var entry in before)
        {
            Assert.Equal(entry.Value, PdfFilteredTextTestDocument.Serialize(context.Document.Objects.Get(entry.Key)));
        }
        Assert.False(context.Text.Session.History.CanUndo);
    }

    [Fact]
    public void ExternalFontCancellationAfterSnapshotMutationLeavesGraphUnchanged()
    {
        using var context = new PdfFontCollisionTestDocument("direct");
        MemoryMarshal.AsMemory(context.Text.Descriptor.EncodedBytes).Span.Fill(0xFF);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var failure = Assert.Throws<OperationCanceledException>(() => ChangeFont(context, cancellation.Token));
        Assert.Equal(cancellation.Token, failure.CancellationToken);
        context.AssertOriginalGraph();
        Assert.False(context.Text.Session.History.CanUndo);
    }

    [Theory]
    [InlineData("replace", false, false)]
    [InlineData("replace", true, true)]
    [InlineData("style", false, false)]
    [InlineData("style", true, true)]
    [InlineData("typography", false, false)]
    [InlineData("typography", true, true)]
    public void SavedFilteredMutationPreservesDescriptorAndStreamRepresentation(string operation, bool incremental, bool streamXref)
    {
        var saved = CreateSavedDocument(operation, incremental, streamXref);
        string serialized = Encoding.Latin1.GetString(saved.Pdf);
        Assert.Contains("/PrivateParameter 42", serialized, StringComparison.Ordinal);
        Assert.Contains("/Predictor 1", serialized, StringComparison.Ordinal);
        Assert.Contains("/Fl", serialized, StringComparison.Ordinal);
    }

    // Exposes the saved fixture to parser-based checks without new project references.
    public static (byte[] Pdf, string Content) CreateSavedDocument(string operation, bool incremental, bool streamXref)
    {
        using var context = new PdfFilteredTextTestDocument();
        var mode = streamXref ? PdfCrossReferenceWriteMode.CrossReferenceStream : PdfCrossReferenceWriteMode.TraditionalTable;
        byte[] original = PdfFullRewriteWriter.Write(PdfWritePlanner.Create(context.Document,
            new PdfWriteOptions { CrossReferenceMode = mode }));
        string originalText = Encoding.Latin1.GetString(original);
        int startXref = originalText.LastIndexOf("startxref", StringComparison.Ordinal) + "startxref".Length;
        long offset = long.Parse(originalText[startXref..].Split((char[])null,
            StringSplitOptions.RemoveEmptyEntries)[0], CultureInfo.InvariantCulture);
        var revision = new PdfMemorySourceRevision(original, offset, mode);
        ReadOnlyMemory<byte> retained = context.Descriptor.EncodedBytes;
        MemoryMarshal.AsMemory(retained).Span.Fill(0xFF);
        using var transaction = context.Session.BeginTransaction();
        PdfDescriptorOwnershipCharacterizationTests.Apply(context, context.Source, operation);
        var changeSet = transaction.Commit();
        string content = context.ReadContent();
        Assert.Contains(operation == "replace" ? "(Other) Tj" : "(Hello) Tj", content, StringComparison.Ordinal);
        MemoryMarshal.AsMemory(retained).Span.Clear();
        byte[] saved = PdfEditingSaveService.Write(context.Session, changeSet,
            new PdfWriteOptions
            {
                Mode = incremental ? PdfWriteMode.IncrementalUpdate : PdfWriteMode.FullRewrite,
                CrossReferenceMode = mode
            }, incremental ? revision : null);
        if (incremental)
        {
            Assert.Equal(original, saved[..original.Length]);
        }
        Assert.Equal("Hello"u8.ToArray(), context.Descriptor.EncodedBytes.ToArray());
        return (saved, content);
    }

    private static void ChangeFont(PdfFontCollisionTestDocument context, CancellationToken cancellationToken = default) =>
        _ = PdfExternalExistingTextFontService.ChangeFont(context.Text.Session, context.Text.Session.Document.Pages[0],
            context.Text.Source, context.Text.Descriptor, context.Match, new PdfOpenTypeFontSanitizer(),
            "FExt1", cancellationToken);
}
