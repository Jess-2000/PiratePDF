using System.Threading;
using BlackFlag.Engine.Document.Objects;
using BlackFlag.Engine.Fonts;
using BlackFlag.Engine.Fonts.Embedding;
using BlackFlag.Engine.Fonts.Sanitization;
using BlackFlag.Engine.Services.Fonts;
using BlackFlag.Engine.Services.Tests.Text;
using BlackFlag.Engine.Writing;

namespace BlackFlag.Engine.Services.Tests.Fonts;

public sealed partial class PdfExternalFontEmbeddingRightsTests
{
    public static IEnumerable<object[]> ProjectionCases()
    {
        foreach (string operation in new[] { "full", "subset", "type0" })
        foreach (var row in new (ushort Flags, PdfExternalFontEmbeddingRights Provider, bool NoSubset, bool Bitmap, bool Full, bool Subset)[]
        {
            (0, PdfExternalFontEmbeddingRights.Allowed, false, false, true, true),
            (8, PdfExternalFontEmbeddingRights.Editable, false, false, true, true),
            (4, PdfExternalFontEmbeddingRights.Allowed, false, false, false, false),
            (2, PdfExternalFontEmbeddingRights.Allowed, false, false, false, false),
            (0x100, PdfExternalFontEmbeddingRights.Allowed, false, false, true, false),
            (0x108, PdfExternalFontEmbeddingRights.Editable, false, false, true, false),
            (0x104, PdfExternalFontEmbeddingRights.Allowed, false, false, false, false),
            (0x102, PdfExternalFontEmbeddingRights.Restricted, false, false, false, false),
            (0x200, PdfExternalFontEmbeddingRights.Allowed, false, false, false, false),
            (0x208, PdfExternalFontEmbeddingRights.Allowed, false, false, false, false),
            (0, PdfExternalFontEmbeddingRights.Restricted, false, false, false, false),
            (0, PdfExternalFontEmbeddingRights.Unknown, false, false, false, false),
            (0, PdfExternalFontEmbeddingRights.PreviewAndPrint, false, false, false, false),
            (0, PdfExternalFontEmbeddingRights.Allowed, true, false, true, false),
            (8, PdfExternalFontEmbeddingRights.Allowed, false, true, false, false),
            (8, PdfExternalFontEmbeddingRights.SubsetOnly, false, false, false, true),
            (0x100, PdfExternalFontEmbeddingRights.SubsetOnly, false, false, false, false),
            (0, PdfExternalFontEmbeddingRights.SubsetOnly, true, false, false, false),
            (12, PdfExternalFontEmbeddingRights.Allowed, false, false, false, false)
        })
            yield return [operation, row.Flags, row.Provider, row.NoSubset, row.Bitmap, operation == "full" ? row.Full : row.Subset];
    }

    [Theory]
    [MemberData(nameof(ProjectionCases))]
    public void ProjectorsIntersectProviderAndProgramBeforeAllocating(
        string operation, ushort flags, PdfExternalFontEmbeddingRights provider, bool noSubset, bool bitmap, bool permitted)
    {
        using var context = new PdfFontCollisionTestDocument("direct");
        var program = Program(3, flags);
        var face = new PdfExternalFontFace("test", "face", "Test", "Test", 400, 5,
            PdfExternalFontSlant.Upright, PdfExternalFontTechnology.TrueType, provider)
        { NoSubsetting = noSubset, BitmapEmbeddingOnly = bitmap };
        var plan = PdfWritePlanner.Create(context.Document);
        PdfObjectIdentifier firstFree = plan.CreateObjectAllocator().ReserveNext();
        var failure = Record.Exception(() => Project(plan, face, program, operation));
        if (permitted) Assert.Null(failure);
        else Assert.IsType<NotSupportedException>(failure);
        context.AssertOriginalGraph();
        Assert.False(context.Text.Session.History.CanUndo);
        Assert.Equal(firstFree, plan.CreateObjectAllocator().ReserveNext());
    }

    [Theory]
    [InlineData("subset")]
    [InlineData("type0")]
    public void ForbiddenSubsetRejectsBeforeGlyphSelection(string operation)
    {
        using var context = new PdfFontCollisionTestDocument("direct");
        var failure = Assert.Throws<NotSupportedException>(() => Project(
            PdfWritePlanner.Create(context.Document), context.Face, Program(3, 0x100), operation, "\U0001FFFF"));
        Assert.Contains("subsetting", failure.Message, StringComparison.Ordinal);
        // The same unmapped text reaches glyph selection when permission allows it.
        Assert.Throws<NotSupportedException>(() => Project(
            PdfWritePlanner.Create(context.Document), context.Face, Program(3, 0), operation, "\U0001FFFF"));
        context.AssertOriginalGraph();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void DirectSubsetterAlsoEnforcesProgramBeforeResourceWork(bool unicode)
    {
        var program = Program(3, 0x108);
        var options = new PdfTrueTypeSubsetOptions { MaximumRetainedGlyphs = 1 };
        var failure = Assert.Throws<NotSupportedException>(() =>
        {
            if (unicode) _ = new PdfUnicodeTrueTypeGlyphSubsetter().CreateSubset(program, "A", options);
            else _ = new PdfTrueTypeGlyphSubsetter().CreateSubset(program, "A", options);
        });
        Assert.Contains("subsetting", failure.Message, StringComparison.Ordinal);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var canceled = Assert.Throws<OperationCanceledException>(() =>
        {
            if (unicode) _ = new PdfUnicodeTrueTypeGlyphSubsetter().CreateSubset(program, "A", options, cancellation.Token);
            else _ = new PdfTrueTypeGlyphSubsetter().CreateSubset(program, "A", options, cancellation.Token);
        });
        Assert.Equal(cancellation.Token, canceled.CancellationToken);
    }

    private static PdfObjectWriteProjectionSet Project(PdfWritePlan plan, PdfExternalFontFace face,
        PdfSanitizedExternalFontProgram program, string operation, string text = "A") => operation switch
        {
            "full" => PdfExternalTrueTypeResourceProjector.Create(plan, PdfFilteredTextTestDocument.PageId, "FExt1", face, program).Projections,
            "subset" => PdfExternalTrueTypeSubsetResourceProjector.Create(plan, PdfFilteredTextTestDocument.PageId, "FExt1", face, program, text).Projections,
            "type0" => PdfExternalType0TrueTypeResourceProjector.Create(plan, PdfFilteredTextTestDocument.PageId, "FExt1", face, program, text).Projections,
            _ => throw new ArgumentOutOfRangeException(nameof(operation))
        };
}
