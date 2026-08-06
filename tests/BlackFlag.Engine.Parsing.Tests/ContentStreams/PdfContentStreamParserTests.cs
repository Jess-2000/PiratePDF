using System.Text;
using BlackFlag.Engine.Objects;
using BlackFlag.Engine.Parsing.ContentStreams;
using BlackFlag.Engine.Rendering.Graphics;

namespace BlackFlag.Engine.Parsing.Tests.ContentStreams;

public sealed class PdfContentStreamParserTests
{
    [Fact]
    public void ParseReturnsCanonicalOperationsInSourceOrder()
    {
        IReadOnlyList<PdfGraphicsOperation> operations = Parse("q 1 0 0 1 12 24 cm /Logo Do Q");

        Assert.Equal(4, operations.Count);
        Assert.Equal("q", operations[0].Operator.Code);
        Assert.Equal("cm", operations[1].Operator.Code);
        Assert.Equal(6, operations[1].Operands.Count);
        Assert.Equal("Do", operations[2].Operator.Code);
        Assert.Equal("Logo", Assert.IsType<PdfName>(operations[2].Operands[0]).Value);
        Assert.Equal("Q", operations[3].Operator.Code);
    }

    [Fact]
    public void ParsePreservesCompositeTextOperands()
    {
        IReadOnlyList<PdfGraphicsOperation> operations = Parse("BT /F1 12 Tf [(Hello) -20 (World)] TJ ET");

        Assert.Equal("TJ", operations[2].Operator.Code);
        PdfArray values = Assert.IsType<PdfArray>(operations[2].Operands[0]);
        Assert.Equal(3, values.Count);
    }

    [Fact]
    public void ParseRejectsIncorrectOperandCount()
    {
        PdfContentParseException exception = Assert.Throws<PdfContentParseException>(
            () => Parse("1 2 3 re"));

        Assert.Equal(PdfContentParseFailure.OperandCountMismatch, exception.Failure);
    }

    [Fact]
    public void ParseRejectsIncorrectOperandType()
    {
        PdfContentParseException exception = Assert.Throws<PdfContentParseException>(
            () => Parse("12 Do"));

        Assert.Equal(PdfContentParseFailure.InvalidOperand, exception.Failure);
    }

    [Fact]
    public void ParseSupportsToleratedUnknownOperators()
    {
        var parser = new PdfContentStreamParser(
            new PdfContentParseOptions(allowUnknownOperators: true));

        PdfGraphicsOperation operation = parser.Parse(Bytes("1 2 VendorOp"))[0];

        Assert.Equal("VendorOp", operation.Operator.Code);
        Assert.Equal(2, operation.Operands.Count);
    }

    [Fact]
    public void ParseRejectsUnbalancedGraphicsState()
    {
        PdfContentParseException exception = Assert.Throws<PdfContentParseException>(
            () => Parse("Q"));

        Assert.Equal(PdfContentParseFailure.UnbalancedGraphicsState, exception.Failure);
    }

    [Fact]
    public void ParsePreservesInlineImageDictionaryAndEncodedBytes()
    {
        IReadOnlyList<PdfGraphicsOperation> operations = Parse("BI /W 1 /H 1 /BPC 8 /CS /RGB ID abc EI q Q");

        PdfGraphicsOperation imageOperation = operations[0];
        Assert.Equal("BI", imageOperation.Operator.Code);
        Assert.NotNull(imageOperation.InlineImage);
        Assert.Equal(3, imageOperation.InlineImage.EncodedBytes.Length);
        Assert.Equal("abc", Encoding.ASCII.GetString(imageOperation.InlineImage.EncodedBytes.Span));
        Assert.Equal(4, imageOperation.InlineImage.Dictionary.Count);
        Assert.Equal("q", operations[1].Operator.Code);
    }

    [Fact]
    public void EnumerateMultipleStreamsRetainsStreamIndexes()
    {
        var parser = new PdfContentStreamParser();
        PdfGraphicsOperation[] operations = parser.Enumerate(
            new[] { Bytes("q Q"), Bytes("BT ET") }).ToArray();

        Assert.Equal(new[] { 0, 0, 1, 1 }, operations.Select(static value => value.SourceSpan.StreamIndex));
    }

    [Fact]
    public void ParseEnforcesOperationLimit()
    {
        var parser = new PdfContentStreamParser(
            new PdfContentParseOptions(maximumOperationCount: 2));

        PdfContentParseException exception = Assert.Throws<PdfContentParseException>(
            () => parser.Parse(Bytes("q Q BT ET")));

        Assert.Equal(PdfContentParseFailure.LimitExceeded, exception.Failure);
    }

    private static IReadOnlyList<PdfGraphicsOperation> Parse(string content) =>
        new PdfContentStreamParser().Parse(Bytes(content));

    private static ReadOnlyMemory<byte> Bytes(string content) =>
        Encoding.ASCII.GetBytes(content);
}
