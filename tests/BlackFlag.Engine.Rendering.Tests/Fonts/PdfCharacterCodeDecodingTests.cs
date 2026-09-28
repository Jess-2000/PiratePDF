#nullable enable

using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using BlackFlag.Engine.Objects;
using BlackFlag.Engine.Rendering.DisplayList;
using BlackFlag.Engine.Rendering.Fonts;
using BlackFlag.Engine.Rendering.Graphics;
using BlackFlag.Engine.Rendering.Resources;

namespace BlackFlag.Engine.Rendering.Tests.Fonts;

public sealed class PdfCharacterCodeDecodingTests
{
    private const string VariableMap = """
        2 begincodespacerange <00> <7F> <8000> <80FF> endcodespacerange
        2 begincidchar <41> 7 <80AB> 300 endcidchar
        2 beginbfchar <41> <0041> <80AB> <03A9> endbfchar
        """;

    [Fact]
    public void SimpleFontPreservesOneByteUnicodeAndOrder()
    {
        const string map = """
            1 begincodespacerange <00> <FF> endcodespacerange
            2 beginbfchar <41> <0041> <80> <20AC> endbfchar
            """;
        var run = CreateRun("TrueType", new PdfName("WinAnsiEncoding"), map, [0x80, 0x41]);
        PdfDecodedTextRun result = new PdfFontDecoder().Decode(run);

        Assert.Empty(result.Diagnostics);
        Assert.Collection(result.Glyphs,
            glyph => AssertGlyph(glyph, [0x80], null, null, "\u20AC", 0, 1),
            glyph => AssertGlyph(glyph, [0x41], null, null, "A", 1, 1));
    }

    [Fact]
    public void CompositeFontPreservesVariableLengthCidUnicodeAndPosition()
    {
        byte[] input = [0x41, 0x80, 0xAB, 0x41];
        PdfCMap map = PdfCMapParser.Parse(Encoding.ASCII.GetBytes(VariableMap));
        PdfCharacterCode publicCode = map.ReadCode(input, 1);
        MemoryMarshal.AsMemory(publicCode.Bytes).Span[1] = 0xAC;
        Assert.Equal("80AB", publicCode.ToString());
        Assert.True(map.TryGetCid(publicCode, out int cid));
        Assert.Equal(300, cid);
        Assert.True(map.TryGetUnicode(publicCode, out string? unicode));
        Assert.Equal("\u03A9", unicode);
        Assert.False(map.TryGetCid(new PdfCharacterCode([0x80, 0xAC]), out _));

        var run = CreateRun("Type0", Stream(VariableMap), VariableMap, input);
        var decoder = new PdfFontDecoder();
        for (int iteration = 0; iteration < 2; iteration++)
        {
            PdfDecodedTextRun result = decoder.Decode(run);
            Assert.Empty(result.Diagnostics);
            Assert.Collection(result.Glyphs,
                glyph => AssertGlyph(glyph, [0x41], 7, 7, "A", 0, 11),
                glyph => AssertGlyph(glyph, [0x80, 0xAB], 300, 300, "\u03A9", 11, 11),
                glyph => AssertGlyph(glyph, [0x41], 7, 7, "A", 22, 11));
        }
    }

    [Theory]
    [InlineData("Identity-H")]
    [InlineData("Identity-V")]
    public void IdentityEncodingPreservesBigEndianCidFallback(string encoding)
    {
        const string unicodeMap = """
            1 begincodespacerange <0000> <FFFF> endcodespacerange
            2 beginbfchar <0041> <0041> <012C> <03A9> endbfchar
            """;
        var run = CreateRun("Type0", new PdfName(encoding), unicodeMap, [0, 0x41, 1, 0x2C]);
        PdfDecodedTextRun result = new PdfFontDecoder().Decode(run);

        Assert.Empty(result.Diagnostics);
        Assert.Collection(result.Glyphs,
            glyph => AssertGlyph(glyph, [0, 0x41], 65, 65, "A", 0, 11),
            glyph => AssertGlyph(glyph, [1, 0x2C], 300, 300, "\u03A9", 11, 11));
    }

    [Theory]
    [InlineData("4180")]
    [InlineData("41FF")]
    public void CompositeMalformedCodeRetainsFailureAtSourceOffset(string hex)
    {
        var run = CreateRun("Type0", Stream(VariableMap), VariableMap, Convert.FromHexString(hex));
        var exception = Assert.Throws<InvalidDataException>(() => new PdfFontDecoder().Decode(run));
        Assert.Equal("No CMap codespace accepts the character code beginning at byte offset 1.", exception.Message);
    }

    private static PdfDisplayTextRun CreateRun(
        string subtype, PdfObject encoding, string unicodeMap, byte[] bytes)
    {
        var font = new PdfResolvedFont(
            PdfResourceIdentity.ForLocal(1), new PdfDictionary([]), subtype, "Test", "Test Regular",
            400, 5, PdfFontSlant.Upright, PdfFontTechnology.TrueType,
            PdfFontEmbeddingRights.Unknown, false, encoding, Stream(unicodeMap),
            null, null, null, []);
        return new PdfDisplayTextRun(
            font, 10, [PdfTextRunElement.CreateText(bytes)], PdfMatrix.Identity,
            new PdfGraphicsStateSnapshot { Font = font, FontSize = 10, CharacterSpacing = 1 },
            new PdfContentSourceSpan(0, 0, 1), null);
    }

    private static PdfStream Stream(string text) => new(new PdfDictionary([]), Encoding.ASCII.GetBytes(text));

    private static void AssertGlyph(
        PdfDecodedGlyph glyph, byte[] source, int? cid, ushort? glyphId,
        string unicode, double x, double advance)
    {
        Assert.Equal(source, glyph.SourceCode.ToArray());
        Assert.Equal(cid, glyph.Cid);
        Assert.Equal(glyphId, glyph.GlyphId);
        Assert.Equal(unicode, glyph.UnicodeText);
        Assert.Equal(x, glyph.X);
        Assert.Equal(advance, glyph.Advance);
    }
}
