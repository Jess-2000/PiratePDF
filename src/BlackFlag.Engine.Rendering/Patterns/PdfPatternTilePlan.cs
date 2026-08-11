
namespace BlackFlag.Engine.Rendering.Patterns;

/// <summary>Describes the finite tile-index range required for one paint operation.</summary>
public sealed record PdfPatternTilePlan(
    int MinimumXIndex,
    int MaximumXIndex,
    int MinimumYIndex,
    int MaximumYIndex,
    int CellCount);
