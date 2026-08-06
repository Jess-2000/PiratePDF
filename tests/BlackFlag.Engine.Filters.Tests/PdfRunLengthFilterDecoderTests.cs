using BlackFlag.Engine.Filters.Tests.Corpus;
using BlackFlag.Engine.Objects;

namespace BlackFlag.Engine.Filters.Tests;

public sealed class PdfRunLengthFilterDecoderTests
{
    public static IEnumerable<object[]> ValidCases =>
        PdfBinaryFilterCorpus.RunLengthValid.Select(
            value =>
                new object[]
                {
                    value
                });

    public static IEnumerable<object[]> InvalidCases =>
        PdfBinaryFilterCorpus.RunLengthInvalid.Select(
            value =>
                new object[]
                {
                    value
                });

    [Theory]
    [MemberData(nameof(ValidCases))]
    public void DecodeValidCorpus(
        PdfBinaryFilterCorpusCase testCase)
    {
        ReadOnlyMemory<byte> result =
            Decode(testCase.Encoded);

        Assert.Equal(
            testCase.Expected,
            result.ToArray());
    }

    [Theory]
    [MemberData(nameof(InvalidCases))]
    public void RejectInvalidCorpus(
        PdfBinaryFilterCorpusCase testCase)
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
                    [249, 0, 128],
                    maximumDecodedBytes: 7));

        Assert.Equal(
            PdfFilterDecodeFailure.DecodedByteLimitExceeded,
            exception.Failure);
    }

    [Fact]
    public void DefaultRegistryContainsRunLengthDecode()
    {
        IPdfFilterDecoder decoder =
            PdfFilterRegistrations.CreateDefault()
                .GetRequired(
                    new PdfName("RunLengthDecode"));

        Assert.IsType<PdfRunLengthFilterDecoder>(
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
                new PdfName("RunLengthDecode")
            ],
            decodeParameters: null,
            new PdfFilterDecodeLimits(
                maximumDecodedBytes,
                maximumExpansionRatio: 100,
                maximumFilterCount: 8,
                bufferSize: 16));
    }
}

