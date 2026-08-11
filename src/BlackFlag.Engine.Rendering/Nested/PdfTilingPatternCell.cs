
using BlackFlag.Engine.Rendering.DisplayList;

namespace BlackFlag.Engine.Rendering.Nested;

/// <summary>Represents one interpreted Type 1 tiling-pattern cell.</summary>
public sealed record PdfTilingPatternCell(
    PdfDisplayList Content,
    PdfDisplayBounds Bounds,
    double XStep,
    double YStep,
    PdfMatrix Matrix,
    int PaintType,
    int TilingType);
