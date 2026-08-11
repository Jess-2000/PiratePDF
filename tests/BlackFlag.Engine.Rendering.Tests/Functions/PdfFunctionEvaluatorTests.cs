
using BlackFlag.Engine.Objects;
using BlackFlag.Engine.Rendering.Functions;

namespace BlackFlag.Engine.Rendering.Tests.Functions;

public sealed class PdfFunctionEvaluatorTests
{
    [Fact]
    public void Type2InterpolatesOutputs()
    {
        PdfDictionary function = new(
        [
            new(new PdfName("FunctionType"), new PdfInteger(2)),
            new(new PdfName("Domain"), new PdfArray([new PdfInteger(0), new PdfInteger(1)])),
            new(new PdfName("C0"), new PdfArray([new PdfInteger(0), new PdfInteger(0), new PdfInteger(0)])),
            new(new PdfName("C1"), new PdfArray([new PdfInteger(1), new PdfInteger(0), new PdfInteger(0)])),
            new(new PdfName("N"), new PdfInteger(1))
        ]);

        double[] value = PdfFunctionEvaluator.Evaluate(function, [0.5]);

        Assert.Equal(0.5, value[0], 6);
        Assert.Equal(0, value[1], 6);
        Assert.Equal(0, value[2], 6);
    }

    [Fact]
    public void Type3SelectsAndMapsStitchedFunction()
    {
        PdfDictionary first = Type2(0, 1);
        PdfDictionary second = Type2(1, 0);

        PdfDictionary stitching = new(
        [
            new(new PdfName("FunctionType"), new PdfInteger(3)),
            new(new PdfName("Domain"), new PdfArray([new PdfInteger(0), new PdfInteger(1)])),
            new(new PdfName("Functions"), new PdfArray([first, second])),
            new(new PdfName("Bounds"), new PdfArray([new PdfReal(0.5)])),
            new(new PdfName("Encode"), new PdfArray([
                new PdfInteger(0), new PdfInteger(1),
                new PdfInteger(0), new PdfInteger(1)
            ]))
        ]);

        double[] left = PdfFunctionEvaluator.Evaluate(stitching, [0.25]);
        double[] right = PdfFunctionEvaluator.Evaluate(stitching, [0.75]);

        Assert.Equal(0.5, left[0], 6);
        Assert.Equal(0.5, right[0], 6);
    }

    private static PdfDictionary Type2(int start, int end) => new(
    [
        new(new PdfName("FunctionType"), new PdfInteger(2)),
        new(new PdfName("Domain"), new PdfArray([new PdfInteger(0), new PdfInteger(1)])),
        new(new PdfName("C0"), new PdfArray([new PdfInteger(start)])),
        new(new PdfName("C1"), new PdfArray([new PdfInteger(end)])),
        new(new PdfName("N"), new PdfInteger(1))
    ]);
}
