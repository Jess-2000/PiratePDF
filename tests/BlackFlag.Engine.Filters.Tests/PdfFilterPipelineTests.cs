using System.Buffers;
using BlackFlag.Engine.Objects;

namespace BlackFlag.Engine.Filters.Tests;

public sealed class PdfFilterPipelineTests
{
    [Fact]
    public void DecodeWithoutFiltersReturnsOwnedSnapshot()
    {
        byte[] encodedData =
            "unchanged"u8.ToArray();

        var pipeline =
            new PdfFilterPipeline(
                new PdfFilterRegistry());

        ReadOnlyMemory<byte> result =
            pipeline.Decode(
                encodedData,
                [],
                decodeParameters: null,
                CreateLimits());

        encodedData[0] =
            (byte)'X';

        Assert.Equal(
            "unchanged"u8.ToArray(),
            result.ToArray());
    }

    [Fact]
    public void DecodeAppliesFiltersInDeclarationOrder()
    {
        var appendA =
            new TestFilterDecoder(
                "AppendA",
                static (input, output, _) =>
                {
                    TestFilterDecoder.Write(output, input);
                    TestFilterDecoder.Write(output, "A"u8);
                });

        var appendB =
            new TestFilterDecoder(
                "AppendB",
                static (input, output, _) =>
                {
                    TestFilterDecoder.Write(output, input);
                    TestFilterDecoder.Write(output, "B"u8);
                });

        var pipeline =
            new PdfFilterPipeline(
                new PdfFilterRegistry(
                    [appendA, appendB]));

        ReadOnlyMemory<byte> result =
            pipeline.Decode(
                "X"u8.ToArray(),
                [
                    new PdfName("AppendA"),
                    new PdfName("AppendB")
                ],
                decodeParameters: null,
                CreateLimits());

        Assert.Equal(
            "XAB"u8.ToArray(),
            result.ToArray());
    }

    [Fact]
    public void DecodeSuppliesAlignedParametersAndIndexes()
    {
        var firstParameters =
            new PdfDictionary([]);

        var secondParameters =
            new PdfDictionary([]);

        var seen =
            new List<(int Index, PdfDictionary? Parameters)>();

        var first =
            new TestFilterDecoder(
                "First",
                (input, output, context) =>
                {
                    seen.Add(
                        (context.FilterIndex, context.Parameters));

                    TestFilterDecoder.Write(output, input);
                });

        var second =
            new TestFilterDecoder(
                "Second",
                (input, output, context) =>
                {
                    seen.Add(
                        (context.FilterIndex, context.Parameters));

                    TestFilterDecoder.Write(output, input);
                });

        var pipeline =
            new PdfFilterPipeline(
                new PdfFilterRegistry(
                    [first, second]));

        pipeline.Decode(
            "data"u8.ToArray(),
            [
                new PdfName("First"),
                new PdfName("Second")
            ],
            [
                firstParameters,
                secondParameters
            ],
            CreateLimits());

        Assert.Equal(2, seen.Count);
        Assert.Equal(
            (0, firstParameters),
            seen[0]);
        Assert.Equal(
            (1, secondParameters),
            seen[1]);
    }

    [Fact]
    public void DecodeRejectsMismatchedParameterCount()
    {
        var pipeline =
            new PdfFilterPipeline(
                new PdfFilterRegistry());

        Assert.Throws<ArgumentException>(
            () => pipeline.Decode(
                "data"u8.ToArray(),
                [new PdfName("Example")],
                [],
                CreateLimits()));
    }

    [Fact]
    public void DecodeRejectsExcessiveFilterCount()
    {
        var pipeline =
            new PdfFilterPipeline(
                new PdfFilterRegistry());

        PdfFilterDecodeException exception =
            Assert.Throws<PdfFilterDecodeException>(
                () => pipeline.Decode(
                    "data"u8.ToArray(),
                    [
                        new PdfName("One"),
                        new PdfName("Two")
                    ],
                    decodeParameters: null,
                    new PdfFilterDecodeLimits(
                        maximumDecodedBytes: 1024,
                        maximumExpansionRatio: 100,
                        maximumFilterCount: 1,
                        bufferSize: 16)));

        Assert.Equal(
            PdfFilterDecodeFailure.FilterCountLimitExceeded,
            exception.Failure);
    }

    [Fact]
    public void DecodeEnforcesDecodedByteLimitThroughWriter()
    {
        var expanding =
            new TestFilterDecoder(
                "Expand",
                static (_, output, _) =>
                    TestFilterDecoder.Write(
                        output,
                        new byte[17]));

        var pipeline =
            new PdfFilterPipeline(
                new PdfFilterRegistry(
                    [expanding]));

        PdfFilterDecodeException exception =
            Assert.Throws<PdfFilterDecodeException>(
                () => pipeline.Decode(
                    "data"u8.ToArray(),
                    [new PdfName("Expand")],
                    decodeParameters: null,
                    new PdfFilterDecodeLimits(
                        maximumDecodedBytes: 16,
                        maximumExpansionRatio: 100,
                        maximumFilterCount: 8,
                        bufferSize: 8)));

        Assert.Equal(
            PdfFilterDecodeFailure.DecodedByteLimitExceeded,
            exception.Failure);
    }

    [Fact]
    public void DecodeWrapsInvalidDataFailure()
    {
        var invalid =
            new TestFilterDecoder(
                "Invalid",
                static (_, _, _) =>
                    throw new InvalidDataException(
                        "Malformed test input."));

        var pipeline =
            new PdfFilterPipeline(
                new PdfFilterRegistry(
                    [invalid]));

        PdfFilterDecodeException exception =
            Assert.Throws<PdfFilterDecodeException>(
                () => pipeline.Decode(
                    "data"u8.ToArray(),
                    [new PdfName("Invalid")],
                    decodeParameters: null,
                    CreateLimits()));

        Assert.Equal(
            PdfFilterDecodeFailure.InvalidData,
            exception.Failure);
    }

    [Fact]
    public void DecodeHonorsCancellationBetweenStages()
    {
        using var cancellation =
            new CancellationTokenSource();

        var cancelling =
            new TestFilterDecoder(
                "Cancel",
                (input, output, _) =>
                {
                    TestFilterDecoder.Write(output, input);
                    cancellation.Cancel();
                });

        var second =
            new TestFilterDecoder(
                "Second",
                static (input, output, _) =>
                    TestFilterDecoder.Write(output, input));

        var pipeline =
            new PdfFilterPipeline(
                new PdfFilterRegistry(
                    [cancelling, second]));

        PdfFilterDecodeException exception =
            Assert.Throws<PdfFilterDecodeException>(
                () => pipeline.Decode(
                    "data"u8.ToArray(),
                    [
                        new PdfName("Cancel"),
                        new PdfName("Second")
                    ],
                    decodeParameters: null,
                    CreateLimits(),
                    cancellation.Token));

        Assert.Equal(
            PdfFilterDecodeFailure.Cancelled,
            exception.Failure);
    }

    private static PdfFilterDecodeLimits CreateLimits()
    {
        return new PdfFilterDecodeLimits(
            maximumDecodedBytes: 1024,
            maximumExpansionRatio: 100,
            maximumFilterCount: 8,
            bufferSize: 16);
    }
}

