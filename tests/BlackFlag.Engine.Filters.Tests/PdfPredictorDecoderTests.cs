using System.Buffers;

namespace BlackFlag.Engine.Filters.Tests;

public sealed class PdfPredictorDecoderTests
{
    [Fact]
    public void PngAveragePredictorDecodesRow()
    {
        ReadOnlyMemory<byte> result =
            DecodePredictor(
                [
                    3,
                    10,
                    15,
                    20
                ],
                predictor: 13,
                columns: 3);

        Assert.Equal(
            new byte[]
            {
                10,
                20,
                30
            },
            result.ToArray());
    }

    [Fact]
    public void PngPaethPredictorUsesLeftAndAboveNeighbors()
    {
        ReadOnlyMemory<byte> result =
            DecodePredictor(
                [
                    0,
                    10,
                    20,
                    30,
                    4,
                    1,
                    2,
                    3
                ],
                predictor: 15,
                columns: 3);

        Assert.Equal(
            new byte[]
            {
                10,
                20,
                30,
                11,
                22,
                33
            },
            result.ToArray());
    }

    [Fact]
    public void FixedPngPredictorRejectsMismatchedRowFilter()
    {
        PdfFilterDecodeException exception =
            Assert.Throws<PdfFilterDecodeException>(
                () => DecodePredictor(
                    [
                        0,
                        1,
                        2,
                        3
                    ],
                    predictor: 11,
                    columns: 3));

        Assert.Equal(
            PdfFilterDecodeFailure.InvalidData,
            exception.Failure);
    }

    private static ReadOnlyMemory<byte> DecodePredictor(
        byte[] data,
        int predictor,
        int columns)
    {
        var limits =
            new PdfFilterDecodeLimits(
                maximumDecodedBytes: 1024,
                maximumExpansionRatio: 1000,
                maximumFilterCount: 8,
                bufferSize: 16);

        var budget =
            new PdfFilterDecodeBudget(
                data.Length,
                limits);

        var writer =
            new ArrayBufferWriter<byte>();

        PdfPredictorDecoder.Decode(
            data,
            writer,
            new PdfFlateDecodeParameters(
                predictor,
                Colors: 1,
                BitsPerComponent: 8,
                Columns: columns),
            budget);

        return writer.WrittenMemory.ToArray();
    }
}

