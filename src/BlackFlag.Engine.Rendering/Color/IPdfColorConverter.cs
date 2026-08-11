using BlackFlag.Engine.Rendering.DisplayList;
/// <summary>Converts preserved PDF color semantics to a requested render target.</summary>
namespace BlackFlag.Engine.Rendering.Color;
public interface IPdfColorConverter
{
    /// <summary>Converts one canonical color value.</summary>
    PdfSrgbColor Convert(PdfColorValue color, PdfColorTarget target, double alpha = 1);
}
