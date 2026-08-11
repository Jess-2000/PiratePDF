
using System.Text;

namespace BlackFlag.Engine.Rendering.Images;

/// <summary>Recognizes image formats from content signatures rather than filename extensions.</summary>
public static class PdfImageFormatRecognizer
{
    /// <summary>Recognizes the supplied leading bytes.</summary>
    public static PdfImageSourceFormat Recognize(ReadOnlySpan<byte> data)
    {
        if (StartsWith(data, [0xFF, 0xD8, 0xFF]))
            return PdfImageSourceFormat.Jpeg;

        if (StartsWith(data, [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A]))
            return PdfImageSourceFormat.Png;

        if (StartsWith(data, [0x49, 0x49, 0x2A, 0x00]) ||
            StartsWith(data, [0x4D, 0x4D, 0x00, 0x2A]))
            return PdfImageSourceFormat.Tiff;

        if (StartsWith(data, [0x42, 0x4D]))
            return PdfImageSourceFormat.Bmp;

        if (StartsWithAscii(data, "GIF87a") ||
            StartsWithAscii(data, "GIF89a"))
            return PdfImageSourceFormat.Gif;

        if (data.Length >= 12 &&
            StartsWithAscii(data, "RIFF") &&
            Encoding.ASCII.GetString(data.Slice(8, 4)) == "WEBP")
            return PdfImageSourceFormat.WebP;

        if (StartsWith(data, [0x00, 0x00, 0x00, 0x0C, 0x6A, 0x50, 0x20, 0x20, 0x0D, 0x0A, 0x87, 0x0A]) ||
            StartsWith(data, [0xFF, 0x4F, 0xFF, 0x51]))
            return PdfImageSourceFormat.Jpeg2000;

        if (StartsWith(data, [0xFF, 0x0A]) ||
            StartsWith(data, [0x00, 0x00, 0x00, 0x0C, 0x4A, 0x58, 0x4C, 0x20, 0x0D, 0x0A, 0x87, 0x0A]))
            return PdfImageSourceFormat.JpegXl;

        if (data.Length >= 12 &&
            Encoding.ASCII.GetString(data.Slice(4, 4)) == "ftyp")
            return PdfImageSourceFormat.Heif;

        string prefix =
            Encoding.UTF8.GetString(
                data[..Math.Min(data.Length, 512)]);

        string trimmed =
            prefix.TrimStart('\uFEFF', ' ', '\t', '\r', '\n');

        if (trimmed.StartsWith("<svg", StringComparison.OrdinalIgnoreCase) ||
            (trimmed.StartsWith("<?xml", StringComparison.OrdinalIgnoreCase) &&
             trimmed.Contains("<svg", StringComparison.OrdinalIgnoreCase)))
            return PdfImageSourceFormat.Svg;

        if (trimmed.StartsWith("%!PS-Adobe", StringComparison.Ordinal) &&
            trimmed.Contains("EPSF", StringComparison.Ordinal))
            return PdfImageSourceFormat.Eps;

        return PdfImageSourceFormat.Unknown;
    }

    private static bool StartsWith(
        ReadOnlySpan<byte> data,
        ReadOnlySpan<byte> signature) =>
        data.Length >= signature.Length &&
        data[..signature.Length].SequenceEqual(signature);

    private static bool StartsWithAscii(
        ReadOnlySpan<byte> data,
        string text) =>
        data.Length >= text.Length &&
        Encoding.ASCII.GetString(data[..text.Length]) == text;
}
