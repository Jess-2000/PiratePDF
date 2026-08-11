namespace BlackFlag.Engine.Rendering.Color;
/// <summary>Represents straight-alpha sRGB output components.</summary>
public readonly record struct PdfSrgbColor(double Red, double Green, double Blue, double Alpha = 1)
{
    /// <summary>Returns a component clamped to the unit interval.</summary>
    public static double Clamp(double value) => Math.Clamp(value, 0, 1);
}
