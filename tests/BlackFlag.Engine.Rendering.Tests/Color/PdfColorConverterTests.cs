using BlackFlag.Engine.Rendering.Color;
using BlackFlag.Engine.Rendering.DisplayList;
namespace BlackFlag.Engine.Rendering.Tests.Color;
public sealed class PdfColorConverterTests
{
[Fact] public void ConvertsDeviceRgbToSrgb(){var c=new PdfColorConverter().Convert(new PdfColorValue("DeviceRGB",[0.25,0.5,0.75]),PdfColorTarget.Srgb);Assert.Equal(0.25,c.Red,6);Assert.Equal(0.5,c.Green,6);Assert.Equal(0.75,c.Blue,6);}
[Fact] public void ConvertsDeviceCmykToSrgbPreview(){var c=new PdfColorConverter().Convert(new PdfColorValue("DeviceCMYK",[0,1,1,0]),PdfColorTarget.Srgb);Assert.Equal(1,c.Red,6);Assert.Equal(0,c.Green,6);Assert.Equal(0,c.Blue,6);}
[Theory][InlineData("Multiply",PdfBlendMode.Multiply)][InlineData("Luminosity",PdfBlendMode.Luminosity)] public void ParsesBlendModes(string name,PdfBlendMode expected)=>Assert.Equal(expected,PdfBlendModeParser.Parse(new BlackFlag.Engine.Objects.PdfName(name)));
}
