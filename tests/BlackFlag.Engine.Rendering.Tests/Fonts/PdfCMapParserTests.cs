#nullable enable

using System.Text;
using BlackFlag.Engine.Rendering.Fonts;

namespace BlackFlag.Engine.Rendering.Tests.Fonts;

public sealed class PdfCMapParserTests
{
    [Fact]
    public void ParseMapsUnicodeAndCidRanges()
    {
        const string cmap = """
            1 begincodespacerange
            <00> <FF>
            endcodespacerange
            2 beginbfchar
            <41> <0041>
            <95> <2022>
            endbfchar
            1 begincidrange
            <20> <22> 100
            endcidrange
            """;

        PdfCMap result = PdfCMapParser.Parse(Encoding.ASCII.GetBytes(cmap));

        Assert.True(result.TryGetUnicode(new PdfCharacterCode([0x95]), out string? bullet));
        Assert.Equal("\u2022", bullet);
        Assert.True(result.TryGetCid(new PdfCharacterCode([0x21]), out int cid));
        Assert.Equal(101, cid);
    }

    [Fact]
    public void ParseExpandsUnicodeRange()
    {
        const string cmap = """
            1 begincodespacerange
            <00> <FF>
            endcodespacerange
            1 beginbfrange
            <30> <32> <0030>
            endbfrange
            """;

        PdfCMap result = PdfCMapParser.Parse(Encoding.ASCII.GetBytes(cmap));

        Assert.True(result.TryGetUnicode(new PdfCharacterCode([0x32]), out string? text));
        Assert.Equal("2", text);
    }
    [Fact]
    public void CodeSpacesAreImmutableAndReadCodeUsesLongestDeclaredLength()
    {
        const string cmap = """
            2 begincodespacerange
            <00> <7F>
            <8000> <80FF>
            endcodespacerange
            """;

        PdfCMap result = PdfCMapParser.Parse(Encoding.ASCII.GetBytes(cmap));

        Assert.Equal(2, result.CodeSpaces.Count);
        Assert.Equal(2, result.ReadCode([0x80, 0x2A], 0).Bytes.Length);
        Assert.Throws<NotSupportedException>(() =>
            ((IList<PdfCodeSpaceRange>)result.CodeSpaces)[0] = result.CodeSpaces[0]);
    }

}
