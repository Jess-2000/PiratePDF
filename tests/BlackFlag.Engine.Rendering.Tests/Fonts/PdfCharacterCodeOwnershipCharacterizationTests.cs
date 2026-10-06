using System.Runtime.InteropServices;
using BlackFlag.Engine.Rendering.Fonts;

namespace BlackFlag.Engine.Rendering.Tests.Fonts;

// Retains the original ownership reproduction and CMap control as F4 regressions.
public sealed class PdfCharacterCodeOwnershipCharacterizationTests
{
    [Theory]
    [InlineData("41")]
    [InlineData("80AB")]
    public void PublicSnapshotsPreserveEqualityAndStoredKeys(string hex)
    {
        byte[] input = Convert.FromHexString(hex);
        var code = new PdfCharacterCode(input);
        var equivalent = new PdfCharacterCode(input);
        int originalHash = code.GetHashCode();
        var dictionary = new Dictionary<PdfCharacterCode, int> { [code] = 1 };
        var set = new HashSet<PdfCharacterCode> { code };
        input[0] ^= 0xFF;
        Assert.Equal(hex, code.ToString());
        Assert.Equal(equivalent, code);
        Assert.Equal(originalHash, code.GetHashCode());
        Assert.Equal(1, dictionary[equivalent]);
        Assert.Equal(Convert.FromHexString(hex), code.Bytes.ToArray());

        ReadOnlyMemory<byte> first = code.Bytes;
        ReadOnlyMemory<byte> second = code.Bytes;
        Assert.True(MemoryMarshal.TryGetArray(first, out ArraySegment<byte> snapshot));
        snapshot.Array![snapshot.Offset] ^= 0xFF;
        Assert.NotEqual(hex, Convert.ToHexString(first.Span));
        Assert.Equal(hex, Convert.ToHexString(second.Span));
        Assert.Equal(hex, Convert.ToHexString(code.Bytes.Span));
        Assert.Equal(hex, code.ToString());
        Assert.Equal(equivalent, code);
        Assert.True(code.Equals((object)equivalent));
        Assert.False(code.Equals(new PdfCharacterCode(first.Span)));
        Assert.Equal(originalHash, code.GetHashCode());
        Assert.Equal(equivalent.GetHashCode(), code.GetHashCode());
        Assert.Equal(1, dictionary[code]);
        Assert.Equal(1, dictionary[equivalent]);
        Assert.Throws<ArgumentException>(() => dictionary.Add(equivalent, 2));
        Assert.Single(dictionary);
        // Use hashed lookup explicitly; an assertion that enumerates can hide corruption.
        bool containsOriginal = set.Contains(code);
        bool containsEquivalent = set.Contains(equivalent);
        Assert.True(containsOriginal);
        Assert.True(containsEquivalent);
        Assert.False(set.Add(equivalent));
        Assert.Single(set);

        MemoryMarshal.AsMemory(second).Span[0] ^= 0xFF;
        Assert.Equal(hex, code.ToString());
        Assert.Equal(hex, Convert.ToHexString(code.Bytes.Span));
    }

    [Fact]
    public void EmptyCodeRetainsConstructorRejection()
    {
        var exception = Assert.Throws<ArgumentException>(() => new PdfCharacterCode([]));
        Assert.Equal("bytes", exception.ParamName);
        Assert.StartsWith("A PDF character code cannot be empty.", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void MutatingPublicLookupCodeDoesNotMutatePrivateCMapKeys()
    {
        var cmap = PdfCMapParser.Parse(
            "1 begincodespacerange <00> <FF> endcodespacerange 1 begincidchar <41> 7 endcidchar 1 beginbfchar <41> <0041> endbfchar"u8);
        var code = cmap.ReadCode([0x41], 0);
        Assert.True(cmap.TryGetCid(code, out int cid));
        Assert.Equal(7, cid);
        Assert.True(cmap.TryGetUnicode(code, out string text));
        Assert.Equal("A", text);
        ReadOnlyMemory<byte> snapshot = code.Bytes;
        MemoryMarshal.AsMemory(snapshot).Span[0] = 0x42;
        Assert.True(cmap.TryGetCid(code, out cid));
        Assert.Equal(7, cid);
        Assert.True(cmap.TryGetUnicode(code, out text));
        Assert.Equal("A", text);
        var changedQuery = new PdfCharacterCode(snapshot.Span);
        Assert.False(cmap.TryGetCid(changedQuery, out _));
        Assert.False(cmap.TryGetUnicode(changedQuery, out _));
        Assert.Equal(code, cmap.ReadCode([0x41], 0));
        Assert.True(cmap.TryGetCid(new PdfCharacterCode([0x41]), out cid));
        Assert.Equal(7, cid);
        Assert.True(cmap.TryGetUnicode(new PdfCharacterCode([0x41]), out text));
        Assert.Equal("A", text);
    }
}
