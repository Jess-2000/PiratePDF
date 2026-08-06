using System.IO.Compression;
using BlackFlag.Engine.Objects;

namespace BlackFlag.Engine.Filters.Tests;

public sealed class PdfFilterIntegrationTests
{
    [Fact]
    public void PipelineDecodesAscii85ThenFlate()
    {
        byte[] original =
            "BlackFlag chained filter integration"u8.ToArray();

        byte[] compressed =
            Compress(original);

        byte[] encoded =
            EncodeAscii85(compressed);

        var pipeline =
            new PdfFilterPipeline(
                PdfFilterRegistrations.CreateDefault());

        ReadOnlyMemory<byte> result =
            pipeline.Decode(
                encoded,
                [
                    new PdfName("ASCII85Decode"),
                    new PdfName("FlateDecode")
                ],
                decodeParameters: null,
                new PdfFilterDecodeLimits(
                    maximumDecodedBytes: 1024 * 1024,
                    maximumExpansionRatio: 1000,
                    maximumFilterCount: 8,
                    bufferSize: 256));

        Assert.Equal(
            original,
            result.ToArray());
    }

    [Fact]
    public void PipelineDecodesAsciiHexThenRunLength()
    {
        byte[] runLengthEncoded =
        [
            2,
            (byte)'A',
            (byte)'B',
            (byte)'C',
            254,
            (byte)'Z',
            128
        ];

        byte[] asciiHexEncoded =
            EncodeAsciiHex(runLengthEncoded);

        var pipeline =
            new PdfFilterPipeline(
                PdfFilterRegistrations.CreateDefault());

        ReadOnlyMemory<byte> result =
            pipeline.Decode(
                asciiHexEncoded,
                [
                    new PdfName("ASCIIHexDecode"),
                    new PdfName("RunLengthDecode")
                ],
                decodeParameters: null,
                new PdfFilterDecodeLimits(
                    maximumDecodedBytes: 1024,
                    maximumExpansionRatio: 100,
                    maximumFilterCount: 8,
                    bufferSize: 32));

        Assert.Equal(
            "ABCZZZ"u8.ToArray(),
            result.ToArray());
    }

    [Fact]
    public void PipelineRejectsChainBeyondConfiguredLimit()
    {
        var pipeline =
            new PdfFilterPipeline(
                PdfFilterRegistrations.CreateDefault());

        PdfFilterDecodeException exception =
            Assert.Throws<PdfFilterDecodeException>(
                () => pipeline.Decode(
                    "~>"u8.ToArray(),
                    [
                        new PdfName("ASCII85Decode"),
                        new PdfName("ASCIIHexDecode")
                    ],
                    decodeParameters: null,
                    new PdfFilterDecodeLimits(
                        maximumDecodedBytes: 1024,
                        maximumExpansionRatio: 100,
                        maximumFilterCount: 1,
                        bufferSize: 32)));

        Assert.Equal(
            PdfFilterDecodeFailure.FilterCountLimitExceeded,
            exception.Failure);
    }

    [Fact]
    public void DefaultRegistryContainsAllBf1041Filters()
    {
        PdfFilterRegistry registry =
            PdfFilterRegistrations.CreateDefault();

        Assert.IsType<PdfAsciiHexFilterDecoder>(
            registry.GetRequired(
                new PdfName("ASCIIHexDecode")));

        Assert.IsType<PdfAscii85FilterDecoder>(
            registry.GetRequired(
                new PdfName("ASCII85Decode")));

        Assert.IsType<PdfRunLengthFilterDecoder>(
            registry.GetRequired(
                new PdfName("RunLengthDecode")));

        Assert.IsType<PdfLzwFilterDecoder>(
            registry.GetRequired(
                new PdfName("LZWDecode")));

        Assert.IsType<PdfFlateFilterDecoder>(
            registry.GetRequired(
                new PdfName("FlateDecode")));
    }

    private static byte[] Compress(
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

    private static byte[] EncodeAsciiHex(
        ReadOnlySpan<byte> data)
    {
        const string Digits = "0123456789ABCDEF";

        byte[] result =
            new byte[
                checked(
                    (data.Length * 2) +
                    1)];

        int offset = 0;

        foreach (byte value in data)
        {
            result[offset++] =
                (byte)Digits[value >> 4];

            result[offset++] =
                (byte)Digits[value & 0x0F];
        }

        result[offset] =
            (byte)'>';

        return result;
    }

    private static byte[] EncodeAscii85(
        ReadOnlySpan<byte> data)
    {
        using var destination =
            new MemoryStream();

        int offset = 0;

        while (offset + 4 <= data.Length)
        {
            uint tuple =
                ((uint)data[offset] << 24) |
                ((uint)data[offset + 1] << 16) |
                ((uint)data[offset + 2] << 8) |
                data[offset + 3];

            if (tuple == 0)
            {
                destination.WriteByte(
                    (byte)'z');
            }
            else
            {
                Span<byte> group =
                    stackalloc byte[5];

                for (int index = 4; index >= 0; index--)
                {
                    group[index] =
                        (byte)(
                            (tuple % 85) +
                            (byte)'!');

                    tuple /= 85;
                }

                destination.Write(group);
            }

            offset += 4;
        }

        int remaining =
            data.Length -
            offset;

        if (remaining > 0)
        {
            uint tuple = 0;

            for (int index = 0; index < remaining; index++)
            {
                tuple |=
                    (uint)data[offset + index] <<
                    (24 - (index * 8));
            }

            Span<byte> group =
                stackalloc byte[5];

            for (int index = 4; index >= 0; index--)
            {
                group[index] =
                    (byte)(
                        (tuple % 85) +
                        (byte)'!');

                tuple /= 85;
            }

            destination.Write(
                group[..(remaining + 1)]);
        }

        destination.WriteByte(
            (byte)'~');

        destination.WriteByte(
            (byte)'>');

        return destination.ToArray();
    }
}

