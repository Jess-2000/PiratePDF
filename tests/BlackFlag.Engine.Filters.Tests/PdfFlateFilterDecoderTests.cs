using BlackFlag.Engine.Objects;

namespace BlackFlag.Engine.Filters.Tests;

public sealed class PdfFlateFilterDecoderTests
{
    [Fact]
    public void DecodeRestoresZlibData()
    {
        byte[] original =
            "BlackFlag FlateDecode"u8.ToArray();

        ReadOnlyMemory<byte> result =
            Decode(
                PdfFlateTestData.Compress(original));

        Assert.Equal(
            original,
            result.ToArray());
    }

    [Fact]
    public void DecodeHandlesBinaryData()
    {
        byte[] original =
            Enumerable.Range(
                    0,
                    4096)
                .Select(
                    value =>
                        (byte)(
                            value %
                            256))
                .ToArray();

        ReadOnlyMemory<byte> result =
            Decode(
                PdfFlateTestData.Compress(original));

        Assert.Equal(
            original,
            result.ToArray());
    }

    [Fact]
    public void DecodeRejectsMalformedZlibData()
    {
        PdfFilterDecodeException exception =
            Assert.Throws<PdfFilterDecodeException>(
                () => Decode(
                    [1, 2, 3, 4, 5]));

        Assert.Equal(
            PdfFilterDecodeFailure.InvalidData,
            exception.Failure);
    }

    [Fact]
    public void DecodeEnforcesDecodedByteLimit()
    {
        byte[] original =
            new byte[1024];

        PdfFilterDecodeException exception =
            Assert.Throws<PdfFilterDecodeException>(
                () => Decode(
                    PdfFlateTestData.Compress(original),
                    maximumDecodedBytes: 100,
                    maximumExpansionRatio: 1000));

        Assert.Equal(
            PdfFilterDecodeFailure.DecodedByteLimitExceeded,
            exception.Failure);
    }

    [Fact]
    public void DecodeEnforcesExpansionRatio()
    {
        byte[] original =
            new byte[4096];

        PdfFilterDecodeException exception =
            Assert.Throws<PdfFilterDecodeException>(
                () => Decode(
                    PdfFlateTestData.Compress(original),
                    maximumDecodedBytes: 8192,
                    maximumExpansionRatio: 2));

        Assert.Equal(
            PdfFilterDecodeFailure.ExpansionRatioExceeded,
            exception.Failure);
    }

    [Fact]
    public void DecodeSupportsTiffPredictorEightBit()
    {
        byte[] predicted =
        [
            10,
            10,
            10,
            10
        ];

        PdfDictionary parameters =
            CreateParameters(
                predictor: 2,
                colors: 1,
                bitsPerComponent: 8,
                columns: 4);

        ReadOnlyMemory<byte> result =
            Decode(
                PdfFlateTestData.Compress(predicted),
                parameters);

        Assert.Equal(
            new byte[]
            {
                10,
                20,
                30,
                40
            },
            result.ToArray());
    }

    [Fact]
    public void DecodeSupportsPngSubPredictor()
    {
        byte[] predicted =
        [
            1,
            10,
            10,
            10,
            10
        ];

        PdfDictionary parameters =
            CreateParameters(
                predictor: 11,
                colors: 1,
                bitsPerComponent: 8,
                columns: 4);

        ReadOnlyMemory<byte> result =
            Decode(
                PdfFlateTestData.Compress(predicted),
                parameters);

        Assert.Equal(
            new byte[]
            {
                10,
                20,
                30,
                40
            },
            result.ToArray());
    }

    [Fact]
    public void DecodeSupportsPngUpAcrossRows()
    {
        byte[] predicted =
        [
            2,
            10,
            20,
            30,
            2,
            1,
            2,
            3
        ];

        PdfDictionary parameters =
            CreateParameters(
                predictor: 12,
                colors: 1,
                bitsPerComponent: 8,
                columns: 3);

        ReadOnlyMemory<byte> result =
            Decode(
                PdfFlateTestData.Compress(predicted),
                parameters);

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
    public void DecodeSupportsPngOptimumRows()
    {
        byte[] predicted =
        [
            0,
            1,
            2,
            3,
            1,
            4,
            1,
            1
        ];

        PdfDictionary parameters =
            CreateParameters(
                predictor: 15,
                colors: 1,
                bitsPerComponent: 8,
                columns: 3);

        ReadOnlyMemory<byte> result =
            Decode(
                PdfFlateTestData.Compress(predicted),
                parameters);

        Assert.Equal(
            new byte[]
            {
                1,
                2,
                3,
                4,
                5,
                6
            },
            result.ToArray());
    }

    [Fact]
    public void DecodeRejectsIncompletePredictorRow()
    {
        byte[] predicted =
        [
            1,
            10,
            20
        ];

        PdfDictionary parameters =
            CreateParameters(
                predictor: 11,
                colors: 1,
                bitsPerComponent: 8,
                columns: 3);

        PdfFilterDecodeException exception =
            Assert.Throws<PdfFilterDecodeException>(
                () => Decode(
                    PdfFlateTestData.Compress(predicted),
                    parameters));

        Assert.Equal(
            PdfFilterDecodeFailure.InvalidData,
            exception.Failure);
    }

    [Fact]
    public void DecodeRejectsUnsupportedPredictor()
    {
        PdfDictionary parameters =
            CreateParameters(
                predictor: 9,
                colors: 1,
                bitsPerComponent: 8,
                columns: 1);

        PdfFilterDecodeException exception =
            Assert.Throws<PdfFilterDecodeException>(
                () => Decode(
                    PdfFlateTestData.Compress([1]),
                    parameters));

        Assert.Equal(
            PdfFilterDecodeFailure.InvalidData,
            exception.Failure);
    }

    [Fact]
    public void DefaultRegistryContainsFlateDecode()
    {
        PdfFilterRegistry registry =
            PdfFilterRegistrations.CreateDefault();

        IPdfFilterDecoder decoder =
            registry.GetRequired(
                new PdfName("FlateDecode"));

        Assert.IsType<PdfFlateFilterDecoder>(
            decoder);
    }

    private static ReadOnlyMemory<byte> Decode(
        byte[] encodedData,
        PdfDictionary? parameters = null,
        long maximumDecodedBytes = 1024 * 1024,
        double maximumExpansionRatio = 1000)
    {
        var pipeline =
            new PdfFilterPipeline(
                PdfFilterRegistrations.CreateDefault());

        return pipeline.Decode(
            encodedData,
            [
                new PdfName("FlateDecode")
            ],
            [
                parameters
            ],
            new PdfFilterDecodeLimits(
                maximumDecodedBytes,
                maximumExpansionRatio,
                maximumFilterCount: 8,
                bufferSize: 256));
    }

    private static PdfDictionary CreateParameters(
        int predictor,
        int colors,
        int bitsPerComponent,
        int columns)
    {
        return new PdfDictionary(
            [
                new KeyValuePair<PdfName, PdfObject>(
                    new PdfName("Predictor"),
                    new PdfInteger(predictor)),
                new KeyValuePair<PdfName, PdfObject>(
                    new PdfName("Colors"),
                    new PdfInteger(colors)),
                new KeyValuePair<PdfName, PdfObject>(
                    new PdfName("BitsPerComponent"),
                    new PdfInteger(bitsPerComponent)),
                new KeyValuePair<PdfName, PdfObject>(
                    new PdfName("Columns"),
                    new PdfInteger(columns))
            ]);
    }
}

