using BlackFlag.Engine.Filters.Tests.Corpus;
using BlackFlag.Engine.Objects;

namespace BlackFlag.Engine.Filters.Tests;

public sealed class PdfAsciiHexFilterDecoderTests
{
    public static IEnumerable<object[]> ValidCases =>
        PdfTextFilterCorpus.AsciiHexValid.Select(
            value =>
                new object[]
                {
                    value
                });

    public static IEnumerable<object[]> InvalidCases =>
        PdfTextFilterCorpus.AsciiHexInvalid.Select(
            value =>
                new object[]
                {
                    value
                });

    [Theory]
    [MemberData(nameof(ValidCases))]
    internal void DecodeValidCorpus(
        PdfTextFilterCorpusCase testCase)
    {
        ReadOnlyMemory<byte> result =
            Decode(testCase.Encoded);

        Assert.Equal(
            testCase.Expected,
            result.ToArray());
    }

    [Theory]
    [MemberData(nameof(InvalidCases))]
    internal void RejectInvalidCorpus(
        PdfTextFilterCorpusCase testCase)
    {
        PdfFilterDecodeException exception =
            Assert.Throws<PdfFilterDecodeException>(
                () => Decode(testCase.Encoded));

        Assert.Equal(
            testCase.ExpectedFailure,
            exception.Failure);
    }

    [Fact]
    public void DecodeHonorsDecodedByteLimit()
    {
        PdfFilterDecodeException exception =
            Assert.Throws<PdfFilterDecodeException>(
                () => Decode(
                    "000102>"u8.ToArray(),
                    maximumDecodedBytes: 2));

        Assert.Equal(
            PdfFilterDecodeFailure.DecodedByteLimitExceeded,
            exception.Failure);
    }

    [Fact]
    public void DefaultRegistryContainsAsciiHexDecode()
    {
        IPdfFilterDecoder decoder =
            PdfFilterRegistrations.CreateDefault()
                .GetRequired(
                    new PdfName("ASCIIHexDecode"));

        Assert.IsType<PdfAsciiHexFilterDecoder>(
            decoder);
    }

    private static ReadOnlyMemory<byte> Decode(
        byte[] encoded,
        long maximumDecodedBytes = 1024)
    {
        var pipeline =
            new PdfFilterPipeline(
                PdfFilterRegistrations.CreateDefault());

        return pipeline.Decode(
            encoded,
            [
                new PdfName("ASCIIHexDecode")
            ],
            decodeParameters: null,
            new PdfFilterDecodeLimits(
                maximumDecodedBytes,
                maximumExpansionRatio: 100,
                maximumFilterCount: 8,
                bufferSize: 16));
    }
}
