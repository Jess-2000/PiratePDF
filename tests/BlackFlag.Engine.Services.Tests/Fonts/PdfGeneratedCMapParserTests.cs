#nullable enable

using System.Text;
using BlackFlag.Engine.Rendering.Fonts;
using BlackFlag.Engine.Services.Fonts;

namespace BlackFlag.Engine.Services.Tests.Fonts;

public sealed class PdfGeneratedCMapParserTests
{
    [Fact]
    public void GeneratedCidToUnicodeDictionaryAndSurrogatePairsParse()
    {
        byte[] bytes = PdfCidToUnicodeCMapBuilder.Build(new Dictionary<ushort, int>
        {
            [1] = 0x41,
            [2] = 0x03A9,
            [3] = 0x1F600
        });
        Assert.Contains("/CIDSystemInfo <<", Encoding.ASCII.GetString(bytes), StringComparison.Ordinal);
        PdfCMap cmap = PdfCMapParser.Parse(bytes);

        Assert.Equal(2, cmap.ReadCode([0, 1], 0).Bytes.Length);
        AssertUnicode(cmap, [0, 1], "A");
        AssertUnicode(cmap, [0, 2], "\u03A9");
        AssertUnicode(cmap, [0, 3], "\U0001F600");
    }

    [Fact]
    public void GeneratedSimpleFontToUnicodeParses()
    {
        byte[] bytes = PdfToUnicodeCMapBuilder.Build(new Dictionary<byte, int>
        {
            [65] = 65,
            [128] = 0x20AC
        });
        PdfCMap cmap = PdfCMapParser.Parse(bytes);
        AssertUnicode(cmap, [65], "A");
        AssertUnicode(cmap, [128], "\u20AC");
    }

    [Fact]
    public void EntireTwoByteCidSpaceFitsDefaultTokenBudget()
    {
        var mappings = Enumerable.Range(0, ushort.MaxValue + 1)
            .ToDictionary(cid => (ushort)cid, _ => 65);
        byte[] bytes = PdfCidToUnicodeCMapBuilder.Build(mappings);
        Assert.True(PdfCMapParser.Tokenize(Encoding.ASCII.GetString(bytes),
            PdfCMapParser.MaximumTokenCount).Count() < 140_000);
        PdfCMap cmap = PdfCMapParser.Parse(bytes);
        foreach (ushort cid in mappings.Keys)
        {
            AssertUnicode(cmap, [(byte)(cid >> 8), (byte)cid], "A");
        }
    }

    private static void AssertUnicode(PdfCMap cmap, byte[] code, string expected)
    {
        Assert.True(cmap.TryGetUnicode(new PdfCharacterCode(code), out string? actual));
        Assert.Equal(expected, actual);
    }
}
