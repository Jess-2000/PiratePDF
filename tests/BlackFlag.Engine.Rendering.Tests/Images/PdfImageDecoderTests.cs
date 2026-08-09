#nullable enable

using System.IO;
using BlackFlag.Engine.Objects;
using BlackFlag.Engine.Rendering.DisplayList;
using BlackFlag.Engine.Rendering.Graphics;
using BlackFlag.Engine.Rendering.Images;

namespace BlackFlag.Engine.Rendering.Tests.Images;

public sealed class PdfImageDecoderTests
{
    [Fact]
    public void DecodesOneBitGrayImageWithRowPadding()
    {
        PdfDisplayImage item = CreateInlineImage(
            3,
            1,
            1,
            "DeviceGray",
            [0b1010_0000]);

        PdfImageDecodeResult result = new PdfImageDecoder().Decode(item);

        Assert.Equal(3, result.Image.Width);
        Assert.Equal(
            new byte[]
            {
                255, 255, 255, 255,
                0, 0, 0, 255,
                255, 255, 255, 255
            },
            result.Image.Pixels.ToArray());
    }

    [Fact]
    public void DecodeArrayCanInvertGraySamples()
    {
        PdfDisplayImage item = CreateInlineImage(
            2,
            1,
            8,
            "DeviceGray",
            [0, 255],
            new PdfArray([new PdfInteger(1), new PdfInteger(0)]));

        PdfImageDecodeResult result = new PdfImageDecoder().Decode(item);

        Assert.Equal(
            new byte[]
            {
                255, 255, 255, 255,
                0, 0, 0, 255
            },
            result.Image.Pixels.ToArray());
    }

    [Fact]
    public void DecodesDeviceRgbSamples()
    {
        PdfDisplayImage item = CreateInlineImage(
            1,
            1,
            8,
            "DeviceRGB",
            [10, 20, 30]);

        PdfImageDecodeResult result = new PdfImageDecoder().Decode(item);

        Assert.Equal(
            new byte[] { 10, 20, 30, 255 },
            result.Image.Pixels.ToArray());
    }

    [Fact]
    public void DecodesDeviceCmykSamples()
    {
        PdfDisplayImage item = CreateInlineImage(
            1,
            1,
            8,
            "DeviceCMYK",
            [0, 255, 255, 0]);

        PdfImageDecodeResult result = new PdfImageDecoder().Decode(item);

        Assert.Equal(
            new byte[] { 255, 0, 0, 255 },
            result.Image.Pixels.ToArray());
    }

    [Fact]
    public void EnforcesPixelLimitBeforeAllocating()
    {
        PdfDisplayImage item = CreateInlineImage(
            10,
            10,
            8,
            "DeviceGray",
            new byte[100]);

        Assert.Throws<InvalidDataException>(() =>
            new PdfImageDecoder().Decode(
                item,
                new PdfImageDecodeLimits { MaximumDecodedPixels = 50 }));
    }

    private static PdfDisplayImage CreateInlineImage(
        int width,
        int height,
        int bits,
        string colorSpace,
        byte[] bytes,
        PdfArray? decode = null)
    {
        List<KeyValuePair<PdfName, PdfObject>> entries =
        [
            new(new PdfName("W"), new PdfInteger(width)),
            new(new PdfName("H"), new PdfInteger(height)),
            new(new PdfName("BPC"), new PdfInteger(bits)),
            new(new PdfName("CS"), new PdfName(colorSpace))
        ];

        if (decode is not null)
        {
            entries.Add(new(new PdfName("D"), decode));
        }

        PdfInlineImage inline = new(new PdfDictionary(entries), bytes);
        return new PdfDisplayImage(
            null,
            inline,
            PdfMatrix.Identity,
            new PdfGraphicsStateSnapshot(),
            new PdfContentSourceSpan(0, 0, bytes.Length),
            null);
    }
}
