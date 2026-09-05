using System.IO;
using System.Text;
using BlackFlag.Engine.Objects;
using BlackFlag.Engine.Rendering.Graphics;
using BlackFlag.Engine.Services.Text;

namespace BlackFlag.Engine.Services.Tests.Text;

public sealed class PdfCompoundTextOperatorNormalizerTests
{
    [Theory]
    [InlineData("'", false)]
    [InlineData("'", true)]
    [InlineData("\"", false)]
    [InlineData("\"", true)]
    public void NormalizationPreservesStringBytesFormatAndSurroundingContent(
        string operatorCode,
        bool hexadecimal)
    {
        const string prefix = "BT\n/F1 12 Tf\n";
        const string suffix = "\n(Next) Tj\nET";
        byte[] textBytes = [0x41, 0x28, 0x29, 0x5C, 0x00, 0xFF];
        string stringToken = hexadecimal
            ? "<4128295C00FF>"
            : "(A\\(\\)\\\\\\000\\377)";
        string target = operatorCode == "'"
            ? stringToken + " '"
            : "2 -1.25 " + stringToken + " \"";
        byte[] source = Encoding.ASCII.GetBytes(prefix + target + suffix);
        byte[] original = source.ToArray();
        PdfObject text = hexadecimal
            ? new PdfHexString(textBytes)
            : new PdfLiteralString(Encoding.Latin1.GetString(textBytes));
        PdfObject[] operands = operatorCode == "'"
            ? [text]
            : [new PdfInteger(2), new PdfReal(-1.25), text];
        Assert.True(PdfGraphicsOperator.TryGet(operatorCode, out var operationType));
        var operation = new PdfGraphicsOperation(
            operationType,
            operands,
            new PdfContentSourceSpan(0, prefix.Length, prefix.Length + target.Length));

        byte[] normalized = PdfCompoundTextOperatorNormalizer.Normalize(source, operation);

        string expected = prefix +
            (operatorCode == "'" ? "T*\n" : "2 Tw\n-1.25 Tc\nT*\n") +
            stringToken + " Tj" + suffix;
        Assert.Equal(Encoding.ASCII.GetBytes(expected), normalized);
        Assert.Equal(original, source);
    }

    [Theory]
    [InlineData("'", 0)]
    [InlineData("\"", 0)]
    [InlineData("\"", 1)]
    [InlineData("\"", 2)]
    public void InvalidOperandTypesAreRejected(string operatorCode, int operandIndex)
    {
        PdfObject[] operands = operatorCode == "'"
            ? [new PdfLiteralString("Hello")]
            : [new PdfInteger(2), new PdfReal(1.25), new PdfLiteralString("Hello")];
        operands[operandIndex] = new PdfName("Invalid");
        Assert.True(PdfGraphicsOperator.TryGet(operatorCode, out var operationType));
        var operation = new PdfGraphicsOperation(
            operationType,
            operands,
            new PdfContentSourceSpan(0, 0, 0));

        Assert.Throws<InvalidDataException>(
            () => PdfCompoundTextOperatorNormalizer.Normalize([], operation));
    }
}
