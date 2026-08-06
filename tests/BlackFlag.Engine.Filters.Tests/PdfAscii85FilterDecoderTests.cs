using BlackFlag.Engine.Filters.Tests.Corpus;
using BlackFlag.Engine.Objects;

namespace BlackFlag.Engine.Filters.Tests;

public sealed class PdfAscii85FilterDecoderTests
{
    public static IEnumerable<object[]> ValidCases =>
        PdfTextFilterCorpus.Ascii85Valid.Select(
            value =>
                new object[]
                {
                    value
                });

    public static IEnumerable<object[]> InvalidCases =>
        PdfTextFilterCorpus.Ascii85Invalid.Select(
            value =>
                new object[]
                {
                    value
                });

    [Theory]
    [MemberData(nameof(ValidCases))]
    public void DecodeValidCorpus(
        object testCaseObject)
    {
        var testCase = (PdfTextFilterCorpusCase)testCaseObject;

        ReadOnlyMemory<byte> result =
            Decode(testCase.Encoded);

        Assert.Equal(
            testCase.Expected,
            result.ToArray());
    }

    [Theory]
    [MemberData(nameof(InvalidCases))]
    public void RejectInvalidCorpus(
        object testCaseObject)
    {
        var testCase = (PdfTextFilterCorpusCase)testCaseObject;

        PdfFilterDecodeException exception =
            Assert.Throws<PdfFilterDecodeException>(
                () => Decode(testCase.Encoded));

        Assert.Equal(
            testCase.ExpectedFailure,
            exception.Failure);
    }

    [Fact]
    public void DecodeSupportsMultipleZeroTuples()
    {
        ReadOnlyMemory<byte> result =
            Decode(
                "zzz~>"u8.ToArray());

        Assert.Equal(
            new byte[12],
            result.ToArray());
    }

    [Fact]
    public void DecodeHonorsDecodedByteLimit()
    {
        PdfFilterDecodeException exception =
            Assert.Throws<PdfFilterDecodeException>(
                () => Decode(
                    "zz~>"u8.ToArray(),
                    maximumDecodedBytes: 7));

        Assert.Equal(
            PdfFilterDecodeFailure.DecodedByteLimitExceeded,
            exception.Failure);
    }

    [Fact]
    public void DefaultRegistryContainsAscii85Decode()
    {
        IPdfFilterDecoder decoder =
            PdfFilterRegistrations.CreateDefault()
                .GetRequired(
                    new PdfName("ASCII85Decode"));

        Assert.IsType<PdfAscii85FilterDecoder>(
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
                new PdfName("ASCII85Decode")
            ],
            decodeParameters: null,
            new PdfFilterDecodeLimits(
                maximumDecodedBytes,
                maximumExpansionRatio: 100,
                maximumFilterCount: 8,
                bufferSize: 16));
    }
}
