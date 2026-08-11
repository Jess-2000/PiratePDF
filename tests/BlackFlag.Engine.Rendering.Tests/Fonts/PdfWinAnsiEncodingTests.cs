using BlackFlag.Engine.Rendering.Fonts;

namespace BlackFlag.Engine.Rendering.Tests.Fonts;

public sealed class PdfWinAnsiEncodingTests
{
    [Theory]
    [InlineData(0x95, "\u2022")]
    [InlineData(0x96, "\u2013")]
    [InlineData(0x97, "\u2014")]
    [InlineData(0x41, "A")]
    public void MapsCommonListAndTextCodes(byte code, string expected)
    {
        Assert.Equal(expected, PdfWinAnsiEncoding.GetUnicode(code));
    }
}
