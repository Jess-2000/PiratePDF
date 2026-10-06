#nullable enable

using System.IO;
using System.Threading;
using BlackFlag.Engine.Objects;
using BlackFlag.Engine.Rendering.DisplayList;
using BlackFlag.Engine.Rendering.Graphics;
using BlackFlag.Engine.Rendering.Images;

namespace BlackFlag.Engine.Rendering.Tests.Images;

public sealed class PdfImageDecodeLimitTests
{
    [Theory]
    [InlineData(50_000, 50_000)]
    [InlineData(32_768, 16_384)]
    public void UnrepresentableDeclaredRgbaSizeIsRejectedBeforeCodec(int width, int height)
    {
        var codec = new LegacyCodec();
        var limits = new PdfImageDecodeLimits { MaximumDecodedPixels = long.MaxValue };

        InvalidDataException exception = Assert.Throws<InvalidDataException>(() =>
            new PdfImageDecoder([codec]).Decode(CreateImage(width, height, [1]), limits));

        Assert.IsType<OverflowException>(exception.InnerException);
        Assert.Equal(0, codec.Calls);
    }

    [Theory]
    [InlineData(3, 1)]
    [InlineData(1, 3)]
    public void DeclaredDimensionLimitStillPrecedesCodec(int width, int height)
    {
        var codec = new LegacyCodec();

        Assert.Throws<InvalidDataException>(() => new PdfImageDecoder([codec]).Decode(
            CreateImage(width, height, [1]), new PdfImageDecodeLimits { MaximumDimension = 2 }));

        Assert.Equal(0, codec.Calls);
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(1, 0)]
    [InlineData(-1, 1)]
    [InlineData(1, -1)]
    public void InvalidDeclaredDimensionsStillPrecedeCodec(int width, int height)
    {
        var codec = new LegacyCodec();

        Assert.Throws<InvalidDataException>(() =>
            new PdfImageDecoder([codec]).Decode(CreateImage(width, height, [1])));

        Assert.Equal(0, codec.Calls);
    }

    [Fact]
    public void EncodedLimitAllowsExactAndRejectsOneMoreBeforeCodec()
    {
        var codec = new LegacyCodec();
        var decoder = new PdfImageDecoder([codec]);
        var limits = new PdfImageDecodeLimits { MaximumEncodedBytes = 2 };

        _ = decoder.Decode(CreateImage(1, 1, [1, 2]), limits);
        Assert.Throws<InvalidDataException>(() => decoder.Decode(CreateImage(1, 1, [1, 2, 3]), limits));

        Assert.Equal(1, codec.Calls);
    }

    [Fact]
    public void LegacyCodecStillRunsAndItsReturnedDimensionsAreChecked()
    {
        var codec = new LegacyCodec();
        var decoder = new PdfImageDecoder([codec]);
        PdfImageDecodeResult decoded = decoder.Decode(CreateImage(1, 1, [1]));

        Assert.Equal(2, decoded.Image.Width);
        Assert.Throws<InvalidDataException>(() => decoder.Decode(
            CreateImage(1, 1, [1]), new PdfImageDecodeLimits { MaximumDimension = 1 }));
        Assert.Equal(2, codec.Calls);
    }

    [Fact]
    public void BoundedCodecReceivesExactPerCallLimitsAndCancellationToken()
    {
        var codec = new BoundedCodec();
        var limits = new PdfImageDecodeLimits { MaximumDimension = 3, MaximumDecodedPixels = 6 };
        using var cancellation = new CancellationTokenSource();

        _ = new PdfImageDecoder([codec]).Decode(CreateImage(1, 1, [1]), limits, cancellation.Token);

        Assert.Same(limits, codec.ReceivedLimits);
        Assert.Equal(cancellation.Token, codec.ReceivedToken);
    }

    private sealed class BoundedCodec : IBoundedPdfImageCodec
    {
        public string FilterName => "DCTDecode";
        public PdfImageDecodeLimits? ReceivedLimits { get; private set; }
        public CancellationToken ReceivedToken { get; private set; }

        public PdfDecodedImage Decode(ReadOnlyMemory<byte> encodedBytes, PdfDictionary dictionary,
            CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("The bounded entry point must be used.");

        public PdfDecodedImage Decode(ReadOnlyMemory<byte> encodedBytes, PdfDictionary dictionary,
            PdfImageDecodeLimits limits, CancellationToken cancellationToken)
        {
            ReceivedLimits = limits;
            ReceivedToken = cancellationToken;
            return new PdfDecodedImage(1, 1, new byte[4]);
        }
    }

    private static PdfDisplayImage CreateImage(int width, int height, byte[] encoded)
    {
        var dictionary = new PdfDictionary(new Dictionary<PdfName, PdfObject>
        {
            [new PdfName("W")] = new PdfInteger(width),
            [new PdfName("H")] = new PdfInteger(height),
            [new PdfName("F")] = new PdfName("DCT")
        });
        return new PdfDisplayImage(null, new PdfInlineImage(dictionary, encoded), PdfMatrix.Identity,
            new PdfGraphicsStateSnapshot(), new PdfContentSourceSpan(0, 0, encoded.Length), null);
    }

    // An existing implementation need not adopt the repository's internal seam.
    private sealed class LegacyCodec : IPdfImageCodec
    {
        public string FilterName => "DCTDecode";
        public int Calls { get; private set; }

        public PdfDecodedImage Decode(ReadOnlyMemory<byte> encodedBytes, PdfDictionary dictionary,
            CancellationToken cancellationToken = default)
        {
            Calls++;
            cancellationToken.ThrowIfCancellationRequested();
            return new PdfDecodedImage(2, 1, new byte[8]);
        }
    }
}
