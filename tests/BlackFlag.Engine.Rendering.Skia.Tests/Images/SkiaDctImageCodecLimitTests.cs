using System;
using System.IO;
using System.Threading;
using BlackFlag.Engine.Objects;
using BlackFlag.Engine.Rendering.Images;
using BlackFlag.Engine.Rendering.Skia.Images;
using SkiaSharp;

namespace BlackFlag.Engine.Rendering.Skia.Tests.Images;

public sealed class SkiaDctImageCodecLimitTests
{
    [Theory]
    [InlineData(3, 1)]
    [InlineData(1, 3)]
    public void IntrinsicDimensionAtLimitAllocatesAndOneOverRejectsBeforeAllocation(int width, int height)
    {
        int allocations = 0;
        var codec = new SkiaDctImageCodec(info =>
        {
            allocations++;
            return new SKBitmap(info);
        });
        var decoder = new PdfImageDecoder([codec]);
        var item = SkiaJpegTestData.Display(SkiaJpegTestData.Create(width, height));

        PdfImageDecodeResult decoded = decoder.Decode(item, new PdfImageDecodeLimits { MaximumDimension = 3 });
        Assert.Equal(width, decoded.Image.Width);
        Assert.Equal(height, decoded.Image.Height);
        Assert.Equal(1, allocations);

        InvalidDataException exception = Assert.Throws<InvalidDataException>(() =>
            decoder.Decode(item, new PdfImageDecodeLimits { MaximumDimension = 2 }));
        Assert.Equal("Image dimension exceeds the configured limit.", exception.Message);
        Assert.Equal(1, allocations);
    }

    [Fact]
    public void IntrinsicPixelLimitRejectsBeforeAllocationDespiteSmallDictionary()
    {
        var codec = new SkiaDctImageCodec(_ => throw new InvalidOperationException("Pixels must not be allocated."));
        var decoder = new PdfImageDecoder([codec]);

        InvalidDataException exception = Assert.Throws<InvalidDataException>(() => decoder.Decode(
            SkiaJpegTestData.Display(SkiaJpegTestData.Create()),
            new PdfImageDecodeLimits { MaximumDecodedPixels = 5 }));

        Assert.Equal("Decoded image exceeds the configured pixel limit.", exception.Message);
    }

    [Theory]
    [InlineData(50_000, 50_000)]
    [InlineData(32_768, 16_384)]
    public void IntrinsicPixelAndByteOverflowRejectBeforeAllocation(int width, int height)
    {
        byte[] encoded = SkiaJpegTestData.WithIntrinsicDimensions((ushort)width, (ushort)height);
        var codec = new SkiaDctImageCodec(_ => throw new InvalidOperationException("Pixels must not be allocated."));
        var decoder = new PdfImageDecoder([codec]);

        InvalidDataException exception = Assert.Throws<InvalidDataException>(() => decoder.Decode(
            SkiaJpegTestData.Display(encoded),
            new PdfImageDecodeLimits { MaximumDecodedPixels = long.MaxValue }));

        Assert.IsType<OverflowException>(exception.InnerException);
        Assert.Equal("Decoded image RGBA size exceeds the supported range.", exception.Message);
    }

    [Theory]
    [InlineData(0, 2)]
    [InlineData(3, 0)]
    public void ZeroIntrinsicDimensionRejectsWithoutPixelAllocation(int width, int height)
    {
        byte[] encoded = SkiaJpegTestData.WithIntrinsicDimensions((ushort)width, (ushort)height);
        var codec = new SkiaDctImageCodec(_ => throw new InvalidOperationException("Pixels must not be allocated."));

        Assert.Throws<InvalidDataException>(() =>
            new PdfImageDecoder([codec]).Decode(SkiaJpegTestData.Display(encoded)));
    }

    [Fact]
    public void DirectCodecUsesDefaultPixelLimitBeforeAllocation()
    {
        byte[] encoded = SkiaJpegTestData.WithIntrinsicDimensions(10_001, 10_000);
        var codec = new SkiaDctImageCodec(_ => throw new InvalidOperationException("Pixels must not be allocated."));

        InvalidDataException exception = Assert.Throws<InvalidDataException>(() =>
            codec.Decode(encoded, new PdfDictionary([])));

        Assert.Equal("Decoded image exceeds the configured pixel limit.", exception.Message);
    }

    [Fact]
    public void NativeAllocationFailureIsInvalidData()
    {
        int attempts = 0;
        var codec = new SkiaDctImageCodec(_ =>
        {
            attempts++;
            return null;
        });

        InvalidDataException exception = Assert.Throws<InvalidDataException>(() =>
            codec.Decode(SkiaJpegTestData.Create(), new PdfDictionary([])));

        Assert.Equal(1, attempts);
        Assert.Equal("JPEG pixel allocation failed.", exception.Message);
    }

    [Fact]
    public void NullPixelPointerIsRejectedBeforeDecodeOrCopy()
    {
        using var emptyBitmap = new SKBitmap();
        Assert.Equal(IntPtr.Zero, emptyBitmap.GetPixels());
        var codec = new SkiaDctImageCodec(_ => emptyBitmap);

        InvalidDataException exception = Assert.Throws<InvalidDataException>(() =>
            codec.Decode(SkiaJpegTestData.Create(), new PdfDictionary([])));

        Assert.Equal("JPEG pixel allocation did not provide the required RGBA buffer.", exception.Message);
    }

    [Fact]
    public void UndersizedPixelBufferIsRejectedBeforeNativeWrite()
    {
        var codec = new SkiaDctImageCodec(_ => new SKBitmap(1, 1));

        InvalidDataException exception = Assert.Throws<InvalidDataException>(() =>
            codec.Decode(SkiaJpegTestData.Create(), new PdfDictionary([])));

        Assert.Equal("JPEG pixel allocation did not provide the required RGBA buffer.", exception.Message);
    }

    [Fact]
    public void MalformedJpegRejectsBeforeAllocation()
    {
        var codec = new SkiaDctImageCodec(_ => throw new InvalidOperationException("Pixels must not be allocated."));

        InvalidDataException exception = Assert.Throws<InvalidDataException>(() =>
            codec.Decode(new byte[] { 1, 2, 3, 4 }, new PdfDictionary([])));

        Assert.Equal("DCTDecode payload is not a supported JPEG image.", exception.Message);
    }

    [Fact]
    public void PreCancelledDecodeRetainsTokenAndDoesNotAllocate()
    {
        byte[] encoded = SkiaJpegTestData.Create();
        var codec = new SkiaDctImageCodec(_ => throw new InvalidOperationException("Pixels must not be allocated."));
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        OperationCanceledException exception = Assert.Throws<OperationCanceledException>(() =>
            new PdfImageDecoder([codec]).Decode(
                SkiaJpegTestData.Display(encoded), cancellationToken: cancellation.Token));

        Assert.Equal(cancellation.Token, exception.CancellationToken);
    }

    [Fact]
    public void CancellationAfterAllocationIsObservedBeforeManagedCopy()
    {
        using var cancellation = new CancellationTokenSource();
        var codec = new SkiaDctImageCodec(info =>
        {
            var bitmap = new SKBitmap(info);
            cancellation.Cancel();
            return bitmap;
        });

        OperationCanceledException exception = Assert.Throws<OperationCanceledException>(() =>
            codec.Decode(SkiaJpegTestData.Create(), new PdfDictionary([]), cancellation.Token));

        Assert.Equal(cancellation.Token, exception.CancellationToken);
    }
}
