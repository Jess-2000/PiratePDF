#nullable enable
using System.IO;
using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Threading;
using BlackFlag.Engine.Filters;
using BlackFlag.Engine.Objects;
using BlackFlag.Engine.Parsing.CrossReferences;
using BlackFlag.Engine.Parsing.ObjectStreams;
using BlackFlag.Engine.Rendering.Fonts;

namespace BlackFlag.Engine.Parsing.Tests.Loading;

public sealed class PdfStreamOwnershipIntegrationTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ObjectStreamParsingKeepsOriginalDataAfterSnapshotAndOutputMutation(bool flate)
    {
        byte[] decoded = "2 0 42"u8.ToArray();
        byte[] encoded = flate ? Compress(decoded) : decoded;
        var entries = new Dictionary<PdfName, PdfObject>
        {
            [new("Type")] = new PdfName("ObjStm"),
            [new("N")] = new PdfInteger(1),
            [new("First")] = new PdfInteger(4),
            [new("Length")] = new PdfInteger(encoded.Length)
        };
        if (flate)
        {
            entries.Add(new("Filter"), new PdfArray([new PdfName("FlateDecode")]));
            entries.Add(new("DecodeParms"), new PdfArray([new PdfDictionary(new Dictionary<PdfName, PdfObject>
            {
                [new("Predictor")] = new PdfInteger(1),
                [new("Private")] = new PdfInteger(23)
            })]));
        }

        var stream = new PdfStream(new PdfDictionary(entries), encoded);
        var parser = new PdfObjectStreamParser(DefaultPipeline(), new PdfFilterDecodeLimits());
        PdfCompressedObjects before = parser.Parse(stream);
        ReadOnlyMemory<byte> snapshot = stream.Data;
        MemoryMarshal.AsMemory(snapshot).Span.Fill(0xFF);
        ReadOnlyMemory<byte> output = new PdfFontStreamDecoder().Decode(stream);
        Assert.Equal(decoded, output.ToArray());
        MemoryMarshal.AsMemory(output).Span.Fill(0);
        Assert.Equal(42, Assert.IsType<PdfInteger>(before.GetByIndex(0)).Value);
        Assert.Equal(42, Assert.IsType<PdfInteger>(parser.Parse(stream).GetByIndex(0)).Value);
        Assert.Equal(encoded, stream.Data.ToArray());
        Assert.Equal(encoded.Length, stream.Length);
        Assert.Same(before.Dictionary, stream.Dictionary);
    }

    [Fact]
    public void CallerSuppliedPipelineCannotMutateRetainedObjectStream()
    {
        var stream = new PdfStream(new PdfDictionary(new Dictionary<PdfName, PdfObject>
        {
            [new("Type")] = new PdfName("ObjStm"),
            [new("N")] = new PdfInteger(1),
            [new("First")] = new PdfInteger(4)
        }), "2 0 42"u8.ToArray());
        var hostile = new MutatingPipeline();
        var parser = new PdfObjectStreamParser(hostile, new PdfFilterDecodeLimits());
        Assert.Equal(42, Assert.IsType<PdfInteger>(parser.Parse(stream).GetByIndex(0)).Value);
        MemoryMarshal.AsMemory(hostile.Retained).Span.Fill(0);
        Assert.Equal("2 0 42"u8.ToArray(), stream.Data.ToArray());
        Assert.Equal(42, Assert.IsType<PdfInteger>(parser.Parse(stream).GetByIndex(0)).Value);
    }

    [Fact]
    public void XrefStreamSnapshotMutationCannotChangeParsedOffsetsOrGeneration()
    {
        var dictionary = new PdfDictionary(new Dictionary<PdfName, PdfObject>
        {
            [new("Type")] = new PdfName("XRef"),
            [new("Size")] = new PdfInteger(1),
            [new("W")] = new PdfArray([new PdfInteger(1), new PdfInteger(2), new PdfInteger(1)])
        });
        var stream = new PdfStream(dictionary, new byte[] { 1, 0, 42, 7 });
        var parser = new PdfCrossReferenceStreamParser(DefaultPipeline(), new PdfFilterDecodeLimits());
        var before = parser.Parse(100, stream);
        MemoryMarshal.AsMemory(stream.Data).Span[2] = 43;
        var after = parser.Parse(100, stream);
        Assert.Equal(before.Sections.SelectMany(s => s.Entries).ToArray(),
            after.Sections.SelectMany(s => s.Entries).ToArray());
        Assert.Equal(new byte[] { 1, 0, 42, 7 }, stream.Data.ToArray());
        Assert.Same(dictionary, stream.Dictionary);
    }

    [Theory]
    [InlineData("malformed")]
    [InlineData("decoded-limit")]
    [InlineData("expansion-limit")]
    [InlineData("cancelled")]
    public void SnapshotMutationPreservesDecodeFailureContract(string scenario)
    {
        byte[] encoded = scenario == "malformed" ? new byte[] { 1, 2, 3, 4, 5 } : Compress(new byte[4096]);
        var stream = new PdfStream(new PdfDictionary(new Dictionary<PdfName, PdfObject>
        {
            [new("Filter")] = new PdfName("FlateDecode")
        }), encoded);
        var limits = new PdfFilterDecodeLimits(
            scenario == "decoded-limit" ? 1 : 8192,
            scenario == "expansion-limit" ? 1 : 1000, 8, 1024);
        var decoder = new PdfFontStreamDecoder(DefaultPipeline(), limits);
        using var cancellation = new CancellationTokenSource();
        if (scenario == "cancelled") cancellation.Cancel();
        Exception before = Assert.ThrowsAny<Exception>(() => decoder.Decode(stream, cancellation.Token));
        MemoryMarshal.AsMemory(stream.Data).Span.Fill(0xFF);
        Exception after = Assert.ThrowsAny<Exception>(() => decoder.Decode(stream, cancellation.Token));
        Assert.Equal(before.GetType(), after.GetType());
        Assert.Equal(before.Message, after.Message);
        if (scenario == "cancelled")
        {
            Assert.Equal(cancellation.Token, Assert.IsType<OperationCanceledException>(after).CancellationToken);
        }
        else
        {
            PdfFilterDecodeFailure expected = scenario switch
            {
                "malformed" => PdfFilterDecodeFailure.InvalidData,
                "decoded-limit" => PdfFilterDecodeFailure.DecodedByteLimitExceeded,
                _ => PdfFilterDecodeFailure.ExpansionRatioExceeded
            };
            Assert.Equal(expected, Assert.IsType<PdfFilterDecodeException>(before).Failure);
            Assert.Equal(Assert.IsType<PdfFilterDecodeException>(before).Failure,
                Assert.IsType<PdfFilterDecodeException>(after).Failure);
        }
        Assert.Equal(encoded, stream.Data.ToArray());
    }

    private static PdfFilterPipeline DefaultPipeline() => new(PdfFilterRegistrations.CreateDefault());

    private static byte[] Compress(byte[] input)
    {
        using var output = new MemoryStream();
        using (var compressor = new ZLibStream(output, CompressionLevel.Optimal, leaveOpen: true))
        {
            compressor.Write(input);
        }
        return output.ToArray();
    }

    private sealed class MutatingPipeline : IPdfFilterPipeline
    {
        public ReadOnlyMemory<byte> Retained { get; private set; }
        public ReadOnlyMemory<byte> Decode(ReadOnlyMemory<byte> encodedData,
            IReadOnlyList<PdfName> filters, IReadOnlyList<PdfDictionary?>? decodeParameters,
            PdfFilterDecodeLimits limits, CancellationToken cancellationToken = default)
        {
            byte[] output = encodedData.ToArray();
            Retained = encodedData;
            MemoryMarshal.AsMemory(encodedData).Span[^1] = (byte)'3';
            return output;
        }
    }
}
