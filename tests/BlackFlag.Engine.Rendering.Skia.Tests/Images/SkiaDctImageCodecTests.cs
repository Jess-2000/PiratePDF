using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using BlackFlag.Engine.Objects;
using BlackFlag.Engine.Rendering.Images;
using BlackFlag.Engine.Rendering.Skia.Images;
using Xunit.Abstractions;

namespace BlackFlag.Engine.Rendering.Skia.Tests.Images;

public sealed class SkiaDctImageCodecTests(ITestOutputHelper output)
{
    [Fact]
    public void ValidSmallJpegRetainsExactWhiteRgbaPixels()
    {
        byte[] encoded = SkiaJpegTestData.Create();
        PdfDecodedImage decoded = new SkiaDctImageCodec().Decode(encoded, new PdfDictionary([]));

        Assert.Equal(3, decoded.Width);
        Assert.Equal(2, decoded.Height);
        Assert.Equal([.. Enumerable.Repeat((byte)255, 24)], decoded.Pixels.ToArray());

        output.WriteLine($"Native validation host: {RuntimeInformation.OSDescription}; {RuntimeInformation.ProcessArchitecture}");
        if (OperatingSystem.IsWindows())
        {
            using Process process = Process.GetCurrentProcess();
            foreach (ProcessModule module in process.Modules)
            {
                if (string.Equals(module.ModuleName, "libSkiaSharp.dll", StringComparison.OrdinalIgnoreCase))
                {
                    using FileStream nativeFile = File.OpenRead(module.FileName);
                    output.WriteLine($"Loaded native asset: {module.FileName}");
                    output.WriteLine($"SHA256: {Convert.ToHexString(SHA256.HashData(nativeFile))}");
                }
            }
        }
    }

    [Fact]
    public void WithinLimitDeclaredIntrinsicMismatchRetainsIntrinsicDimensions()
    {
        var decoder = new PdfImageDecoder([new SkiaDctImageCodec()]);
        PdfImageDecodeResult result = decoder.Decode(SkiaJpegTestData.Display(SkiaJpegTestData.Create()),
            new PdfImageDecodeLimits { MaximumDimension = 3, MaximumDecodedPixels = 6 });

        Assert.Equal(3, result.Image.Width);
        Assert.Equal(2, result.Image.Height);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(20)]
    public void TruncatedJpegHeaderIsInvalidData(int retainedBytes)
    {
        byte[] encoded = SkiaJpegTestData.Create();

        Assert.Throws<InvalidDataException>(() =>
            new SkiaDctImageCodec().Decode(encoded.AsMemory()[..retainedBytes], new PdfDictionary([])));
    }

    [Fact]
    public void MissingEndMarkerRetainsIncompleteInputAcceptance()
    {
        byte[] encoded = SkiaJpegTestData.Create();
        Assert.Equal(new byte[] { 0xFF, 0xD9 }, encoded[^2..]);

        PdfDecodedImage decoded = new SkiaDctImageCodec().Decode(encoded[..^2], new PdfDictionary([]));

        // Characterized before B2: this incomplete member is accepted with
        // transparent pixels, not the complete fixture's white pixels.
        Assert.Equal(3, decoded.Width);
        Assert.Equal(2, decoded.Height);
        Assert.Equal(new byte[24], decoded.Pixels.ToArray());
    }
}
