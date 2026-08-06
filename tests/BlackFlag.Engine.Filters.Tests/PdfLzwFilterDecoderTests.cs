using BlackFlag.Engine.Objects;

namespace BlackFlag.Engine.Filters.Tests;

public sealed class PdfLzwFilterDecoderTests
{
    [Fact]
    public void DecodeRestoresLiteralCodes()
    {
        byte[] original =
            "BlackFlag LZW"u8.ToArray();

        ReadOnlyMemory<byte> result =
            Decode(
                PdfLzwTestEncoder.EncodeLiteralCodes(
                    original));

        Assert.Equal(
            original,
            result.ToArray());
    }

    [Fact]
    public void DecodeSupportsClearCodeReset()
    {
        byte[] encoded =
            PdfLzwTestEncoder.PackCodes(
                [
                    256,
                    (byte)'A',
                    (byte)'B',
                    256,
                    (byte)'C',
                    257
                ]);

        ReadOnlyMemory<byte> result =
            Decode(encoded);

        Assert.Equal(
            "ABC"u8.ToArray(),
            result.ToArray());
    }

    [Fact]
    public void DecodeSupportsKwKwKCase()
    {
        byte[] encoded =
            PdfLzwTestEncoder.PackCodes(
                [
                    256,
                    (byte)'A',
                    258,
                    257
                ]);

        ReadOnlyMemory<byte> result =
            Decode(encoded);

        Assert.Equal(
            "AAA"u8.ToArray(),
            result.ToArray());
    }

    [Fact]
    public void DecodeRejectsInvalidCode()
    {
        byte[] encoded =
            PdfLzwTestEncoder.PackCodes(
                [
                    256,
                    300,
                    257
                ]);

        PdfFilterDecodeException exception =
            Assert.Throws<PdfFilterDecodeException>(
                () => Decode(encoded));

        Assert.Equal(
            PdfFilterDecodeFailure.InvalidData,
            exception.Failure);
    }

    [Fact]
    public void DecodeRejectsMissingEndOfDataCode()
    {
        byte[] encoded =
            PdfLzwTestEncoder.PackCodes(
                [
                    256,
                    (byte)'A'
                ]);

        PdfFilterDecodeException exception =
            Assert.Throws<PdfFilterDecodeException>(
                () => Decode(encoded));

        Assert.Equal(
            PdfFilterDecodeFailure.InvalidData,
            exception.Failure);
    }

    [Fact]
    public void DecodeHonorsDecodedByteLimit()
    {
        byte[] encoded =
            PdfLzwTestEncoder.EncodeLiteralCodes(
                new byte[32]);

        PdfFilterDecodeException exception =
            Assert.Throws<PdfFilterDecodeException>(
                () => Decode(
                    encoded,
                    maximumDecodedBytes: 16,
                    maximumExpansionRatio: 100));

        Assert.Equal(
            PdfFilterDecodeFailure.DecodedByteLimitExceeded,
            exception.Failure);
    }

    [Fact]
    public void DecodeSupportsEarlyChangeZero()
    {
        byte[] original =
            Enumerable.Range(
                    0,
                    300)
                .Select(
                    value =>
                        (byte)(
                            value &
                            0xFF))
                .ToArray();

        byte[] encoded =
            PdfLzwTestEncoder.EncodeLiteralCodes(
                original,
                earlyChange: 0);

        PdfDictionary parameters =
            new(
                [
                    new KeyValuePair<PdfName, PdfObject>(
                        new PdfName("EarlyChange"),
                        new PdfInteger(0))
                ]);

        ReadOnlyMemory<byte> result =
            Decode(
                encoded,
                parameters: parameters);

        Assert.Equal(
            original,
            result.ToArray());
    }

    [Fact]
    public void DecodeAppliesPngPredictor()
    {
        byte[] predicted =
        [
            1,
            10,
            10,
            10
        ];

        byte[] encoded =
            PdfLzwTestEncoder.EncodeLiteralCodes(
                predicted);

        PdfDictionary parameters =
            new(
                [
                    new KeyValuePair<PdfName, PdfObject>(
                        new PdfName("Predictor"),
                        new PdfInteger(11)),
                    new KeyValuePair<PdfName, PdfObject>(
                        new PdfName("Colors"),
                        new PdfInteger(1)),
                    new KeyValuePair<PdfName, PdfObject>(
                        new PdfName("BitsPerComponent"),
                        new PdfInteger(8)),
                    new KeyValuePair<PdfName, PdfObject>(
                        new PdfName("Columns"),
                        new PdfInteger(3))
                ]);

        ReadOnlyMemory<byte> result =
            Decode(
                encoded,
                parameters: parameters);

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
    public void DefaultRegistryContainsLzwDecode()
    {
        IPdfFilterDecoder decoder =
            PdfFilterRegistrations.CreateDefault()
                .GetRequired(
                    new PdfName("LZWDecode"));

        Assert.IsType<PdfLzwFilterDecoder>(
            decoder);
    }

    private static ReadOnlyMemory<byte> Decode(
        byte[] encoded,
        PdfDictionary? parameters = null,
        long maximumDecodedBytes = 1024 * 1024,
        double maximumExpansionRatio = 100)
    {
        var pipeline =
            new PdfFilterPipeline(
                PdfFilterRegistrations.CreateDefault());

        return pipeline.Decode(
            encoded,
            [
                new PdfName("LZWDecode")
            ],
            [
                parameters
            ],
            new PdfFilterDecodeLimits(
                maximumDecodedBytes,
                maximumExpansionRatio,
                maximumFilterCount: 8,
                bufferSize: 64));
    }
}
