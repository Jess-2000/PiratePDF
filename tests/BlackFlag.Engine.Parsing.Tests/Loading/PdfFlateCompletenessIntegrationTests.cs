using System.IO;
using System.IO.Compression;
using BlackFlag.Engine.Filters;
using BlackFlag.Engine.Objects;
using BlackFlag.Engine.Parsing.CrossReferences;
using BlackFlag.Engine.Parsing.ObjectStreams;
using BlackFlag.Engine.Rendering.Fonts;

namespace BlackFlag.Engine.Parsing.Tests.Loading;

public sealed class PdfFlateCompletenessIntegrationTests
{
    [Fact]
    public void PartialObjectStreamMustNotPublishShortenedInteger()
    {
        byte[] complete = Compress("2 0 42"u8);
        var dictionary = new PdfDictionary(new Dictionary<PdfName, PdfObject>
        {
            [new("Type")] = new PdfName("ObjStm"),
            [new("N")] = new PdfInteger(1),
            [new("First")] = new PdfInteger(4),
            [new("Filter")] = new PdfName("FlateDecode")
        });
        var parser = new PdfObjectStreamParser(Pipeline(), new());
        Assert.Equal(42, Assert.IsType<PdfInteger>(parser.Parse(new(dictionary, complete)).GetByIndex(0)).Value);
        // Stored-block prefix contains the complete header and only "2 0 4".
        byte[] partial = complete[..12];
        var stream = new PdfStream(dictionary, partial);
        AssertInvalidZlib(Assert.Throws<PdfFilterDecodeException>(() => parser.Parse(stream)));
        Assert.Equal(partial, stream.Data.ToArray());
        Assert.Same(dictionary, stream.Dictionary);
    }

    [Fact]
    public void XrefWithMissingTrailerMustNotPublishOffsets()
    {
        byte[] complete = Compress([1, 0, 42, 7]);
        var dictionary = new PdfDictionary(new Dictionary<PdfName, PdfObject>
        {
            [new("Type")] = new PdfName("XRef"),
            [new("Size")] = new PdfInteger(1),
            [new("W")] = new PdfArray([new PdfInteger(1), new PdfInteger(2), new PdfInteger(1)]),
            [new("Filter")] = new PdfName("FlateDecode")
        });
        var parser = new PdfCrossReferenceStreamParser(Pipeline(), new());
        Assert.Single(parser.Parse(100, new(dictionary, complete)).Sections);
        AssertInvalidZlib(Assert.Throws<PdfFilterDecodeException>(() =>
            parser.Parse(100, new(dictionary, complete[..^1]))));
    }

    [Fact]
    public void CMapWithMissingTrailerMustNotPublishMapping()
    {
        byte[] complete = Compress("1 begincodespacerange <00> <FF> endcodespacerange 1 beginbfchar <41> <0041> endbfchar"u8);
        var dictionary = new PdfDictionary(new Dictionary<PdfName, PdfObject>
        {
            [new("Filter")] = new PdfArray([new PdfName("FlateDecode")]),
            [new("DecodeParms")] = new PdfArray([PdfNull.Instance])
        });
        var decoder = new PdfFontStreamDecoder();
        var map = PdfCMapParser.Parse(decoder.Decode(new(dictionary, complete)).Span);
        Assert.True(map.TryGetUnicode(new PdfCharacterCode([0x41]), out string text));
        Assert.Equal("A", text);
        AssertInvalidZlib(Assert.Throws<PdfFilterDecodeException>(() =>
            decoder.Decode(new(dictionary, complete[..^1]))));
    }

    private static void AssertInvalidZlib(PdfFilterDecodeException exception)
    {
        Assert.Equal(PdfFilterDecodeFailure.InvalidData, exception.Failure);
        Assert.Equal("The FlateDecode stream contains invalid or incomplete zlib data.", exception.Message);
    }

    private static PdfFilterPipeline Pipeline() => new(PdfFilterRegistrations.CreateDefault());

    private static byte[] Compress(ReadOnlySpan<byte> bytes)
    {
        using var output = new MemoryStream();
        using (var zlib = new ZLibStream(output, CompressionLevel.NoCompression, true)) zlib.Write(bytes);
        return output.ToArray();
    }
}
