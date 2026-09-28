using System.IO;
using System.Text;
using System.Threading;
using BlackFlag.Engine.Editing.Text;
using BlackFlag.Engine.Filters;
using BlackFlag.Engine.Rendering.DisplayList;
using BlackFlag.Engine.Services.Text;

namespace BlackFlag.Engine.Services.Tests.Text;

public sealed class PdfExistingTextDecorationPreservationTests
{
    [Fact]
    public void TaggedPropertyDictionaryIsNotDuplicatedOrRewritten()
    {
        using var context = new PdfDecorationTestContext();
        var properties = new BlackFlag.Engine.Objects.PdfDictionary(
            [new(new BlackFlag.Engine.Objects.PdfName("MCID"), new BlackFlag.Engine.Objects.PdfInteger(7))]);
        context.Add("BT").Add("Tf", "/F1", 12).Add("BDC", "/Span", properties)
            .Add("Tj", "AB").Add("EMC").Add("Tj", "After").Add("ET").Finish();
        context.Apply(PdfDecorationTestContext.Underline());
        _ = PdfDecorationSyntaxAssertions.ReadPaths(context.Current);
        Assert.Equal(1, context.Current.Split("/Span << /MCID 7 >> BDC", StringSplitOptions.None).Length - 1);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void CommittedDecorationSavesThroughThePublicTransactionBoundary(bool streamXref)
    {
        using var context = CreateSimple();
        using var transaction = context.Session.BeginTransaction();
        context.Apply(PdfDecorationTestContext.Underline());
        var changes = transaction.Commit();
        byte[] saved = context.Save(changes, streamXref);
        string text = Encoding.ASCII.GetString(saved);
        Assert.Contains("4 2 obj", text, StringComparison.Ordinal);
        Assert.Contains(context.Current, text, StringComparison.Ordinal);
        _ = PdfDecorationSyntaxAssertions.ReadPaths(context.Current);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void NonFiniteGeometryRejectsBeforePublication(bool scriptOverflow)
    {
        using var context = new PdfDecorationTestContext();
        context.Add("BT").Add("Tf", "/F1", 12).Add("Tm", double.MaxValue, 0, 0, 1, 0, 0)
            .Add("Tj", "AB").Add("ET").Finish();
        var update = new PdfExistingTextStyleUpdate
        {
            ScriptStyle = scriptOverflow ? PdfExistingTextScriptStyle.Superscript : null,
            SuperscriptRise = double.MaxValue,
            Decoration = new PdfExistingTextDecoration(PdfExistingTextDecorationKind.Underline, 12)
        };
        Assert.Throws<NotSupportedException>(() => context.Apply(update));
        Assert.Equal(context.Original, context.Current);
        Assert.False(context.Session.History.CanUndo);
    }

    [Fact]
    public void LaterProjectionFailureRollsBackAnEarlierDecoration()
    {
        using var context = CreateSimple(true);
        byte[] original = context.Stream.ToByteArray();
        int objects = context.Document.Objects.Count;
        Assert.Throws<NotSupportedException>(() =>
        {
            using var transaction = context.Session.BeginTransaction();
            context.Apply(PdfDecorationTestContext.Underline());
            context.Apply(new PdfExistingTextStyleUpdate
            {
                ScriptStyle = PdfExistingTextScriptStyle.Superscript,
                SuperscriptRise = double.MaxValue,
                Decoration = new PdfExistingTextDecoration(PdfExistingTextDecorationKind.Underline, 12)
            });
            _ = transaction.Commit();
        });
        Assert.Equal(original, context.Stream.ToByteArray());
        Assert.Equal(objects, context.Document.Objects.Count);
        Assert.False(context.Session.History.CanUndo);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void MarkedScopeInsideTextObjectRetainsProperNesting(bool nested)
    {
        using var context = new PdfDecorationTestContext();
        context.Add("BT").Add("Tf", "/F1", 12).Add("Tj", "Before").Add("BMC", "/Span");
        if (nested) context.Add("BMC", "/Em");
        context.Add("Tj", "AB").Add("Tj", "Inside");
        if (nested) context.Add("EMC");
        context.Add("EMC").Add("Tj", "After").Add("ET").Finish();
        context.Apply(PdfDecorationTestContext.Underline(), 1);
        _ = PdfDecorationSyntaxAssertions.ReadPaths(context.Current);
        Assert.Equal(1, context.Current.Split("/Span BMC", StringSplitOptions.None).Length - 1);
        Assert.Contains("ET\n/Span BMC\nBT\n", context.Current, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void StrokeAndColorAreIsolatedWithinAnExistingClip(int renderingMode)
    {
        using var context = new PdfDecorationTestContext();
        context.Add("re", 0, 0, 300, 300).Add("W").Add("n")
            .Add("RG", 0, 0, 1).Add("w", 7).Add("J", 2).Add("j", 1)
            .Add("BT").Add("Tf", "/F1", 12).Add("Tr", renderingMode)
            .Add("Tj", "AB").Add("ET").Add("m", 1, 2).Add("l", 3, 4).Add("S").Finish();
        context.Apply(new PdfExistingTextStyleUpdate
        {
            FillColor = PdfExistingTextColor.Cmyk(0, 1, 0, 0),
            Decoration = new PdfExistingTextDecoration(PdfExistingTextDecorationKind.Underline, 12, -2, 1)
        });
        PdfDisplayPath[] paths = PdfDecorationSyntaxAssertions.ReadPaths(context.Current);
        Assert.Equal(2, paths.Length);
        Assert.Equal("DeviceCMYK", paths[0].GraphicsState.StrokeColor.ColorSpaceName);
        Assert.Equal(1, paths[0].GraphicsState.LineWidth);
        Assert.Equal("DeviceRGB", paths[1].GraphicsState.StrokeColor.ColorSpaceName);
        Assert.Equal(new double[] { 0, 0, 1 }, paths[1].GraphicsState.StrokeColor.Components);
        Assert.Equal(7, paths[1].GraphicsState.LineWidth);
        Assert.Equal(PdfLineCapStyle.ProjectingSquare, paths[1].GraphicsState.LineCap);
        Assert.Equal(PdfLineJoinStyle.Round, paths[1].GraphicsState.LineJoin);
        Assert.Same(paths[0].GraphicsState.Clip, paths[1].GraphicsState.Clip);
        PdfDecorationSyntaxAssertions.Point(new PdfPoint(1, 2), paths[1].Path.Segments[0].EndPoint);
    }

    [Theory]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    [InlineData(7)]
    public void TextClippingFailsBeforeAnyMutation(int mode)
    {
        using var context = new PdfDecorationTestContext();
        context.Add("BT").Add("Tf", "/F1", 12).Add("Tr", mode)
            .Add("Tj", "AB").Add("Tr", 0).Add("Tj", "CD").Add("ET").Finish();
        Assert.Throws<NotSupportedException>(() => context.Apply(PdfDecorationTestContext.Underline(), 1));
        Assert.Equal(context.Original, context.Current);
        Assert.False(context.Session.History.CanUndo);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void TransactionRollbackUndoAndRedoPreserveExactStreamBytes(bool filtered)
    {
        using var context = CreateSimple(filtered);
        byte[] original = context.Stream.ToByteArray();
        byte[] dictionary = PdfFilteredTextTestDocument.Serialize(context.Stream.Dictionary);
        using (context.Session.BeginTransaction())
        {
            context.Apply(PdfDecorationTestContext.Underline());
            Assert.NotEqual(original, context.Stream.ToByteArray());
        }
        Assert.Equal(original, context.Stream.ToByteArray());
        Assert.False(context.Session.History.CanUndo);
        using (var transaction = context.Session.BeginTransaction())
        {
            context.Apply(PdfDecorationTestContext.Underline());
            _ = transaction.Commit();
        }
        byte[] changed = context.Stream.ToByteArray();
        Assert.Equal(dictionary, PdfFilteredTextTestDocument.Serialize(context.Stream.Dictionary));
        _ = PdfDecorationSyntaxAssertions.ReadPaths(context.Current);
        _ = context.Session.History.Undo();
        Assert.Equal(original, context.Stream.ToByteArray());
        _ = context.Session.History.Redo();
        Assert.Equal(changed, context.Stream.ToByteArray());
        Assert.Equal(dictionary, PdfFilteredTextTestDocument.Serialize(context.Stream.Dictionary));
    }

    [Fact]
    public void StaleTargetAndBoundariesRejectWithoutHistory()
    {
        foreach (string change in new[] { "text", "boundary" })
        {
            using var context = CreateSimple();
            string current = change == "text"
                ? context.Original.Replace("(AB)", "(CD)", StringComparison.Ordinal)
                : context.Original.Replace("ET", "XX", StringComparison.Ordinal);
            context.Stream.ReplaceData(Encoding.ASCII.GetBytes(current));
            Assert.Throws<InvalidOperationException>(() => context.Apply(PdfDecorationTestContext.Underline()));
            Assert.Equal(current, context.Current);
            Assert.False(context.Session.History.CanUndo);
        }
    }

    [Fact]
    public void FormAndSharedSourcesRemainUnsupportedWithoutCloning()
    {
        using var context = CreateSimple();
        int objectCount = context.Document.Objects.Count;
        foreach (bool shared in new[] { false, true })
        {
            var source = new PdfExistingTextDiscoverySource(
                PdfDecorationTestContext.PageId, PdfExistingTextSourceKind.FormXObject,
                context.Source.Operations, context.Source.DisplayList, [PdfDecorationTestContext.ContentId], shared);
            var descriptor = PdfExistingTextDiscoveryService.Discover(context.Session, context.Session.Document.Pages[0], source)[0];
            Assert.Throws<NotSupportedException>(() => PdfExistingTextStyleService.Apply(
                context.Session, context.Session.Document.Pages[0], source, descriptor, PdfDecorationTestContext.Underline()));
        }
        Assert.Equal(context.Original, context.Current);
        Assert.Equal(objectCount, context.Document.Objects.Count);
        Assert.False(context.Session.History.CanUndo);
    }

    [Fact]
    public void FilterFailuresAndCancellationKeepTheirContracts()
    {
        using var context = CreateSimple(true);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var exception = Assert.Throws<OperationCanceledException>(() => PdfExistingTextStyleService.Apply(
            context.Session, context.Session.Document.Pages[0], context.Source, context.Target(),
            PdfDecorationTestContext.Underline(), cancellation.Token));
        Assert.Equal(cancellation.Token, exception.CancellationToken);
        byte[] complete = context.Stream.ToByteArray();
        context.Stream.ReplaceData(complete[..^1]);
        Assert.Throws<InvalidDataException>(() => context.Apply(PdfDecorationTestContext.Underline()));
        byte[] bomb = PdfFilteredTextTestDocument.Compress(new byte[1024 * 1024]);
        context.Stream.ReplaceData(bomb);
        var limit = Assert.Throws<PdfFilterDecodeException>(() => context.Apply(PdfDecorationTestContext.Underline()));
        Assert.Equal(PdfFilterDecodeFailure.ExpansionRatioExceeded, limit.Failure);
        Assert.Equal(bomb, context.Stream.ToByteArray());
        Assert.False(context.Session.History.CanUndo);
    }

    [Fact]
    public void NonDecorationStyleSerializationIsUnchanged()
    {
        using var context = CreateSimple();
        context.Apply(new PdfExistingTextStyleUpdate
        {
            ScriptStyle = PdfExistingTextScriptStyle.Superscript,
            SuperscriptScale = 0.5,
            SuperscriptRise = 0.25,
            FillColor = PdfExistingTextColor.Gray(0.5)
        });
        Assert.Equal("BT\n/F1 12 Tf\n/F1 6 Tf\n3 Ts\n0.5 g\n(AB) Tj\n/F1 12 Tf\n0 Ts\n0 g\n\nET\n", context.Current);
    }

    [Fact]
    public void UnfinishedPathCannotBeConsumedByDecoration()
    {
        using var context = new PdfDecorationTestContext();
        context.Add("m", 1, 2).Add("l", 3, 4).Add("BT").Add("Tf", "/F1", 12)
            .Add("Tj", "AB").Add("ET").Add("S").Finish();
        Assert.Throws<NotSupportedException>(() => context.Apply(PdfDecorationTestContext.Underline()));
        Assert.Equal(context.Original, context.Current);
    }

    private static PdfDecorationTestContext CreateSimple(bool filtered = false) => new PdfDecorationTestContext()
        .Add("BT").Add("Tf", "/F1", 12).Add("Tj", "AB").Add("ET").Finish(filtered);
}
