using System.Buffers;
using System.IO.Compression;
using BlackFlag.Engine.Objects;

namespace BlackFlag.Engine.Filters.Tests;

public sealed class PdfFlateCompletenessTests
{
    [Theory]
    [InlineData("")]
    [InlineData("78")]
    [InlineData("789C")]
    [InlineData("7801")]
    [InlineData("78DA")]
    [InlineData("0000")]
    [InlineData("0102030405")]
    [InlineData("010600F9FF414243444546")]
    public void IncompleteHeadersAndMalformedDataAreRejected(string hex)
    {
        PdfFilterDecodeException exception = Assert.Throws<PdfFilterDecodeException>(
            () => Decode(Convert.FromHexString(hex)));
        Assert.Equal(PdfFilterDecodeFailure.InvalidData, exception.Failure);
    }

    [Theory]
    [InlineData(CompressionLevel.NoCompression)]
    [InlineData(CompressionLevel.Fastest)]
    [InlineData(CompressionLevel.SmallestSize)]
    public void EveryProperPrefixOfCompleteMemberIsRejected(CompressionLevel level)
    {
        byte[] payload = "ABCDEF"u8.ToArray();
        byte[] complete = Compress(payload, level);
        Assert.Equal(payload, Decode(complete).ToArray());
        for (int retained = 1; retained < complete.Length; retained++)
        {
            var exception = Assert.Throws<PdfFilterDecodeException>(() => Decode(complete[..retained]));
            AssertInvalidZlib(exception);
        }
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(13)]
    public void CompleteFirstMemberRetainsTrailingDataAcceptance(int trailingBytes)
    {
        byte[] complete = Compress("ABCDEF"u8);
        byte[] trailing = trailingBytes == 13
            ? Compress("GH"u8)
            : Enumerable.Repeat((byte)0xFF, trailingBytes).ToArray();
        Assert.Equal("ABCDEF"u8.ToArray(), Decode([.. complete, .. trailing]).ToArray());
    }

    [Fact]
    public void CompleteEmptyMemberRemainsValid()
    {
        byte[] complete = Convert.FromHexString("789C030000000001");
        Assert.Empty(Decode(complete).ToArray());
        for (int retained = 1; retained < complete.Length; retained++)
        {
            AssertInvalidZlib(Assert.Throws<PdfFilterDecodeException>(() => Decode(complete[..retained])));
        }
    }

    [Fact]
    public void MissingFinalBlockAndIncorrectChecksumAreRejected()
    {
        byte[] complete = Compress("ABCDEF"u8);
        byte[] noFinalBlock = complete[..^4];
        noFinalBlock[2] &= 0xFE;
        AssertInvalidZlib(Assert.Throws<PdfFilterDecodeException>(() => Decode(noFinalBlock)));
        complete[^1] ^= 0xFF;
        AssertInvalidZlib(Assert.Throws<PdfFilterDecodeException>(() => Decode(complete)));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(4)]
    [InlineData(7)]
    public void IncompleteCompressionFailsBeforePredictorProcessing(int missing)
    {
        byte[] complete = Compress([1, 10, 10, 10, 10]);
        var parameters = PredictorParameters();
        Assert.Equal(new byte[] { 10, 20, 30, 40 }, Decode(complete, parameters).ToArray());
        AssertInvalidZlib(Assert.Throws<PdfFilterDecodeException>(
            () => Decode(complete[..^missing], parameters)));
    }

    [Fact]
    public void CompleteCompressionStillRequiresCompletePredictorRows()
    {
        var exception = Assert.Throws<PdfFilterDecodeException>(
            () => Decode(Compress([1, 10]), PredictorParameters()));
        Assert.Equal(PdfFilterDecodeFailure.InvalidData, exception.Failure);
        Assert.Equal("PNG predictor data does not contain complete prefixed rows.", exception.Message);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ByteAndRatioBoundsStillApplyBeforeOutputGrowth(bool truncated)
    {
        byte[] payload = new byte[128];
        byte[] complete = Compress(payload, CompressionLevel.SmallestSize);
        byte[] encoded = truncated ? complete[..^1] : complete;
        double ratio = (double)payload.Length / encoded.Length;
        var exact = new PdfFilterDecodeLimits(payload.Length, ratio, 1, 16);
        var tooSmall = new PdfFilterDecodeLimits(payload.Length - 1, ratio, 1, 16);
        var lowRatio = new PdfFilterDecodeLimits(payload.Length, Math.BitDecrement(ratio), 1, 16);
        if (truncated)
            AssertInvalidZlib(Assert.Throws<PdfFilterDecodeException>(() => Decode(encoded, limits: exact)));
        else
            Assert.Equal(payload, Decode(encoded, limits: exact).ToArray());

        Assert.Equal(PdfFilterDecodeFailure.DecodedByteLimitExceeded,
            Assert.Throws<PdfFilterDecodeException>(() => Decode(encoded, limits: tooSmall)).Failure);
        Assert.Equal(PdfFilterDecodeFailure.ExpansionRatioExceeded,
            Assert.Throws<PdfFilterDecodeException>(() => Decode(encoded, limits: lowRatio)).Failure);
    }

    [Fact]
    public void PipelinePreCancellationKeepsOriginalTokenAndException()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var exception = Assert.Throws<OperationCanceledException>(() =>
            new PdfFilterPipeline(PdfFilterRegistrations.CreateDefault()).Decode(
                new byte[] { 0x78 }, [new PdfName("FlateDecode")], null,
                new PdfFilterDecodeLimits(), cancellation.Token));
        Assert.Equal(cancellation.Token, exception.CancellationToken);
    }

    [Fact]
    public void DirectDecoderReportsFailureAfterWritingProvisionalOutput()
    {
        byte[] encoded = Compress("ABCDEF"u8)[..^1];
        var destination = new ArrayBufferWriter<byte>();
        var context = new PdfFilterDecodeContext(new PdfFilterDecodeBudget(encoded.Length, new()), null, 0);
        var exception = Assert.Throws<PdfFilterDecodeException>(() =>
            new PdfFlateFilterDecoder().Decode(encoded, destination, context));
        AssertInvalidZlib(exception);
        // The low-level writer API is streaming, not transactional. Its caller
        // must discard provisional output on failure; the pipeline does so.
        Assert.Equal("ABCDEF"u8.ToArray(), destination.WrittenSpan.ToArray());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void CancellationAfterOutputPrecedesNextRead(bool truncated)
    {
        byte[] complete = Compress("ABCDEF"u8);
        byte[] encoded = truncated ? complete[..^1] : complete;
        using var cancellation = new CancellationTokenSource();
        var budget = new PdfFilterDecodeBudget(encoded.Length, new(), cancellation.Token);
        var destination = new CancellingWriter(cancellation);
        var exception = Assert.Throws<PdfFilterDecodeException>(() =>
            new PdfFlateFilterDecoder().Decode(encoded, destination, new(budget, null, 0)));
        Assert.Equal(PdfFilterDecodeFailure.Cancelled, exception.Failure);
        Assert.Equal(cancellation.Token, Assert.IsType<OperationCanceledException>(exception.InnerException).CancellationToken);
        Assert.Equal(6, destination.WrittenCount);
    }

    private sealed class CancellingWriter(CancellationTokenSource cancellation) : IBufferWriter<byte>
    {
        private readonly ArrayBufferWriter<byte> buffer = new();
        public int WrittenCount => buffer.WrittenCount;
        public void Advance(int count)
        {
            buffer.Advance(count);
            cancellation.Cancel();
        }
        public Memory<byte> GetMemory(int sizeHint = 0) => buffer.GetMemory(sizeHint);
        public Span<byte> GetSpan(int sizeHint = 0) => buffer.GetSpan(sizeHint);
    }

    private static PdfDictionary PredictorParameters() => new(new Dictionary<PdfName, PdfObject>
    {
        [new("Predictor")] = new PdfInteger(11),
        [new("Colors")] = new PdfInteger(1),
        [new("BitsPerComponent")] = new PdfInteger(8),
        [new("Columns")] = new PdfInteger(4)
    });

    private static ReadOnlyMemory<byte> Decode(
        byte[] encoded, PdfDictionary? parameters = null, PdfFilterDecodeLimits? limits = null) =>
        new PdfFilterPipeline(PdfFilterRegistrations.CreateDefault()).Decode(
            encoded, [new PdfName("FlateDecode")], [parameters], limits ?? new());

    private static void AssertInvalidZlib(PdfFilterDecodeException exception)
    {
        Assert.Equal(PdfFilterDecodeFailure.InvalidData, exception.Failure);
        Assert.Equal("The FlateDecode stream contains invalid or incomplete zlib data.", exception.Message);
        Assert.IsType<InvalidDataException>(exception.InnerException);
    }

    private static byte[] Compress(ReadOnlySpan<byte> bytes, CompressionLevel level = CompressionLevel.NoCompression)
    {
        using var output = new MemoryStream();
        using (var zlib = new ZLibStream(output, level, leaveOpen: true)) zlib.Write(bytes);
        return output.ToArray();
    }
}
