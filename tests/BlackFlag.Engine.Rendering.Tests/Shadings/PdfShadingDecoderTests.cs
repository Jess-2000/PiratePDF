
using BlackFlag.Engine.Objects;
using BlackFlag.Engine.Rendering.Resources;
using BlackFlag.Engine.Rendering.Shadings;

namespace BlackFlag.Engine.Rendering.Tests.Shadings;

public sealed class PdfShadingDecoderTests
{
    [Fact]
    public void DecodesAxialDeviceRgbShading()
    {
        PdfDictionary function = new(
        [
            new(new PdfName("FunctionType"), new PdfInteger(2)),
            new(new PdfName("Domain"), new PdfArray([new PdfInteger(0), new PdfInteger(1)])),
            new(new PdfName("C0"), new PdfArray([new PdfInteger(1), new PdfInteger(0), new PdfInteger(0)])),
            new(new PdfName("C1"), new PdfArray([new PdfInteger(0), new PdfInteger(0), new PdfInteger(1)])),
            new(new PdfName("N"), new PdfInteger(1))
        ]);

        PdfDictionary dictionary = new(
        [
            new(new PdfName("ShadingType"), new PdfInteger(2)),
            new(new PdfName("ColorSpace"), new PdfName("DeviceRGB")),
            new(new PdfName("Coords"), new PdfArray([
                new PdfInteger(0), new PdfInteger(0),
                new PdfInteger(100), new PdfInteger(0)
            ])),
            new(new PdfName("Function"), function),
            new(new PdfName("Extend"), new PdfArray([new PdfBoolean(true), new PdfBoolean(true)]))
        ]);

        var shading = new PdfResolvedShading(
            PdfResourceIdentity.ForLocal(1),
            dictionary,
            2,
            new PdfName("DeviceRGB"),
            function);

        PdfShadingDescriptor decoded = new PdfShadingDecoder().Decode(shading);

        Assert.Equal(2, decoded.ShadingType);
        Assert.Equal(33, decoded.Stops.Count);
        Assert.True(decoded.ExtendStart);
        Assert.True(decoded.ExtendEnd);
        Assert.True(decoded.Stops[0].Color.Red > decoded.Stops[0].Color.Blue);
        Assert.True(decoded.Stops[^1].Color.Blue > decoded.Stops[^1].Color.Red);
    }
}
