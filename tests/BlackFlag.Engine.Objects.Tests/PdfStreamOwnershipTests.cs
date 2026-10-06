using System.Runtime.InteropServices;
using Xunit;

namespace BlackFlag.Engine.Objects.Tests;

public sealed class PdfStreamOwnershipTests
{
    [Fact]
    public void ConstructorAndRetainedSnapshotsCannotChangeValueOrHashKeys()
    {
        byte[] input = [1, 2, 3];
        var dictionary = new PdfDictionary(new Dictionary<PdfName, PdfObject>
        {
            [new PdfName("Length")] = new PdfInteger(99)
        });
        var stream = new PdfStream(dictionary, input);
        var equivalent = new PdfStream(dictionary, input);
        var lookup = new Dictionary<PdfStream, string> { [stream] = "retained" };
        var set = new HashSet<PdfStream> { stream };
        int hash = stream.GetHashCode();
        ReadOnlyMemory<byte> first = stream.Data;
        ReadOnlyMemory<byte> second = stream.Data;
        input[0] = 8;
        MemoryMarshal.AsMemory(first).Span.Fill(9);

        Assert.Equal(new byte[] { 1, 2, 3 }, second.ToArray());
        Assert.Equal(second.ToArray(), stream.Data.ToArray());
        Assert.Equal(equivalent, stream);
        Assert.True(stream == equivalent);
        Assert.False(stream != equivalent);
        Assert.Equal(hash, stream.GetHashCode());
        Assert.Equal("retained", lookup[equivalent]);
        Assert.Contains(equivalent, set);
        Assert.False(set.Add(equivalent));
        Assert.Same(dictionary, stream.Dictionary);
        // Stored byte length must not be inferred from or normalize /Length.
        Assert.Equal(99, Assert.IsType<PdfInteger>(dictionary[new PdfName("Length")]).Value);
        Assert.Equal(3, stream.Length);
    }

    [Fact]
    public void EmptySnapshotsPreserveEmptyStreamEquality()
    {
        var stream = new PdfStream(PdfDictionary.Empty, ReadOnlyMemory<byte>.Empty);
        Assert.True(stream.Data.IsEmpty);
        Assert.Equal(0, stream.Length);
        Assert.Equal(new PdfStream(PdfDictionary.Empty, ReadOnlyMemory<byte>.Empty), stream);
        Assert.Equal(PdfObjectType.Stream, stream.Type);
    }
}
