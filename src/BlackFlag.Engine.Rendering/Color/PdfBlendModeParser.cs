using BlackFlag.Engine.Objects;
namespace BlackFlag.Engine.Rendering.Color;
/// <summary>Normalizes PDF blend-mode objects.</summary>
public static class PdfBlendModeParser
{
    /// <summary>Parses the first recognized blend mode, defaulting null to Normal.</summary>
    public static PdfBlendMode Parse(PdfObject? value)=>value switch
    {
        null => PdfBlendMode.Normal,
        PdfName n => ParseName(n.Value),
        PdfArray a => a.OfType<PdfName>().Select(n=>TryName(n.Value)).FirstOrDefault(v=>v.HasValue) ?? PdfBlendMode.Normal,
        _ => throw new InvalidDataException("Blend mode must be a name or array of names.")
    };
    private static PdfBlendMode ParseName(string n)=>TryName(n)??throw new NotSupportedException($"Blend mode /{n} is unsupported.");
    private static PdfBlendMode? TryName(string n)=>n switch
    {"Normal" or "Compatible"=>PdfBlendMode.Normal,"Multiply"=>PdfBlendMode.Multiply,"Screen"=>PdfBlendMode.Screen,"Overlay"=>PdfBlendMode.Overlay,"Darken"=>PdfBlendMode.Darken,"Lighten"=>PdfBlendMode.Lighten,"ColorDodge"=>PdfBlendMode.ColorDodge,"ColorBurn"=>PdfBlendMode.ColorBurn,"HardLight"=>PdfBlendMode.HardLight,"SoftLight"=>PdfBlendMode.SoftLight,"Difference"=>PdfBlendMode.Difference,"Exclusion"=>PdfBlendMode.Exclusion,"Hue"=>PdfBlendMode.Hue,"Saturation"=>PdfBlendMode.Saturation,"Color"=>PdfBlendMode.Color,"Luminosity"=>PdfBlendMode.Luminosity,_=>null};
}
