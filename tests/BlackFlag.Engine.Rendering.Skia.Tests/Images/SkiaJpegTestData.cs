using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using BlackFlag.Engine.Objects;
using BlackFlag.Engine.Rendering.DisplayList;
using BlackFlag.Engine.Rendering.Graphics;
using SkiaSharp;

namespace BlackFlag.Engine.Rendering.Skia.Tests.Images;

internal static class SkiaJpegTestData
{
    internal static byte[] Create(int width = 3, int height = 2)
    {
        using var bitmap = new SKBitmap(new SKImageInfo(width, height));
        bitmap.Erase(SKColors.White);
        using SKImage image = SKImage.FromBitmap(bitmap);
        using SKData data = image.Encode(SKEncodedImageFormat.Jpeg, 100);
        return data.ToArray();
    }

    internal static byte[] WithIntrinsicDimensions(ushort width, ushort height)
    {
        // Only patch this tiny generated fixture's SOF header. Never create
        // source pixels at the hostile dimensions or decode them in a fixture.
        byte[] encoded = Create();
        for (int offset = 2; offset + 9 < encoded.Length; offset++)
        {
            if (encoded[offset] == 0xFF && encoded[offset + 1] is 0xC0 or 0xC2)
            {
                BinaryPrimitives.WriteUInt16BigEndian(encoded.AsSpan(offset + 5, 2), height);
                BinaryPrimitives.WriteUInt16BigEndian(encoded.AsSpan(offset + 7, 2), width);
                return encoded;
            }
        }

        throw new InvalidOperationException("The test JPEG has no SOF dimension fields.");
    }

    internal static PdfDisplayImage Display(byte[] encoded, int width = 1, int height = 1)
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
}
