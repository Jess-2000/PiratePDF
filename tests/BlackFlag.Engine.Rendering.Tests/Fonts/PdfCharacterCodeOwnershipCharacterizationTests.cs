using System.Runtime.InteropServices;
using BlackFlag.Engine.Rendering.Fonts;

namespace BlackFlag.Engine.Rendering.Tests.Fonts;

// Characterizes an unfixed ownership candidate. A future approved fix must invert
// the mutation expectations, rather than treating the defect as desired behavior.
public sealed class PdfCharacterCodeOwnershipCharacterizationTests
{
    [Fact]
    public void PublicAliasChangesEqualityAndInvalidatesStoredKeys()
    {
        byte[] input = [0x41];
        var code = new PdfCharacterCode(input);
        input[0] = 0x42;
        Assert.Equal(new PdfCharacterCode([0x41]), code);
        int originalHash = code.GetHashCode();
        var dictionary = new Dictionary<PdfCharacterCode, int> { [code] = 1 };
        var set = new HashSet<PdfCharacterCode> { code };
        Assert.True(MemoryMarshal.TryGetArray(code.Bytes, out ArraySegment<byte> alias));

        // Select a different hash instead of assuming that two particular inputs
        // cannot collide under the process-randomized HashCode implementation.
        for (int value = 0; value <= byte.MaxValue && code.GetHashCode() == originalHash; value++)
        {
            alias.Array![alias.Offset] = (byte)value;
        }

        Assert.NotEqual(originalHash, code.GetHashCode());
        Assert.NotEqual(new PdfCharacterCode([0x41]), code);
        var equivalent = new PdfCharacterCode(code.Bytes.Span);
        Assert.Equal(equivalent, code);
        Assert.False(dictionary.ContainsKey(code));
        Assert.False(dictionary.ContainsKey(equivalent));
        // Exercise the set's hashed lookup, not an assertion that enumerates values.
        bool foundInSet = set.Contains(code);
        Assert.False(foundInSet);
        dictionary.Add(equivalent, 2);
        Assert.Equal(2, dictionary.Count);
        Assert.Equal(dictionary.Keys.First(), dictionary.Keys.Last());
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
        MemoryMarshal.AsMemory(code.Bytes).Span[0] = 0x42;
        Assert.False(cmap.TryGetCid(code, out _));
        Assert.True(cmap.TryGetCid(new PdfCharacterCode([0x41]), out cid));
        Assert.Equal(7, cid);
        Assert.True(cmap.TryGetUnicode(new PdfCharacterCode([0x41]), out text));
        Assert.Equal("A", text);
    }
}
