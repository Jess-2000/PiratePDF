using System.IO;
using BlackFlag.Engine.Objects;
using BlackFlag.Engine.Rendering.Functions;

namespace BlackFlag.Engine.Rendering.Tests.Functions;

public sealed class PdfFunctionEvaluatorBf1048GTests
{
    [Fact]
    public void Type0LinearlyInterpolatesOneDimensionalSamples()
    {
        PdfDictionary dictionary = new(
        [
            new(new PdfName("FunctionType"), new PdfInteger(0)),
            new(new PdfName("Domain"), new PdfArray([new PdfInteger(0), new PdfInteger(1)])),
            new(new PdfName("Range"), new PdfArray([new PdfInteger(0), new PdfInteger(1)])),
            new(new PdfName("Size"), new PdfArray([new PdfInteger(2)])),
            new(new PdfName("BitsPerSample"), new PdfInteger(8))
        ]);

        PdfStream function = new(dictionary, new byte[] { 0, 255 });

        double[] result = PdfFunctionEvaluator.Evaluate(function, [0.5]);

        Assert.Equal(0.5, result[0], 2);
    }

    [Fact]
    public void Type4ExecutesBoundedCalculatorProgram()
    {
        PdfDictionary dictionary = new(
        [
            new(new PdfName("FunctionType"), new PdfInteger(4)),
            new(new PdfName("Domain"), new PdfArray([new PdfInteger(0), new PdfInteger(1)])),
            new(new PdfName("Range"), new PdfArray([new PdfInteger(0), new PdfInteger(1)]))
        ]);

        PdfStream function = new(
            dictionary,
            System.Text.Encoding.ASCII.GetBytes("dup mul"));

        double[] result = PdfFunctionEvaluator.Evaluate(function, [0.5]);

        Assert.Equal(0.25, result[0], 6);
    }

    [Fact]
    public void Type4OperationLimitIsEnforced()
    {
        PdfDictionary dictionary = new(
        [
            new(new PdfName("FunctionType"), new PdfInteger(4)),
            new(new PdfName("Domain"), new PdfArray([new PdfInteger(0), new PdfInteger(1)])),
            new(new PdfName("Range"), new PdfArray([new PdfInteger(0), new PdfInteger(1)]))
        ]);

        PdfStream function = new(
            dictionary,
            System.Text.Encoding.ASCII.GetBytes("dup dup dup dup dup"));

        var limits = new PdfFunctionEvaluationLimits
        {
            MaximumCalculatorOperations = 2
        };

        Assert.Throws<InvalidDataException>(() =>
            PdfFunctionEvaluator.Evaluate(function, [0.5], limits));
    }
}
