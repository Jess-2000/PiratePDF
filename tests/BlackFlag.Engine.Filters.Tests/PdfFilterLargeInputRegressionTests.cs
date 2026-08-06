using BlackFlag.Engine.Objects;

namespace BlackFlag.Engine.Filters.Tests;

public sealed class PdfFilterLargeInputRegressionTests
{
    [Fact]
    public void AsciiHexDecodesLargeDeterministicInput()
    {
        byte[] original =
            Enumerable.Range(
                    0,
                    65536)
                .Select(
                    value =>
                        (byte)(
                            value &
                            0xFF))
                .ToArray();

        byte[] encoded =
            EncodeAsciiHex(original);

        ReadOnlyMemory<byte> result =
            Decode(
                encoded,
                "ASCIIHexDecode",
                maximumDecodedBytes: original.Length,
                maximumExpansionRatio: 2);

        Assert.Equal(
            original,
            result.ToArray());
    }

    [Fact]
    public void RunLengthDecodesMaximumRepeatedRuns()
    {
        const int RunCount = 512;

        byte[] encoded =
            new byte[
                checked(
                    (RunCount * 2) +
                    1)];

        int offset = 0;

        for (int index = 0; index < RunCount; index++)
        {
            encoded[offset++] = 129;
            encoded[offset++] =
                (byte)(
                    index &
                    0xFF);
        }

        encoded[offset] = 128;

        ReadOnlyMemory<byte> result =
            Decode(
                encoded,
                "RunLengthDecode",
                maximumDecodedBytes: RunCount * 128,
                maximumExpansionRatio: 100);

        Assert.Equal(
            RunCount * 128,
            result.Length);

        for (int run = 0; run < RunCount; run++)
        {
            ReadOnlySpan<byte> segment =
                result.Span.Slice(
                    run * 128,
                    128);

            Assert.All(
                segment.ToArray(),
                value =>
                    Assert.Equal(
                        (byte)(
                            run &
                            0xFF),
                        value));
        }
    }

    [Fact]
    public void LzwDecodesDictionaryWidthTransitions()
    {
        byte[] original =
            Enumerable.Range(
                    0,
                    1200)
                .Select(
                    value =>
                        (byte)(
                            value &
                            0xFF))
                .ToArray();

        byte[] encoded =
            PdfLzwTestEncoder.EncodeLiteralCodes(
                original);

        ReadOnlyMemory<byte> result =
            Decode(
                encoded,
                "LZWDecode",
                maximumDecodedBytes: original.Length,
                maximumExpansionRatio: 10);

        Assert.Equal(
            original,
            result.ToArray());
    }

    private static ReadOnlyMemory<byte> Decode(
        byte[] encoded,
        string filterName,
        long maximumDecodedBytes,
        double maximumExpansionRatio)
    {
        var pipeline =
            new PdfFilterPipeline(
                PdfFilterRegistrations.CreateDefault());

        return pipeline.Decode(
            encoded,
            [
                new PdfName(filterName)
            ],
            decodeParameters: null,
            new PdfFilterDecodeLimits(
                maximumDecodedBytes,
                maximumExpansionRatio,
                maximumFilterCount: 8,
                bufferSize: 4096));
    }

    private static byte[] EncodeAsciiHex(
        ReadOnlySpan<byte> data)
    {
        const string Digits = "0123456789ABCDEF";

        byte[] result =
            new byte[
                checked(
                    (data.Length * 2) +
                    1)];

        int offset = 0;

        foreach (byte value in data)
        {
            result[offset++] =
                (byte)Digits[value >> 4];

            result[offset++] =
                (byte)Digits[value & 0x0F];
        }

        result[offset] =
            (byte)'>';

        return result;
    }
}

