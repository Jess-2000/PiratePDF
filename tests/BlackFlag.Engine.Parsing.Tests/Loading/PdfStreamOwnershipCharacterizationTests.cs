using System.Runtime.InteropServices;
using System.Text;
using BlackFlag.Engine.Filters;
using BlackFlag.Engine.Objects;
using BlackFlag.Engine.Parsing.CrossReferences;
using BlackFlag.Engine.Parsing.Loading;
using BlackFlag.Engine.Rendering.Fonts;

namespace BlackFlag.Engine.Parsing.Tests.Loading;

// Ownership regressions converted from POST-P0-013 characterization.
public sealed class PdfStreamOwnershipCharacterizationTests
{
    [Fact]
    public void ConstructorAndPublicSnapshotsPreserveEqualityAndDecodedContent()
    {
        byte[] input = "41>"u8.ToArray();
        var dictionary = new PdfDictionary(new Dictionary<PdfName, PdfObject>
        {
            [new PdfName("Length")] = new PdfInteger(3),
            [new PdfName("Filter")] = new PdfName("ASCIIHexDecode")
        });
        var stream = new PdfStream(dictionary, input);
        var original = new PdfStream(dictionary, input);
        int hash = stream.GetHashCode();
        ReadOnlyMemory<byte> first = stream.Data;
        ReadOnlyMemory<byte> second = stream.Data;
        input[0] = (byte)'5';
        Assert.Equal(original, stream);
        var decoder = new PdfFontStreamDecoder();
        byte[] before = decoder.Decode(stream).ToArray();
        Assert.Equal("A"u8.ToArray(), before);
        if (MemoryMarshal.TryGetArray(first, out ArraySegment<byte> alias))
        {
            alias.Array![alias.Offset + 1] = (byte)'2';
        }

        MemoryMarshal.AsMemory(first).Span[1] = (byte)'2';
        Assert.Equal("42>"u8.ToArray(), first.ToArray());
        Assert.Equal("41>"u8.ToArray(), second.ToArray());
        Assert.Equal("41>"u8.ToArray(), stream.Data.ToArray());
        Assert.Equal(original, stream);
        Assert.Equal(hash, stream.GetHashCode());
        Assert.Same(dictionary, stream.Dictionary);
        Assert.Equal("A"u8.ToArray(), decoder.Decode(stream).ToArray());
        Assert.Equal("A"u8.ToArray(), before);
        Assert.Equal(3, Assert.IsType<PdfInteger>(stream.Dictionary[new PdfName("Length")]).Value);
        Assert.Equal(3, stream.Data.Length);
    }

    [Fact]
    public void PublicSnapshotMutationPreservesCachedAndFreshCompressedObjects()
    {
        byte[] pdf = Encoding.ASCII.GetBytes(
            "1 0 obj\n<< /Type /ObjStm /N 1 /First 4 /Length 6 >>\nstream\n2 0 42\nendstream\nendobj");
        using var source = new TestRandomAccessSource(pdf);
        var table = new PdfObjectLocationTable([
            new[] { PdfObjectLocation.CreateDirect(new PdfObjectId(1, 0), 0),
                PdfObjectLocation.CreateCompressed(2, 1, 0) }]);
        PdfIndirectObjectResolver CreateResolver() => new(source, table,
            new PdfFilterPipeline(PdfFilterRegistrations.CreateDefault()), new PdfFilterDecodeLimits());
        var resolver = CreateResolver();
        var stream = Assert.IsType<PdfStream>(resolver.Resolve(new PdfObjectId(1, 0)));
        var cached = Assert.IsType<PdfInteger>(resolver.Resolve(new PdfObjectId(2, 0)));
        Assert.Equal(42, cached.Value);
        PdfDictionary dictionary = stream.Dictionary;
        ReadOnlyMemory<byte> snapshot = stream.Data;
        MemoryMarshal.AsMemory(snapshot).Span[5] = (byte)'3';
        Assert.Equal("2 0 43"u8.ToArray(), snapshot.ToArray());
        Assert.Same(stream, resolver.Resolve(new PdfObjectId(1, 0)));
        Assert.Equal("2 0 42"u8.ToArray(), stream.Data.ToArray());
        Assert.Same(cached, resolver.Resolve(new PdfObjectId(2, 0)));
        var parsedAgain = new BlackFlag.Engine.Parsing.ObjectStreams.PdfObjectStreamParser(
            new PdfFilterPipeline(PdfFilterRegistrations.CreateDefault()), new PdfFilterDecodeLimits()).Parse(stream);
        Assert.Equal(42, Assert.IsType<PdfInteger>(parsedAgain.GetByIndex(0)).Value);
        Assert.Same(dictionary, stream.Dictionary);
        Assert.Same(dictionary, parsedAgain.Dictionary);
        Assert.Equal(1, Assert.IsType<PdfInteger>(dictionary[new PdfName("N")]).Value);
        Assert.Equal(4, parsedAgain.FirstObjectOffset);
        Assert.Equal(2, Assert.Single(parsedAgain.Entries).ObjectNumber);
        Assert.Equal(6, stream.Data.Length);
        Assert.Equal(42, Assert.IsType<PdfInteger>(CreateResolver().Resolve(new PdfObjectId(2, 0))).Value);
        byte[] retainedSource = new byte[pdf.Length];
        Assert.Equal(pdf.Length, source.Read(0, retainedSource));
        Assert.Equal(pdf, retainedSource);
    }
}
