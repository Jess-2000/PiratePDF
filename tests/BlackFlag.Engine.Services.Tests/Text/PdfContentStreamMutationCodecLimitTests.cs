using System.IO;
using System.IO.Compression;
using System.Threading;
using BlackFlag.Engine.Document.Objects;
using BlackFlag.Engine.Document.Objects.Primitives;
using BlackFlag.Engine.Filters;
using BlackFlag.Engine.Services.Text;

namespace BlackFlag.Engine.Services.Tests.Text;

public sealed class PdfContentStreamMutationCodecLimitTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ExactDecodedByteLimitSucceedsAcrossChunks(bool filtered)
    {
        byte[] data = Enumerable.Range(0, 257).Select(value => (byte)value).ToArray();
        var stream = filtered ? CreateStream(Compress(data)) : new PdfStream([], data);
        var limits = new PdfFilterDecodeLimits(data.Length, 1000, 1, 31);

        Assert.Equal(data, PdfContentStreamMutationCodec.Decode(stream, limits));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void DecodedByteLimitPlusOneIsRejectedWithoutChangingSource(bool filtered)
    {
        byte[] data = Enumerable.Range(0, 258).Select(value => (byte)value).ToArray();
        var stream = filtered ? CreateStream(Compress(data)) : new PdfStream([], data);
        byte[] original = stream.ToByteArray();
        var limits = new PdfFilterDecodeLimits(data.Length - 1, 1000, 1, 31);

        PdfFilterDecodeException exception = Assert.Throws<PdfFilterDecodeException>(
            () => PdfContentStreamMutationCodec.Decode(stream, limits));

        Assert.Equal(PdfFilterDecodeFailure.DecodedByteLimitExceeded, exception.Failure);
        Assert.Equal(original, stream.ToByteArray());
    }

    [Fact]
    public void ExactExpansionRatioSucceedsAndSmallerLimitRejects()
    {
        byte[] data = new byte[4096];
        byte[] encoded = Compress(data);
        var stream = CreateStream(encoded);
        double ratio = (double)data.Length / encoded.Length;
        var exact = new PdfFilterDecodeLimits(data.Length, ratio, 1, 31);
        var smaller = new PdfFilterDecodeLimits(data.Length, Math.BitDecrement(ratio), 1, 31);

        Assert.Equal(data, PdfContentStreamMutationCodec.Decode(stream, exact));
        PdfFilterDecodeException exception = Assert.Throws<PdfFilterDecodeException>(
            () => PdfContentStreamMutationCodec.Decode(stream, smaller));
        Assert.Equal(PdfFilterDecodeFailure.ExpansionRatioExceeded, exception.Failure);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void CancellationPrecedesCopyingSupportedContent(bool filtered)
    {
        byte[] data = "BT (Hello) Tj ET"u8.ToArray();
        var stream = filtered ? CreateStream(Compress(data)) : new PdfStream([], data);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        OperationCanceledException decode = Assert.Throws<OperationCanceledException>(
            () => PdfContentStreamMutationCodec.Decode(stream, cancellation.Token));
        OperationCanceledException encode = Assert.Throws<OperationCanceledException>(
            () => PdfContentStreamMutationCodec.Encode(stream, data, cancellation.Token));

        Assert.Equal(cancellation.Token, decode.CancellationToken);
        Assert.Equal(cancellation.Token, encode.CancellationToken);
    }

    [Fact]
    public void DefaultBudgetRejectsHighExpansionWithoutChangingSource()
    {
        byte[] encoded = Compress(new byte[1024 * 1024]);
        var stream = CreateStream(encoded);

        PdfFilterDecodeException exception = Assert.Throws<PdfFilterDecodeException>(
            () => PdfContentStreamMutationCodec.Decode(stream));

        Assert.Equal(PdfFilterDecodeFailure.ExpansionRatioExceeded, exception.Failure);
        Assert.Equal(encoded, stream.ToByteArray());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(4)]
    [InlineData(8)]
    public void TruncatedFlateRetainsInvalidDataExceptionContract(int retainedBytes)
    {
        byte[] encoded = Compress("BT (Hello) Tj ET"u8);
        var stream = CreateStream(encoded[..retainedBytes]);

        InvalidDataException exception = Assert.Throws<InvalidDataException>(
            () => PdfContentStreamMutationCodec.Decode(stream));

        Assert.Contains("invalid or incomplete zlib", exception.Message);
        Assert.Equal(encoded[..retainedBytes], stream.ToByteArray());
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(4)]
    public void MissingFlateTrailerIsRejected(int missingBytes)
    {
        byte[] encoded = Compress("BT (Hello) Tj ET"u8);
        var stream = CreateStream(encoded[..^missingBytes]);

        Assert.Throws<InvalidDataException>(
            () => PdfContentStreamMutationCodec.Decode(stream));
    }

    [Fact]
    public void CorruptFlateRetainsInvalidDataExceptionContract()
    {
        var stream = CreateStream([1, 2, 3, 4, 5]);

        Assert.Throws<InvalidDataException>(
            () => PdfContentStreamMutationCodec.Decode(stream));
    }

    [Fact]
    public void CorruptChecksumIsRejected()
    {
        byte[] encoded = Compress("BT (Hello) Tj ET"u8);
        encoded[^1] ^= 0xFF;

        Assert.Throws<InvalidDataException>(
            () => PdfContentStreamMutationCodec.Decode(CreateStream(encoded)));
    }

    [Theory]
    [InlineData("FlateDecode", false, false)]
    [InlineData("Fl", false, false)]
    [InlineData("Fl", true, false)]
    [InlineData("FlateDecode", true, true)]
    public void SupportedFilterAndParameterRepresentationIsUnchanged(
        string filterName,
        bool arrayFilter,
        bool arrayParameters)
    {
        PdfName filter = PdfName.FromLatin1(filterName);
        var parameters = new PdfDictionary
        {
            { PdfName.FromLatin1("Predictor"), new PdfInteger(1) },
            { PdfName.FromLatin1("PrivateParameter"), new PdfInteger(42) }
        };
        var dictionary = new PdfDictionary
        {
            { PdfName.FromLatin1("Filter"), arrayFilter ? new PdfArray([filter]) : filter },
            { PdfName.FromLatin1("DecodeParms"), arrayParameters ? new PdfArray([parameters]) : parameters },
            { PdfName.FromLatin1("PrivateEntry"), new PdfInteger(17) }
        };
        var stream = new PdfStream(dictionary, Compress("BT (Hello) Tj ET"u8));
        byte[] before = SerializeDictionary(stream.Dictionary);

        byte[] decoded = PdfContentStreamMutationCodec.Decode(stream);
        byte[] encoded = PdfContentStreamMutationCodec.Encode(stream, decoded);

        Assert.Equal(before, SerializeDictionary(stream.Dictionary));
        Assert.Equal(decoded, PdfContentStreamMutationCodec.Decode(new PdfStream(dictionary, encoded)));
    }

    private static byte[] SerializeDictionary(PdfDictionary dictionary)
    {
        using var destination = new MemoryStream();
        BlackFlag.Engine.Writing.PdfObjectSerializer.Write(destination, dictionary);
        return destination.ToArray();
    }

    [Fact]
    public void CancelledFlateDecodeRetainsOriginalTokenAndSource()
    {
        byte[] encoded = Compress("BT (Hello) Tj ET"u8);
        var stream = CreateStream(encoded);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        OperationCanceledException exception = Assert.Throws<OperationCanceledException>(
            () => PdfContentStreamMutationCodec.Decode(stream, cancellation.Token));

        Assert.Equal(cancellation.Token, exception.CancellationToken);
        Assert.Equal(encoded, stream.ToByteArray());
    }

    [Fact]
    public void CompleteEmptyFlateStreamDecodesToEmptyContent()
    {
        // Complete RFC 1950 stream: empty final DEFLATE block and Adler-32.
        var stream = CreateStream([0x78, 0x9C, 0x03, 0x00, 0x00, 0x00, 0x00, 0x01]);

        Assert.Empty(PdfContentStreamMutationCodec.Decode(stream));
    }

    [Fact]
    public void EmptyReplacementEncodesACompleteFlateMember()
    {
        var original = CreateStream(Compress("BT (Hello) Tj ET"u8));
        byte[] encoded = PdfContentStreamMutationCodec.Encode(original, []);

        Assert.NotEmpty(encoded);
        Assert.Empty(PdfContentStreamMutationCodec.Decode(CreateStream(encoded)));
    }

    [Fact]
    public void CompleteFlateWithTrailingBytesRetainsExistingAcceptance()
    {
        byte[] encoded = Compress("BT (Hello) Tj ET"u8);
        var stream = CreateStream([.. encoded, 0x0A, 0x00]);

        Assert.Equal("BT (Hello) Tj ET"u8.ToArray(), PdfContentStreamMutationCodec.Decode(stream));
    }

    [Theory]
    [InlineData("78")]
    [InlineData("789C")]
    [InlineData("7801")]
    [InlineData("78DA")]
    [InlineData("7801000600F9FF414243444546")]
    public void IncompleteFlateMatchesOrdinaryFilterRejection(string hex)
    {
        byte[] encoded = Convert.FromHexString(hex);
        var stream = CreateStream(encoded);
        var editing = Assert.Throws<InvalidDataException>(() => PdfContentStreamMutationCodec.Decode(stream));
        Assert.Equal("The editable Flate content stream contains invalid or incomplete zlib data.", editing.Message);
        var pipeline = new PdfFilterPipeline(PdfFilterRegistrations.CreateDefault());
        var ordinary = Assert.Throws<PdfFilterDecodeException>(() => pipeline.Decode(
            encoded, [new BlackFlag.Engine.Objects.PdfName("FlateDecode")], null, new()));
        Assert.Equal(PdfFilterDecodeFailure.InvalidData, ordinary.Failure);
        Assert.Equal(encoded, stream.ToByteArray());
    }

    [Fact]
    public void ConcatenatedMembersKeepOnlyFirstMemberInBothPaths()
    {
        byte[] encoded = [.. Compress("ABCDEF"u8), .. Compress("GH"u8)];
        var pipeline = new PdfFilterPipeline(PdfFilterRegistrations.CreateDefault());
        Assert.Equal("ABCDEF"u8.ToArray(), pipeline.Decode(
            encoded, [new BlackFlag.Engine.Objects.PdfName("FlateDecode")], null, new()).ToArray());
        Assert.Equal("ABCDEF"u8.ToArray(), PdfContentStreamMutationCodec.Decode(CreateStream(encoded)));
    }

    private static PdfStream CreateStream(byte[] encoded) => new(
        new PdfDictionary
        {
            { PdfName.FromLatin1("Filter"), PdfName.FromLatin1("FlateDecode") }
        },
        encoded);

    private static byte[] Compress(ReadOnlySpan<byte> data)
    {
        using var destination = new MemoryStream();
        using (var zlib = new ZLibStream(destination, CompressionLevel.Optimal, leaveOpen: true))
        {
            zlib.Write(data);
        }

        return destination.ToArray();
    }
}
