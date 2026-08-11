
using BlackFlag.Engine.Rendering.Color;

namespace BlackFlag.Engine.Rendering.Shadings;

/// <summary>Represents one sampled color stop in a canonical PDF shading.</summary>
public sealed record PdfGradientStop(double Offset, PdfSrgbColor Color);
