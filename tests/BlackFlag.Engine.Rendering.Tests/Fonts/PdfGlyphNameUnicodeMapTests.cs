#nullable enable
using BlackFlag.Engine.Rendering.Fonts;

namespace BlackFlag.Engine.Rendering.Tests.Fonts;

public sealed class PdfGlyphNameUnicodeMapTests
{
    [Theory]
    [InlineData("bullet", "\u2022")]
    [InlineData("blacksquare", "\u25A0")]
    [InlineData("arrowright", "\u2192")]
    [InlineData("endash", "\u2013")]
    [InlineData("emdash", "\u2014")]
    [InlineData("uni2022", "\u2022")]
    public void CommonListAndSymbolGlyphNamesMapToUnicode(
        string glyphName,
        string expected)
    {
        Assert.True(PdfGlyphNameUnicodeMap.TryGetUnicode(glyphName, out string? actual));
        Assert.Equal(expected, actual);
    }
}
