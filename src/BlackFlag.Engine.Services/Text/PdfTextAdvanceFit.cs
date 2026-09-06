namespace BlackFlag.Engine.Services.Text;

/// <summary>
/// Classifies whether a replacement preserves the original horizontal advance
/// or requires local compensation/reflow.
/// </summary>
public enum PdfTextAdvanceFit
{
    Exact = 0,
    LocallyAdjustable = 1,
    RequiresReflow = 2
}
