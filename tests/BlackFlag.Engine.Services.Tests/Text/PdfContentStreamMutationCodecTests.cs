using System.IO;
using System.IO.Compression;
using System.Text;
using BlackFlag.Engine.Document.Objects;
using BlackFlag.Engine.Document.Objects.Primitives;
using BlackFlag.Engine.Services.Text;

namespace BlackFlag.Engine.Services.Tests.Text;

public sealed class PdfContentStreamMutationCodecTests
{
    [Fact]
    public void UnfilteredStreamRoundTripsWithoutRepresentationChange()
    {
        byte[] data =
            Encoding.ASCII.GetBytes(
                "BT (Hello) Tj ET");

        var stream =
            new PdfStream(
                [],
                data);

        byte[] decoded =
            PdfContentStreamMutationCodec.Decode(
                stream);

        byte[] encoded =
            PdfContentStreamMutationCodec.Encode(
                stream,
                decoded);

        Assert.Equal(
            data,
            decoded);

        Assert.Equal(
            data,
            encoded);
    }

    [Fact]
    public void SingleFlateStreamDecodesAndReencodesPreservingFilterDictionary()
    {
        byte[] decoded =
            Encoding.ASCII.GetBytes(
                "BT (Hello) Tj ET");

        byte[] encoded =
            Compress(
                decoded);

        var dictionary =
            new PdfDictionary
            {
                {
                    PdfName.FromLatin1("Filter"),
                    PdfName.FromLatin1("FlateDecode")
                }
            };

        var stream =
            new PdfStream(
                dictionary,
                encoded);

        byte[] actualDecoded =
            PdfContentStreamMutationCodec.Decode(
                stream);

        Assert.Equal(
            decoded,
            actualDecoded);

        byte[] replacement =
            Encoding.ASCII.GetBytes(
                "BT (Edited) Tj ET");

        byte[] replacementEncoded =
            PdfContentStreamMutationCodec.Encode(
                stream,
                replacement);

        Assert.Equal(
            replacement,
            Decompress(
                replacementEncoded));

        Assert.Equal(
            PdfName.FromLatin1("FlateDecode"),
            stream.Dictionary.Get<PdfName>(
                PdfName.FromLatin1("Filter")));
    }

    [Fact]
    public void FlatePredictorOneIsPreservationSafe()
    {
        byte[] data =
            Encoding.ASCII.GetBytes(
                "q /X1 Do Q");

        var stream =
            new PdfStream(
                new PdfDictionary
                {
                    {
                        PdfName.FromLatin1("Filter"),
                        PdfName.FromLatin1("FlateDecode")
                    },
                    {
                        PdfName.FromLatin1("DecodeParms"),
                        new PdfDictionary
                        {
                            {
                                PdfName.FromLatin1("Predictor"),
                                new PdfInteger(1)
                            }
                        }
                    }
                },
                Compress(data));

        Assert.Equal(
            data,
            PdfContentStreamMutationCodec.Decode(
                stream));

        Assert.Equal(
            data,
            Decompress(
                PdfContentStreamMutationCodec.Encode(
                    stream,
                    data)));
    }

    [Fact]
    public void PredictiveFlateIsRejectedRatherThanNormalized()
    {
        var stream =
            new PdfStream(
                new PdfDictionary
                {
                    {
                        PdfName.FromLatin1("Filter"),
                        PdfName.FromLatin1("FlateDecode")
                    },
                    {
                        PdfName.FromLatin1("DecodeParms"),
                        new PdfDictionary
                        {
                            {
                                PdfName.FromLatin1("Predictor"),
                                new PdfInteger(12)
                            }
                        }
                    }
                },
                Compress(
                    Encoding.ASCII.GetBytes(
                        "data")));

        Assert.Throws<NotSupportedException>(
            () =>
                PdfContentStreamMutationCodec.Decode(
                    stream));
    }

    [Fact]
    public void MultiFilterChainIsRejected()
    {
        var stream =
            new PdfStream(
                new PdfDictionary
                {
                    {
                        PdfName.FromLatin1("Filter"),
                        new PdfArray(
                        [
                            PdfName.FromLatin1("ASCII85Decode"),
                            PdfName.FromLatin1("FlateDecode")
                        ])
                    }
                },
                Encoding.ASCII.GetBytes(
                    "ignored"));

        Assert.Throws<NotSupportedException>(
            () =>
                PdfContentStreamMutationCodec.Decode(
                    stream));
    }

    [Fact]
    public void NonFlateFilterIsRejected()
    {
        var stream =
            new PdfStream(
                new PdfDictionary
                {
                    {
                        PdfName.FromLatin1("Filter"),
                        PdfName.FromLatin1("LZWDecode")
                    }
                },
                Encoding.ASCII.GetBytes(
                    "ignored"));

        Assert.Throws<NotSupportedException>(
            () =>
                PdfContentStreamMutationCodec.Decode(
                    stream));
    }

    private static byte[] Compress(
        ReadOnlySpan<byte> data)
    {
        using var destination =
            new MemoryStream();

        using (var zlib =
            new ZLibStream(
                destination,
                CompressionLevel.Optimal,
                leaveOpen: true))
        {
            zlib.Write(
                data);
        }

        return destination.ToArray();
    }

    private static byte[] Decompress(
        byte[] data)
    {
        using var source =
            new MemoryStream(
                data,
                writable: false);

        using var zlib =
            new ZLibStream(
                source,
                CompressionMode.Decompress);

        using var destination =
            new MemoryStream();

        zlib.CopyTo(
            destination);

        return destination.ToArray();
    }
}
