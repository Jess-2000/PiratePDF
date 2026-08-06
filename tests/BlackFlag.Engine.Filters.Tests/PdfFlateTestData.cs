using System.IO.Compression;

namespace BlackFlag.Engine.Filters.Tests;

internal static class PdfFlateTestData
{
    public static byte[] Compress(
        ReadOnlySpan<byte> data)
    {
        using var destination =
            new MemoryStream();

        using (
            var stream =
                new ZLibStream(
                    destination,
                    CompressionLevel.SmallestSize,
                    leaveOpen: true))
        {
            stream.Write(data);
        }

        return destination.ToArray();
    }
}
