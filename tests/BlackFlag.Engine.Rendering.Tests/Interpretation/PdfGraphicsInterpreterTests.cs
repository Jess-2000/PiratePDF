#nullable enable
using BlackFlag.Engine.Objects;
using BlackFlag.Engine.Rendering.DisplayList;
using BlackFlag.Engine.Rendering.Graphics;
using BlackFlag.Engine.Rendering.Interpretation;
using BlackFlag.Engine.Rendering.Resources;

namespace BlackFlag.Engine.Rendering.Tests.Interpretation;

public sealed class PdfGraphicsInterpreterTests
{
    [Fact]
    public void InterpretBuildsTransformedPath()
    {
        PdfResourceScope scope = CreateScope();
        PdfGraphicsInterpreter interpreter = new();

        PdfDisplayList result = interpreter.Interpret(
            [
                Operation("cm", 0, new PdfInteger(2), new PdfInteger(0), new PdfInteger(0),
                    new PdfInteger(2), new PdfInteger(10), new PdfInteger(20)),
                Operation("m", 1, new PdfInteger(1), new PdfInteger(1)),
                Operation("l", 2, new PdfInteger(5), new PdfInteger(1)),
                Operation("S", 3)
            ],
            scope);

        PdfDisplayPath path = Assert.IsType<PdfDisplayPath>(Assert.Single(result));
        Assert.True(path.Stroke);
        Assert.False(path.Fill);
        Assert.Equal(new PdfPoint(12, 22), path.Path.Segments[0].EndPoint);
        Assert.Equal(new PdfPoint(20, 22), path.Path.Segments[1].EndPoint);
    }

    [Fact]
    public void GraphicsStateSaveRestoreRestoresLineWidth()
    {
        PdfResourceScope scope = CreateScope();
        PdfGraphicsInterpreter interpreter = new();

        PdfDisplayList result = interpreter.Interpret(
            [
                Operation("w", 0, new PdfInteger(2)),
                Operation("q", 1),
                Operation("w", 2, new PdfInteger(9)),
                Operation("Q", 3),
                Operation("m", 4, new PdfInteger(0), new PdfInteger(0)),
                Operation("l", 5, new PdfInteger(10), new PdfInteger(0)),
                Operation("S", 6)
            ],
            scope);

        PdfDisplayPath path = Assert.IsType<PdfDisplayPath>(Assert.Single(result));
        Assert.Equal(2d, path.GraphicsState.LineWidth);
    }

    [Fact]
    public void TextShowingProducesEditableTextRun()
    {
        PdfResolvedFont font = CreateFont();
        PdfResourceScope scope = CreateScope(
            (PdfResourceKind.Font, "F1", font));
        PdfGraphicsInterpreter interpreter = new();

        PdfDisplayList result = interpreter.Interpret(
            [
                Operation("BT", 0),
                Operation("Tf", 1, new PdfName("F1"), new PdfInteger(12)),
                Operation("Tj", 2, new PdfLiteralString("PiratePDF")),
                Operation("ET", 3)
            ],
            scope);

        PdfDisplayTextRun text = Assert.IsType<PdfDisplayTextRun>(Assert.Single(result));
        Assert.Same(font, text.Font);
        Assert.Equal(12d, text.FontSize);
        Assert.Null(text.UnicodeText);

        Assert.Equal("PiratePDF", System.Text.Encoding.Latin1.GetString(
            text.EncodedBytes.Span));
    }


    [Fact]
    public void TextArrayPreservesOrderedTextAndAdjustments()
    {
        PdfResolvedFont font = CreateFont();
        PdfResourceScope scope = CreateScope(
            (PdfResourceKind.Font, "F1", font));
        PdfGraphicsInterpreter interpreter = new();

        PdfDisplayList result = interpreter.Interpret(
            [
                Operation("BT", 0),
                Operation("Tf", 1, new PdfName("F1"), new PdfInteger(10)),
                Operation(
                    "TJ",
                    2,
                    new PdfArray(
                    [
                        new PdfLiteralString("A"),
                        new PdfInteger(-120),
                        new PdfLiteralString("B")
                    ])),
                Operation("ET", 3)
            ],
            scope);

        PdfDisplayTextRun text = Assert.IsType<PdfDisplayTextRun>(Assert.Single(result));
        Assert.Equal(3, text.Elements.Count);
        Assert.Equal(PdfTextRunElementKind.Text, text.Elements[0].Kind);
        Assert.Equal(PdfTextRunElementKind.Adjustment, text.Elements[1].Kind);
        Assert.Equal(-120d, text.Elements[1].Adjustment);
        Assert.Equal(PdfTextRunElementKind.Text, text.Elements[2].Kind);
        Assert.Null(text.UnicodeText);

        Assert.Equal("AB", System.Text.Encoding.Latin1.GetString(
            text.EncodedBytes.Span));
    }

    [Fact]
    public void ImageXObjectProducesCanonicalImagePlacement()
    {
        PdfResolvedImageXObject image = CreateImage();
        PdfResourceScope scope = CreateScope(
            (PdfResourceKind.XObject, "Im1", image));
        PdfGraphicsInterpreter interpreter = new();

        PdfDisplayList result = interpreter.Interpret(
            [Operation("Do", 0, new PdfName("Im1"))],
            scope);

        PdfDisplayImage displayImage = Assert.IsType<PdfDisplayImage>(Assert.Single(result));
        Assert.Same(image, displayImage.ImageResource);
        Assert.Null(displayImage.InlineImage);
        Assert.Equal(1d, displayImage.Bounds!.Value.Width);
        Assert.Equal(1d, displayImage.Bounds.Value.Height);
    }

    [Fact]
    public void InlineImageUsesSameDisplayItemTypeAsImageXObject()
    {
        PdfResourceScope scope = CreateScope();
        PdfGraphicsInterpreter interpreter = new();
        var inline = new PdfInlineImage(PdfDictionary.Empty, [1, 2, 3]);

        PdfDisplayList result = interpreter.Interpret(
            [Operation("BI", 0, inlineImage: inline)],
            scope);

        PdfDisplayImage image = Assert.IsType<PdfDisplayImage>(Assert.Single(result));
        Assert.Same(inline, image.InlineImage);
        Assert.Null(image.ImageResource);
    }

    [Fact]
    public void ClippingIsAppliedWhenPathEnds()
    {
        PdfResourceScope scope = CreateScope();
        PdfGraphicsInterpreter interpreter = new();

        PdfDisplayList result = interpreter.Interpret(
            [
                Operation("re", 0, new PdfInteger(0), new PdfInteger(0),
                    new PdfInteger(100), new PdfInteger(100)),
                Operation("W", 1),
                Operation("n", 2),
                Operation("m", 3, new PdfInteger(1), new PdfInteger(1)),
                Operation("l", 4, new PdfInteger(5), new PdfInteger(5)),
                Operation("S", 5)
            ],
            scope);

        PdfDisplayPath path = Assert.IsType<PdfDisplayPath>(Assert.Single(result));
        Assert.NotNull(path.GraphicsState.Clip);
        Assert.Equal(PdfFillRule.Nonzero, path.GraphicsState.Clip!.FillRule);
    }

    [Fact]
    public void MarkedContentContextIsAttachedToNestedItems()
    {
        PdfResourceScope scope = CreateScope();
        PdfGraphicsInterpreter interpreter = new();

        PdfDisplayList result = interpreter.Interpret(
            [
                Operation("BMC", 0, new PdfName("Span")),
                Operation("m", 1, new PdfInteger(0), new PdfInteger(0)),
                Operation("l", 2, new PdfInteger(1), new PdfInteger(1)),
                Operation("S", 3),
                Operation("EMC", 4)
            ],
            scope);

        PdfDisplayPath path = Assert.IsType<PdfDisplayPath>(Assert.Single(result));
        Assert.NotNull(path.MarkedContent);
        Assert.Equal("Span", path.MarkedContent!.Tag.Value);
    }

    [Fact]
    public void RecursiveFormIsRejectedDeterministically()
    {
        PdfResourceScope? scope = null;
        PdfResolvedFormXObject form = CreateForm(() => scope);
        scope = CreateScope((PdfResourceKind.XObject, "Fm1", form));

        var provider = new StaticFormProvider(
            [Operation("Do", 0, new PdfName("Fm1"))]);
        PdfGraphicsInterpreter interpreter = new(formOperationProvider: provider);

        PdfGraphicsInterpretationException exception = Assert.Throws<PdfGraphicsInterpretationException>(
            () => interpreter.Interpret(
                [Operation("Do", 0, new PdfName("Fm1"))],
                scope));

        Assert.Equal(PdfGraphicsInterpretationFailure.RecursiveForm, exception.Failure);
    }

    private static PdfGraphicsOperation Operation(
        string code,
        long offset,
        params PdfObject[] operands) =>
        Operation(code, offset, null, operands);

    private static PdfGraphicsOperation Operation(
        string code,
        long offset,
        PdfInlineImage? inlineImage,
        params PdfObject[] operands)
    {
        Assert.True(PdfGraphicsOperator.TryGet(code, out PdfGraphicsOperator? value));
        return new PdfGraphicsOperation(
            value!,
            operands,
            new PdfContentSourceSpan(0, offset, offset + 1),
            inlineImage);
    }

    private static PdfResourceScope CreateScope(
        params (PdfResourceKind Kind, string Name, PdfResolvedResource Resource)[] resources)
    {
        Dictionary<(PdfResourceKind Kind, string Name), PdfResolvedResource> lookup =
            resources.ToDictionary(
                static item => (item.Kind, item.Name),
                static item => item.Resource);

        return new PdfResourceScope(
            (kind, name) => lookup.TryGetValue((kind, name.Value), out PdfResolvedResource? value)
                ? value
                : null,
            parent: null);
    }

    private static PdfResolvedFont CreateFont() =>
        new(
            PdfResourceIdentity.ForLocal(1),
            PdfDictionary.Empty,
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
            variationAxes: Array.Empty<PdfFontVariationAxis>());

    private static PdfResolvedImageXObject CreateImage() =>
        new(
            PdfResourceIdentity.ForLocal(2),
            new PdfStream(PdfDictionary.Empty, ReadOnlyMemory<byte>.Empty),
            10,
            20,
            8,
            isImageMask: false,
            interpolate: false,
            colorSpace: null,
            decode: null,
            mask: null,
            softMask: null,
            filters: null);

    private static PdfResolvedFormXObject CreateForm(
        Func<PdfResourceScope?> scopeFactory) =>
        new(
            PdfResourceIdentity.ForLocal(3),
            new PdfStream(PdfDictionary.Empty, ReadOnlyMemory<byte>.Empty),
            new PdfArray(
            [
                new PdfInteger(0),
                new PdfInteger(0),
                new PdfInteger(10),
                new PdfInteger(10)
            ]),
            matrix: null,
            resourceScope: new Lazy<PdfResourceScope?>(scopeFactory),
            group: null);

    private sealed class StaticFormProvider : IPdfFormOperationProvider
    {
        private readonly IReadOnlyList<PdfGraphicsOperation> operations;

        public StaticFormProvider(IReadOnlyList<PdfGraphicsOperation> operations)
        {
            this.operations = operations;
        }

        public IEnumerable<PdfGraphicsOperation> GetOperations(
            PdfResolvedFormXObject form,
            PdfResourceScope scope)
        {
            return operations;
        }
    }
}
