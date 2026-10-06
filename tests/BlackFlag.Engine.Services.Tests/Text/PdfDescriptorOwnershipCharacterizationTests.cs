using System.Runtime.InteropServices;
using System.Text;
using BlackFlag.Engine.Document.Objects;
using BlackFlag.Engine.Document.Objects.Primitives;
using BlackFlag.Engine.Editing.Text;
using BlackFlag.Engine.Rendering.DisplayList;
using BlackFlag.Engine.Rendering.Resources;
using BlackFlag.Engine.Services.Text;

namespace BlackFlag.Engine.Services.Tests.Text;

// Ownership regressions converted from the POST-P0-012 characterization cases.
public sealed class PdfDescriptorOwnershipCharacterizationTests
{
    [Fact]
    public void ConstructorInputAndPublicSnapshotsCannotMutateDescriptor()
    {
        using var context = new PdfFilteredTextTestDocument();
        byte[] input = "Hello"u8.ToArray();
        var original = context.Descriptor;
        var descriptor = new PdfExistingTextDescriptor(original.Identity, input, original.UnicodeText,
            original.FontResourceName, original.FontObjectIdentifier, original.FontSize, original.Editability,
            typography: original.Typography);
        input[0] = (byte)'X';
        Assert.Equal("Hello"u8.ToArray(), descriptor.EncodedBytes.ToArray());
        ReadOnlyMemory<byte> retained = descriptor.EncodedBytes;
        ReadOnlyMemory<byte> independent = descriptor.EncodedBytes;
        if (MemoryMarshal.TryGetArray(retained, out ArraySegment<byte> alias))
        {
            alias.Array![alias.Offset] = (byte)'J';
            Assert.Equal("Jello"u8.ToArray(), retained.ToArray());
        }
        Assert.Equal("Hello"u8.ToArray(), descriptor.EncodedBytes.ToArray());
        MemoryMarshal.AsMemory(retained).Span.Fill(0xFF);
        Assert.Equal("Hello"u8.ToArray(), independent.ToArray());
        Assert.Equal("Hello"u8.ToArray(), descriptor.EncodedBytes.ToArray());
        Assert.Equal("Hello", descriptor.UnicodeText);
        Assert.Equal(original.Identity, descriptor.Identity);
        Assert.Equal(original.FontResourceName, descriptor.FontResourceName);
        Assert.Equal(original.FontObjectIdentifier, descriptor.FontObjectIdentifier);
        Assert.Equal(original.FontSize, descriptor.FontSize);
        Assert.Equal(original.Typography, descriptor.Typography);
        Assert.Equal(original.Editability, descriptor.Editability);
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
    [InlineData("typography")]
    [InlineData("width")]
    public void RetainedSnapshotMutationPreservesValidOperationsAndHistory(string operation)
    {
        using var context = new PdfFilteredTextTestDocument();
        var source = WithAlternateFont(context);
        ReadOnlyMemory<byte> retained = context.Descriptor.EncodedBytes;
        string expected;
        using (var transaction = context.Session.BeginTransaction())
        {
            Apply(context, source, operation);
            expected = context.ReadContent();
            transaction.Rollback();
        }
        Assert.Equal(context.OriginalContent, context.ReadContent());
        "World"u8.CopyTo(MemoryMarshal.AsMemory(retained).Span);
        Assert.Equal("Hello"u8.ToArray(), context.Descriptor.EncodedBytes.ToArray());
        using (var transaction = context.Session.BeginTransaction())
        {
            Apply(context, source, operation);
            Assert.Equal(expected, context.ReadContent());
            transaction.Rollback();
        }
        Assert.Equal(context.OriginalContent, context.ReadContent());
        using (var transaction = context.Session.BeginTransaction())
        {
            Apply(context, source, operation);
            _ = transaction.Commit();
        }
        Assert.NotEqual(context.OriginalContent, expected);
        Assert.Equal(expected, context.ReadContent());
        Assert.Contains(operation is "replace" or "width" ? "Other" : "Hello", expected, StringComparison.Ordinal);
        MemoryMarshal.AsMemory(retained).Span.Fill(0xFF);
        _ = context.Session.History.Undo();
        Assert.Equal(context.OriginalContent, context.ReadContent());
        _ = context.Session.History.Redo();
        Assert.Equal(expected, context.ReadContent());
        Assert.Equal("Hello"u8.ToArray(), context.Descriptor.EncodedBytes.ToArray());
    }

    [Theory]
    [InlineData("replace")]
    [InlineData("font")]
    [InlineData("style")]
    [InlineData("typography")]
    [InlineData("width")]
    public void ForgedRetainedSnapshotCannotMakeAStaleTargetCurrent(string operation)
    {
        using var context = new PdfFilteredTextTestDocument();
        var source = WithAlternateFont(context);
        ReadOnlyMemory<byte> retained = context.Descriptor.EncodedBytes;
        string current = context.OriginalContent.Replace("Hello", "World", StringComparison.Ordinal);
        context.Content.ReplaceData(PdfFilteredTextTestDocument.Compress(Encoding.ASCII.GetBytes(current)));
        var before = Assert.Throws<InvalidOperationException>(() => Apply(context, source, operation));
        Assert.Equal(current, context.ReadContent());
        "World"u8.CopyTo(MemoryMarshal.AsMemory(retained).Span);
        Assert.Equal("Hello"u8.ToArray(), context.Descriptor.EncodedBytes.ToArray());
        using (var transaction = context.Session.BeginTransaction())
        {
            var after = Assert.Throws<InvalidOperationException>(() => Apply(context, source, operation));
            Assert.Equal(before.Message, after.Message);
            Assert.Equal(current, context.ReadContent());
            transaction.Rollback();
        }
        Assert.Equal(current, context.ReadContent());
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

    internal static void Apply(PdfFilteredTextTestDocument context, PdfExistingTextDiscoverySource source, string operation)
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
            case "typography":
                _ = PdfExistingTextTypographyService.Apply(context.Session, context.Session.Document.Pages[0],
                    source, context.Descriptor, new PdfExistingTextTypographyUpdate { FontSize = 18 });
                break;
            case "width":
                _ = PdfWidthAwareTextReplacementService.Replace(context.Session, context.Session.Document.Pages[0],
                    source, context.Descriptor, "Other", preserveAdvance: false);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(operation));
        }
    }
}
