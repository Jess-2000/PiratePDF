using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BlackFlag.Engine.Document.Model;
using BlackFlag.Engine.Document.Objects;
using BlackFlag.Engine.Document.Pages;
using BlackFlag.Engine.Objects;
using BlackFlag.Engine.Rendering.DisplayList;
using BlackFlag.Engine.Rendering.Graphics;
using BlackFlag.Engine.Rendering.Resources;
using BlackFlag.Engine.SDK.Adapters.Editing;
using BlackFlag.Engine.SDK.Editing;
using BlackFlag.Engine.Services.Text;

namespace BlackFlag.Engine.SDK.Adapters.Tests.Editing;

public sealed class PdfExistingTextSdkSessionTests
{
    [Fact]
    public void RegisteredSourceDiscoversSdkOwnedTargetAndHidesInternalProvenance()
    {
        var document = PdfDocument.CreateEmpty();
        document.Pages.Add(new PdfPage(612, 792));

        using var sdk = new PdfEditingSdkSession(document);
        PdfEditingSdkPage page = Assert.Single(sdk.Pages);

        Assert.True(PdfGraphicsOperator.TryGet("Tj", out PdfGraphicsOperator textOperator));
        PdfGraphicsOperation operation =
            new(
                textOperator!,
                [new PdfLiteralString("Hello")],
                new PdfContentSourceSpan(0, 10, 20));

        PdfResolvedFont font = CreateFont();
        var run = new PdfDisplayTextRun(
            font,
            12,
            [PdfTextRunElement.CreateText(Encoding.ASCII.GetBytes("Hello"), "Hello")],
            PdfMatrix.Identity,
            new PdfGraphicsStateSnapshot
            {
                Font = font,
                FontSize = 12,
                HorizontalScaling = 100,
                TextMatrix = PdfMatrix.Identity,
                TextLineMatrix = PdfMatrix.Identity
            },
            operation.SourceSpan,
            null);

        var source = new PdfExistingTextDiscoverySource(
            new PdfObjectIdentifier(4),
            Engine.Editing.Text.PdfExistingTextSourceKind.PageContent,
            [operation],
            new PdfDisplayList([run]),
            [new PdfObjectIdentifier(9)]);

        Guid sourceId = sdk.RegisterExistingTextSource(page.Id, source);
        IPdfExistingTextSdkSession textSession = sdk;
        PdfExistingTextSdkTarget target = Assert.Single(textSession.DiscoverExistingText(page.Id));

        Assert.Equal(sourceId, target.SourceId);
        Assert.Equal(page.Id, target.PageId);
        Assert.Equal("Hello", target.Text);
        Assert.Equal(PdfExistingTextSdkEditability.DirectlyEditable, target.Editability);
    }

    [Fact]
    public void IsolatedSdkInterfaceDoesNotExposeInternalExistingTextTypes()
    {
        Type interfaceType = typeof(IPdfExistingTextSdkSession);
        IEnumerable<Type> exposed = interfaceType.GetMethods()
            .SelectMany(static method => method.GetParameters().Select(static parameter => parameter.ParameterType).Append(method.ReturnType));

        Assert.DoesNotContain(exposed, static type =>
            type.Assembly.GetName().Name is string name &&
            name.StartsWith("BlackFlag.Engine.", StringComparison.Ordinal) &&
            name != "BlackFlag.Engine.SDK");
    }

    [Fact]
    public void ExistingTextInterfaceUsesThePublicAdapterImplementations()
    {
        var mapping = typeof(PdfEditingSdkSession).GetInterfaceMap(typeof(IPdfExistingTextSdkSession));

        for (int index = 0; index < mapping.InterfaceMethods.Length; index++)
        {
            Assert.True(mapping.TargetMethods[index].IsPublic);
            Assert.Equal(mapping.InterfaceMethods[index].Name, mapping.TargetMethods[index].Name);
        }
    }

    [Fact]
    public void InterfaceTypographyMapsAllFieldsInvalidatesTargetAndSupportsUndoRedo()
    {
        using var context = CreateContext();
        IPdfExistingTextSdkSession textSession = context.Sdk;
        var target = Assert.Single(textSession.DiscoverExistingText(context.PageId));
        context.Sdk.BeginTransaction();

        PdfEditingSdkOperation operation = textSession.ApplyExistingTextTypography(
            target.Id,
            new PdfExistingTextSdkTypographyUpdate
            {
                FontSize = 18,
                CharacterSpacing = 3,
                WordSpacing = 4,
                HorizontalScaling = 80,
                TextRenderingMode = 2,
                TextRise = 5
            });
        context.Sdk.CommitTransaction();

        const string expected = "BT\n/F1 12 Tf\n" +
            "/F1 18 Tf\n3 Tc\n4 Tw\n80 Tz\n2 Tr\n5 Ts\n(Hello) Tj\n" +
            "/F1 12 Tf\n0 Tc\n0 Tw\n100 Tz\n0 Tr\n0 Ts\n(Next) Tj\nET\n";
        Assert.Equal(expected, context.ReadContent());
        Assert.Equal("ExistingText", operation.Kind);
        Assert.Equal(context.PageId, operation.PageId);
        Assert.Empty(textSession.DiscoverExistingText(context.PageId));
        Assert.Throws<InvalidOperationException>(() => textSession.ApplyExistingTextTypography(
            target.Id, new PdfExistingTextSdkTypographyUpdate { FontSize = 20 }));

        context.Sdk.Undo();
        Assert.Equal(context.Original, context.ReadContent());
        context.Sdk.Redo();
        Assert.Equal(expected, context.ReadContent());
    }

    [Theory]
    [InlineData(PdfExistingTextSdkScriptStyle.Superscript, false, "/F1 6 Tf\n3 Ts\n", "42")]
    [InlineData(PdfExistingTextSdkScriptStyle.Subscript, true, "/F1 9 Tf\n-6 Ts\n", "30")]
    public void InterfaceStyleUsesScriptColorAndDecorationMappings(
        PdfExistingTextSdkScriptStyle scriptStyle,
        bool automaticDecoration,
        string scriptOperators,
        string decorationWidth)
    {
        using var context = CreateContext();
        IPdfExistingTextSdkSession textSession = context.Sdk;
        var target = Assert.Single(textSession.DiscoverExistingText(context.PageId));
        context.Sdk.BeginTransaction();

        textSession.ApplyExistingTextStyle(target.Id, new PdfExistingTextSdkStyleUpdate
        {
            ScriptStyle = scriptStyle,
            SuperscriptScale = 0.5,
            SuperscriptRise = 0.25,
            SubscriptScale = 0.75,
            SubscriptDrop = 0.5,
            FillColor = new PdfExistingTextSdkColor(PdfExistingTextSdkColorSpace.DeviceRgb, [1, 0, 0]),
            StrokeColor = new PdfExistingTextSdkColor(PdfExistingTextSdkColorSpace.DeviceCmyk, [0, 1, 0, 0]),
            Decoration = new PdfExistingTextSdkDecoration(
                PdfExistingTextSdkDecorationKind.Underline,
                automaticDecoration ? null : 42,
                offset: -2,
                thickness: 1)
        });
        context.Sdk.CommitTransaction();

        string expected = "BT\n/F1 12 Tf\n" + scriptOperators +
            "1 0 0 rg\n0 1 0 0 K\n(Hello) Tj\n/F1 12 Tf\n0 Ts\n0 g\n0 G\n" +
            "q\n0 G\n1 w\n0 -2 m\n" + decorationWidth + " -2 l\nS\nQ\n\n(Next) Tj\nET\n";
        Assert.Equal(expected, context.ReadContent());
        Assert.Empty(textSession.DiscoverExistingText(context.PageId));
        Assert.Throws<InvalidOperationException>(() => textSession.ApplyExistingTextStyle(
            target.Id, new PdfExistingTextSdkStyleUpdate { ScriptStyle = scriptStyle }));

        context.Sdk.Undo();
        Assert.Equal(context.Original, context.ReadContent());
        context.Sdk.Redo();
        Assert.Equal(expected, context.ReadContent());
    }

    [Fact]
    public void InvalidStyleMappingDoesNotMutateOrInvalidateTarget()
    {
        using var context = CreateContext();
        IPdfExistingTextSdkSession textSession = context.Sdk;
        var target = Assert.Single(textSession.DiscoverExistingText(context.PageId));
        var update = new PdfExistingTextSdkStyleUpdate
        {
            ScriptStyle = (PdfExistingTextSdkScriptStyle)99
        };

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            textSession.ApplyExistingTextStyle(target.Id, update));
        Assert.Equal(context.Original, context.ReadContent());
        Assert.Equal(target.Id, Assert.Single(textSession.DiscoverExistingText(context.PageId)).Id);
    }

    [Theory]
    [InlineData(-1, 1)]
    [InlineData(0, 2)]
    [InlineData(1, 1)]
    [InlineData(2, 3)]
    public void SdkColorRejectsInvalidSpaceOrComponentCount(int colorSpace, int componentCount)
    {
        double[] values = Enumerable.Repeat(0.5, componentCount).ToArray();
        Assert.ThrowsAny<ArgumentException>(() =>
            new PdfExistingTextSdkColor((PdfExistingTextSdkColorSpace)colorSpace, values));
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(-0.01)]
    [InlineData(1.01)]
    public void SdkColorRejectsNonFiniteOrOutOfRangeComponents(double component)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new PdfExistingTextSdkColor(PdfExistingTextSdkColorSpace.DeviceGray, [component]));
    }

    [Fact]
    public void SdkDecorationRejectsInvalidGeometryAtConstruction()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new PdfExistingTextSdkDecoration((PdfExistingTextSdkDecorationKind)99));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new PdfExistingTextSdkDecoration(PdfExistingTextSdkDecorationKind.Underline, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new PdfExistingTextSdkDecoration(PdfExistingTextSdkDecorationKind.Underline, 1, double.NaN));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new PdfExistingTextSdkDecoration(PdfExistingTextSdkDecorationKind.Underline, 1, thickness: 0));
    }

    [Fact]
    public void NullUpdatesAreRejectedWithoutInvalidatingTarget()
    {
        using var context = CreateContext();
        IPdfExistingTextSdkSession textSession = context.Sdk;
        var target = Assert.Single(textSession.DiscoverExistingText(context.PageId));

        Assert.Throws<ArgumentNullException>(() => textSession.ApplyExistingTextTypography(target.Id, null));
        Assert.Throws<ArgumentNullException>(() => textSession.ApplyExistingTextStyle(target.Id, null));
        Assert.Equal(context.Original, context.ReadContent());
        Assert.Equal(target.Id, Assert.Single(textSession.DiscoverExistingText(context.PageId)).Id);
    }

    [Fact]
    public void NonTextContentMutationInvalidatesRegisteredTextTargets()
    {
        using var context = CreateContext();
        IPdfExistingTextSdkSession textSession = context.Sdk;
        var target = Assert.Single(textSession.DiscoverExistingText(context.PageId));

        context.Sdk.BeginTransaction();
        _ = context.Sdk.ReplacePageContent(context.PageId, Encoding.ASCII.GetBytes("BT (Changed) Tj ET"));
        _ = context.Sdk.CommitTransaction();

        Assert.Empty(textSession.DiscoverExistingText(context.PageId));
        Assert.Throws<InvalidOperationException>(() =>
            textSession.ReplaceExistingText(target.Id, "Again"));
    }

    [Fact]
    public void UndoAndRedoInvalidateRegisteredTextTargets()
    {
        using var context = CreateContext();
        IPdfExistingTextSdkSession textSession = context.Sdk;

        context.Sdk.BeginTransaction();
        _ = context.Sdk.SetDocumentInformation(PdfEditingSdkDocumentInformationField.Title, "Changed");
        _ = context.Sdk.CommitTransaction();
        var beforeUndo = Assert.Single(textSession.DiscoverExistingText(context.PageId));

        _ = context.Sdk.Undo();
        Assert.Throws<InvalidOperationException>(() =>
            textSession.ReplaceExistingText(beforeUndo.Id, "Again"));

        context.Sdk.RegisterExistingTextSource(context.PageId, context.Source);
        var beforeRedo = Assert.Single(textSession.DiscoverExistingText(context.PageId));
        _ = context.Sdk.Redo();
        Assert.Throws<InvalidOperationException>(() =>
            textSession.ReplaceExistingText(beforeRedo.Id, "Again"));
    }

    [Fact]
    public void ReplacingRegisteredSourceCreatesFreshSnapshotScopedTargetId()
    {
        using var context = CreateContext();
        IPdfExistingTextSdkSession textSession = context.Sdk;
        Guid firstTargetId = Assert.Single(textSession.DiscoverExistingText(context.PageId)).Id;

        context.Sdk.RegisterExistingTextSource(context.PageId, context.Source);
        Guid secondTargetId = Assert.Single(textSession.DiscoverExistingText(context.PageId)).Id;

        Assert.NotEqual(firstTargetId, secondTargetId);
        Assert.Throws<InvalidOperationException>(() =>
            textSession.ReplaceExistingText(firstTargetId, "Again"));
    }

    [Theory]
    [InlineData("replace")]
    [InlineData("replaceAll")]
    [InlineData("positioned")]
    [InlineData("font")]
    public void InterfaceTextWorkflowsReachServicesAndShareHistory(string workflow)
    {
        using var context = CreateContext(positioned: workflow == "positioned");
        IPdfExistingTextSdkSession textSession = context.Sdk;
        var target = Assert.Single(textSession.DiscoverExistingText(context.PageId));
        var match = Assert.Single(textSession.FindExistingText("Hel"));
        Assert.Equal(target.Id, match.TargetId);
        Assert.Contains(textSession.GetExistingTextFontCandidates(target.Id),
            candidate => candidate.ResourceName == "F2");

        if (workflow != "replaceAll") context.Sdk.BeginTransaction();
        string expectedTarget;
        switch (workflow)
        {
            case "replace":
                var replacement = textSession.ReplaceExistingText(target.Id, "World");
                Assert.Equal(PdfExistingTextSdkAdvanceFit.Exact, replacement.Measurement.Fit);
                Assert.Equal(30, replacement.Measurement.OriginalAdvance);
                Assert.Equal(30, replacement.Measurement.ReplacementAdvance);
                expectedTarget = "(World) Tj";
                break;
            case "replaceAll":
                var result = textSession.ReplaceAllExistingText("Hello", "World");
                Assert.Equal(1, result.MatchCount);
                Assert.Equal(target.Id, Assert.Single(result.Matches).TargetId);
                expectedTarget = "(World) Tj";
                break;
            case "positioned":
                textSession.ReplacePositionedExistingText(target.Id, ["Wor", "ld"]);
                expectedTarget = "[(Wor) -120 (ld)] TJ";
                break;
            default:
                textSession.ChangeExistingTextFont(target.Id, "F2");
                expectedTarget = "/F2 12 Tf\n(Hello) Tj\n/F1 12 Tf";
                break;
        }
        if (workflow != "replaceAll") context.Sdk.CommitTransaction();

        string expected = "BT\n/F1 12 Tf\n" + expectedTarget + "\n(Next) Tj\nET\n";
        Assert.Equal(expected, context.ReadContent());
        Assert.Empty(textSession.DiscoverExistingText(context.PageId));
        Assert.Throws<InvalidOperationException>(() => textSession.ReplaceExistingText(target.Id, "Again"));
        context.Sdk.Undo();
        Assert.Equal(context.Original, context.ReadContent());
        context.Sdk.Redo();
        Assert.Equal(expected, context.ReadContent());
    }

    private static TestContext CreateContext(bool positioned = false)
    {
        string show = positioned ? "[(Hel) -120 (lo)] TJ" : "(Hello) Tj";
        string content = "BT\n/F1 12 Tf\n" + show + "\n(Next) Tj\nET\n";
        var document = PdfDocument.CreateEmpty();
        var pageId = new PdfObjectIdentifier(3);
        var contentId = new PdfObjectIdentifier(4);
        var page = new PdfPage(612, 792);
        document.Pages.Add(page);
        var stream = new Document.Objects.PdfStream(
            [], Encoding.ASCII.GetBytes(content));
        document.Objects.Add(contentId, stream);
        var sdk = new PdfEditingSdkSession(document);
        Guid sdkPageId = Assert.Single(sdk.Pages).Id;
        Assert.True(PdfGraphicsOperator.TryGet("Tf", out var fontOperator));
        Assert.True(PdfGraphicsOperator.TryGet(positioned ? "TJ" : "Tj", out var textOperator));
        int start = content.IndexOf(show, StringComparison.Ordinal);
        Objects.PdfObject[] operands = positioned
            ? [new Engine.Objects.PdfArray([new PdfLiteralString("Hel"), new PdfInteger(-120), new PdfLiteralString("lo")])]
            : [new PdfLiteralString("Hello")];
        var operation = new PdfGraphicsOperation(textOperator,
            operands, new PdfContentSourceSpan(0, start, start + show.Length));
        var fontSelection = new PdfGraphicsOperation(fontOperator,
            [new PdfName("F1"), new PdfInteger(12)], new PdfContentSourceSpan(0, 3, 12));
        PdfResolvedFont font = CreateFont();
        PdfTextRunElement[] elements = positioned
            ? [PdfTextRunElement.CreateText("Hel"u8, "Hel"),
                PdfTextRunElement.CreateAdjustment(-120), PdfTextRunElement.CreateText("lo"u8, "lo")]
            : [PdfTextRunElement.CreateText("Hello"u8, "Hello")];
        var run = new PdfDisplayTextRun(font, 12, elements, PdfMatrix.Identity,
            new PdfGraphicsStateSnapshot { Font = font, FontSize = 12 }, operation.SourceSpan, null);
        var source = new PdfExistingTextDiscoverySource(pageId,
            Engine.Editing.Text.PdfExistingTextSourceKind.PageContent,
            [fontSelection, operation], new PdfDisplayList([run]), [contentId],
            availableFonts: new Dictionary<string, PdfResolvedFont> { ["F1"] = font, ["F2"] = font });
        sdk.RegisterExistingTextSource(sdkPageId, source);
        return new TestContext(sdk, sdkPageId, stream, content, source);
    }

    private sealed record TestContext(
        PdfEditingSdkSession Sdk,
        Guid PageId,
        Document.Objects.PdfStream Stream,
        string Original,
        PdfExistingTextDiscoverySource Source) : IDisposable
    {
        public string ReadContent() => Encoding.ASCII.GetString(Stream.ToByteArray());
        public void Dispose() => Sdk.Dispose();
    }

    private static PdfResolvedFont CreateFont()
    {
        return new(
            PdfResourceIdentity.ForLocal(1),
            new Objects.PdfDictionary([]),
            "TrueType",
            "Test Sans",
            "Test Sans Regular",
            400,
            5,
            PdfFontSlant.Upright,
            PdfFontTechnology.TrueType,
            PdfFontEmbeddingRights.Unknown,
            isSubset: false,
            encoding: new PdfName("WinAnsiEncoding"),
            toUnicode: null,
            descriptor: new Objects.PdfDictionary(
                [new KeyValuePair<PdfName, Objects.PdfObject>(new PdfName("MissingWidth"), new PdfInteger(500))]),
            descendantFonts: null,
            embeddedFontProgram: null,
            variationAxes: []);
    }

}
