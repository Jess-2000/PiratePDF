using System.Runtime.InteropServices;
using System.Text;
using BlackFlag.Engine.Document.Objects;
using BlackFlag.Engine.Document.Objects.Primitives;
using BlackFlag.Engine.Editing.Text;
using BlackFlag.Engine.Rendering.DisplayList;
using BlackFlag.Engine.Rendering.Resources;
using BlackFlag.Engine.Services.Text;

namespace BlackFlag.Engine.Services.Tests.Text;

// Characterizes current alias/stale-source behavior. A future approved fix must
// replace the defect expectations with immutable-provenance regression assertions.
public sealed class PdfDescriptorOwnershipCharacterizationTests
{
    [Fact]
    public void ConstructorInputIsCopiedButPublicSnapshotMutatesDescriptorOnly()
    {
        using var context = new PdfFilteredTextTestDocument();
        byte[] input = "Hello"u8.ToArray();
        var original = context.Descriptor;
        var descriptor = new PdfExistingTextDescriptor(original.Identity, input, original.UnicodeText,
            original.FontResourceName, original.FontObjectIdentifier, original.FontSize, original.Editability);
        input[0] = (byte)'X';
        Assert.Equal("Hello"u8.ToArray(), descriptor.EncodedBytes.ToArray());
        Assert.True(MemoryMarshal.TryGetArray(descriptor.EncodedBytes, out ArraySegment<byte> alias));
        alias.Array![alias.Offset] = (byte)'J';
        Assert.Equal("Jello"u8.ToArray(), descriptor.EncodedBytes.ToArray());
        Assert.Equal("Hello", descriptor.UnicodeText);
        Assert.Equal(original.Identity, descriptor.Identity);
        Assert.Equal("Hello"u8.ToArray(), original.EncodedBytes.ToArray());
        var run = Assert.IsType<PdfDisplayTextRun>(Assert.Single(context.Source.DisplayList));
        MemoryMarshal.AsMemory(run.EncodedBytes).Span.Fill(0xFF);
        Assert.Equal("Hello"u8.ToArray(), run.EncodedBytes.ToArray());
        Assert.Equal(context.OriginalContent, context.ReadContent());
    }

    [Theory]
    [InlineData("replace")]
    [InlineData("font")]
    [InlineData("style")]
    public void MutatedDescriptorMakesAnOtherwiseValidTargetFail(string operation)
    {
        using var context = new PdfFilteredTextTestDocument();
        var source = WithAlternateFont(context);
        using (var transaction = context.Session.BeginTransaction())
        {
            Apply(context, source, operation);
            transaction.Rollback();
        }
        Assert.Equal(context.OriginalContent, context.ReadContent());
        "World"u8.CopyTo(MemoryMarshal.AsMemory(context.Descriptor.EncodedBytes).Span);
        Assert.Throws<InvalidOperationException>(() => Apply(context, source, operation));
        Assert.Equal(context.OriginalContent, context.ReadContent());
    }

    [Theory]
    [InlineData("replace")]
    [InlineData("font")]
    [InlineData("style")]
    public void ForgedCurrentBytesHavePathSpecificStaleAndHistoryEffects(string operation)
    {
        using var context = new PdfFilteredTextTestDocument();
        var source = WithAlternateFont(context);
        string current = context.OriginalContent.Replace("Hello", "World", StringComparison.Ordinal);
        context.Content.ReplaceData(PdfFilteredTextTestDocument.Compress(Encoding.ASCII.GetBytes(current)));
        Assert.Throws<InvalidOperationException>(() => Apply(context, source, operation));
        Assert.Equal(current, context.ReadContent());
        "World"u8.CopyTo(MemoryMarshal.AsMemory(context.Descriptor.EncodedBytes).Span);
        if (operation == "replace")
        {
            // Direct replacement additionally checks the independent display run.
            Assert.Throws<InvalidOperationException>(() => Apply(context, source, operation));
            Assert.Equal(current, context.ReadContent());
            return;
        }

        using (var transaction = context.Session.BeginTransaction())
        {
            Apply(context, source, operation);
            transaction.Rollback();
        }
        Assert.Equal(current, context.ReadContent());
        using (var transaction = context.Session.BeginTransaction())
        {
            Apply(context, source, operation);
            _ = transaction.Commit();
        }
        string changed = context.ReadContent();
        Assert.NotEqual(current, changed);
        Assert.Contains(operation == "font" ? "(Hello) Tj" : "(World) Tj", changed, StringComparison.Ordinal);
        MemoryMarshal.AsMemory(context.Descriptor.EncodedBytes).Span.Fill(0xFF);
        _ = context.Session.History.Undo();
        Assert.Equal(current, context.ReadContent());
        _ = context.Session.History.Redo();
        Assert.Equal(changed, context.ReadContent());
    }

    private static PdfExistingTextDiscoverySource WithAlternateFont(PdfFilteredTextTestDocument context)
    {
        var run = Assert.IsType<PdfDisplayTextRun>(Assert.Single(context.Source.DisplayList));
        var page = context.Document.Objects.Get<PdfDictionary>(PdfFilteredTextTestDocument.PageId);
        page.Get<PdfDictionary>(PdfName.FromLatin1("Resources")).Get<PdfDictionary>(PdfName.FromLatin1("Font"))
            .Set(PdfName.FromLatin1("F2"), new PdfIndirectReference(new PdfObjectIdentifier(5)));
        return new PdfExistingTextDiscoverySource(context.Source.ContentOwnerIdentifier, context.Source.SourceKind,
            context.Source.Operations, context.Source.DisplayList, [PdfFilteredTextTestDocument.ContentId],
            availableFonts: new Dictionary<string, PdfResolvedFont> { ["F1"] = run.Font, ["F2"] = run.Font });
    }

    private static void Apply(PdfFilteredTextTestDocument context, PdfExistingTextDiscoverySource source, string operation)
    {
        switch (operation)
        {
            case "replace":
                _ = PdfExistingTextReplacementService.Replace(context.Session, context.Session.Document.Pages[0],
                    source, context.Descriptor, "Other");
                break;
            case "font":
                _ = PdfExistingTextFontService.ChangeFont(context.Session, context.Session.Document.Pages[0],
                    source, context.Descriptor, "F2");
                break;
            case "style":
                _ = PdfExistingTextStyleService.Apply(context.Session, context.Session.Document.Pages[0],
                    source, context.Descriptor, new PdfExistingTextStyleUpdate { FillColor = PdfExistingTextColor.Rgb(1, 0, 0) });
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(operation));
        }
    }
}
