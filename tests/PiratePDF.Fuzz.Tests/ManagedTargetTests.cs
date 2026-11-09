using System.Buffers.Binary;
using System.IO.Compression;
using System.Threading.Tasks;
using BlackFlag.Engine.Fonts.Sanitization;
using BlackFlag.Engine.Fonts.Tests;

namespace PiratePDF.Fuzz.Tests;

public sealed class ManagedTargetTests
{
    [Theory]
    [InlineData("filter-asciihex")]
    [InlineData("filter-flate")]
    public void ConcurrentFilterCallsKeepBudgetsAndCancellationIndependent(string id)
    {
        byte[] decoded = new byte[8191];
        new Random(1049).NextBytes(decoded);
        byte[] encoded = id == "filter-asciihex"
            ? System.Text.Encoding.ASCII.GetBytes(Convert.ToHexString(decoded) + ">")
            : Compress(decoded);
        byte[] snapshot = encoded.ToArray();
        FuzzResult expected = FuzzResult.Success(decoded);
        FuzzTarget target = TargetRegistry.Get(id);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        Parallel.For(0, 64, _ =>
        {
            Assert.Equal(expected, target.Run(encoded));
            Assert.Throws<OperationCanceledException>(() => target.Run(encoded, cancellation.Token));
            Assert.Equal(FuzzResult.Rejected("InvalidData"), target.Run("G>"u8));
            Assert.Equal(expected, target.Run(encoded));
        });
        Assert.Equal(snapshot, encoded);
    }

    [Theory]
    [InlineData("filter-asciihex")]
    [InlineData("filter-flate")]
    [InlineData("font-sanitize")]
    public void RegisteredTargetsEnforceInputAndPreCancellation(string id)
    {
        FuzzTarget target = TargetRegistry.Get(id);
        Assert.Throws<ArgumentOutOfRangeException>(() => target.Run(new byte[target.MaximumInputBytes + 1]));
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        OperationCanceledException error = Assert.Throws<OperationCanceledException>(() => target.Run([], cancellation.Token));
        Assert.Equal(cancellation.Token, error.CancellationToken);
    }

    [Fact]
    public void AsciiHexDecodesDeterministicallyWithoutChangingInput()
    {
        byte[] encoded = "48 65 6c6c6f>"u8.ToArray();
        FuzzTarget target = TargetRegistry.Get("filter-asciihex");
        FuzzResult first = target.Run(encoded);
        Assert.Equal(FuzzResult.Success("Hello"u8), first);
        Assert.Equal(first, target.Run(encoded));
        Assert.Equal("48 65 6c6c6f>"u8.ToArray(), encoded);
    }

    [Fact]
    public void AsciiHexAcceptsExactInputLimitAndRejectsMalformedData()
    {
        FuzzTarget target = TargetRegistry.Get("filter-asciihex");
        byte[] exact = Enumerable.Repeat((byte)' ', target.MaximumInputBytes).ToArray();
        exact[^1] = (byte)'>';
        Assert.Equal(FuzzResult.Success([]), target.Run(exact));
        Assert.Equal(FuzzResult.Rejected("InvalidData"), target.Run("G>"u8));
        Assert.Equal(FuzzResult.Rejected("InvalidData"), target.Run("41"u8));
    }

    [Fact]
    public void FlateDecodesOwnedInputAndRejectsTruncation()
    {
        byte[] encoded = Compress("Hello"u8);
        byte[] snapshot = encoded.ToArray();
        FuzzTarget target = TargetRegistry.Get("filter-flate");
        Assert.Equal(FuzzResult.Success("Hello"u8), target.Run(encoded));
        Assert.Equal(target.Run(encoded), target.Run(encoded));
        Assert.Equal(snapshot, encoded);
        Assert.Equal(FuzzResult.Rejected("InvalidData"), target.Run(encoded.AsSpan(0, encoded.Length - 1)));
        Assert.Equal(FuzzResult.Rejected("InvalidData"), target.Run([1, 2, 3, 4, 5]));
    }

    [Fact]
    public void FlateUsesDecodedByteLimitAndExpansionRatio()
    {
        byte[] block = new byte[4096];
        new Random(1049).NextBytes(block);
        byte[] decoded = new byte[32769];
        for (int index = 0; index < decoded.Length; index++) decoded[index] = block[index % block.Length];
        FuzzTarget target = TargetRegistry.Get("filter-flate");
        byte[] exact = Compress(decoded.AsSpan(0, 32768));
        Assert.InRange(exact.Length, 1024, target.MaximumInputBytes);
        Assert.Equal(FuzzResult.Success(decoded.AsSpan(0, 32768)), target.Run(exact));
        Assert.Equal(FuzzResult.Rejected("DecodedByteLimitExceeded"), target.Run(Compress(decoded)));
        Assert.Equal(FuzzResult.Rejected("ExpansionRatioExceeded"), target.Run(Compress(new byte[4096])));
    }

    [Fact]
    public void FontSanitizerReportsCanonicalDigestAndPreservesInput()
    {
        using MemoryStream fixture = TestFontFactory.CreateMinimalTrueType();
        byte[] input = fixture.ToArray();
        byte[] snapshot = input.ToArray();
        string expected = new PdfOpenTypeFontSanitizer().Sanitize(fixture).Sha256;
        FuzzTarget target = TargetRegistry.Get("font-sanitize");
        FuzzResult result = target.Run(input);
        Assert.Equal("success", result.Outcome);
        Assert.Equal(expected, result.Observation);
        Assert.Equal(result, target.Run(input));
        Assert.Equal(snapshot, input);
        Assert.Equal(64, result.Observation.Length);
    }

    [Fact]
    public void FontSanitizerUsesDomainRejectionsAndConfiguredTableCeiling()
    {
        FuzzTarget target = TargetRegistry.Get("font-sanitize");
        Assert.Equal(FuzzResult.Rejected("MalformedHeader"), target.Run([]));
        Assert.Equal(FuzzResult.Rejected("UnsupportedContainer"), target.Run(new byte[65536]));
        byte[] header = new byte[12];
        BinaryPrimitives.WriteUInt32BigEndian(header, 0x00010000);
        BinaryPrimitives.WriteUInt16BigEndian(header.AsSpan(4), 33);
        Assert.Equal(FuzzResult.Rejected("TooManyTables"), target.Run(header));
        Assert.Equal(FuzzResult.Rejected("TooManyTables"), target.Run(header));
    }

    private static byte[] Compress(ReadOnlySpan<byte> input)
    {
        using var output = new MemoryStream();
        using (var compressor = new ZLibStream(output, CompressionLevel.SmallestSize, leaveOpen: true))
        {
            compressor.Write(input);
        }

        return output.ToArray();
    }
}
